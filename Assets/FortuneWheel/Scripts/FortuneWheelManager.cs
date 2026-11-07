using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Linq;
using UnityEngine.Events;
using YG;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FortuneWheelManager : MonoBehaviour
{
    public static FortuneWheelManager instance;

    [Header("UI")]
    public Button FreeTurnButton;
    public GameObject Close;
    public GameObject Circle;

    [Header("How much currency one paid turn costs")]
    public int TurnCost = 300;

    [Header("Params for each sector")]
    public FortuneWheelSector[] Sectors;

    [Header("Time Between Two Free Turns")]
    public int TimerMaxHours;
    [Range(0, 59)] public int TimerMaxMinutes;
    [Range(0, 59)] public int TimerMaxSeconds = 10;

    [Header("Can players turn the wheel for currency?")]
    public bool IsPaidTurnEnabled = true;

    [Header("Can players turn the wheel for FREE from time to time?")]
    public bool IsFreeTurnEnabled = true;

    public LuckyBrainrot luckyBrainrot;

    private bool _isStarted;
    private bool _isCancelledThisSpin;
    private bool _refundSpinOnCancel = true;

    private float _finalAngle;
    private float _startAngle;
    private float _currentLerpRotationTime;

    private int _currentCoinsAmount = 1000;
    private int _previousCoinsAmount;

    private int _timerRemainingHours;
    private int _timerRemainingMinutes;
    private int _timerRemainingSeconds;

    private DateTime _nextFreeTurnTime;
    private const string LAST_FREE_TURN_TIME_NAME = "LastFreeTurnTimeTicks";

    private bool _isFreeTurnAvailable;
    private FortuneWheelSector _finalSector;

    private bool _buttonsHooked;

    private void Awake()
    {
        instance = this;
        _previousCoinsAmount = _currentCoinsAmount;

        HookupButtonsOnce();
        RefreshAllButtons();
    }

    private void OnEnable()
    {
        HookupButtonsOnce();
        RefreshAllButtons();
    }

    private void HookupButtonsOnce()
    {
        if (_buttonsHooked) return;

        if (FreeTurnButton != null)
        {
            FreeTurnButton.onClick.RemoveListener(TurnWheelButtonClick);
            FreeTurnButton.onClick.AddListener(TurnWheelButtonClick);
        }

        _buttonsHooked = true;
    }

    public void RefreshAllButtons()
    {
        RefreshSpinButton();
        RefreshCloseState();
    }

    private void RefreshCloseState()
    {
        // Close скрываем на время вращения, и показываем когда вращение завершено/отменено.
        if (Close != null)
            Close.SetActive(!_isStarted); // SetActive true/false включает/выключает объект [web:7]
    }

    public void RefreshSpinButton()
    {
        bool canSpin = !_isStarted && GetSpins() > 0;
        SetButtonState(FreeTurnButton, canSpin);

        // Если хочешь скрывать кнопку "крутить" во время спина:
        if (FreeTurnButton != null)
            FreeTurnButton.gameObject.SetActive(!_isStarted);
    }

    private long GetSpins()
    {
        return YG2.saves.playerData.wallet.spins;
    }

    private void SetButtonState(Button button, bool enabled)
    {
        if (button == null) return;

        button.interactable = enabled;

        var img = button.GetComponent<Image>();
        if (img != null)
            img.color = new Color(1f, 1f, 1f, enabled ? 1f : 0.5f);
    }

    private void TurnWheel(bool isFree)
    {
        _currentLerpRotationTime = 0f;
        _isCancelledThisSpin = false;

        int[] sectorsAngles = new int[Sectors.Length];
        for (int i = 1; i <= Sectors.Length; i++)
            sectorsAngles[i - 1] = 360 / Sectors.Length * i;

        double rndNumber = UnityEngine.Random.Range(1, Sectors.Sum(sector => sector.Probability));

        int cumulativeProbability = 0;
        int randomFinalAngle = sectorsAngles[0];
        _finalSector = Sectors[0];

        for (int i = 0; i < Sectors.Length; i++)
        {
            cumulativeProbability += Sectors[i].Probability;
            if (rndNumber <= cumulativeProbability)
            {
                randomFinalAngle = sectorsAngles[i];
                _finalSector = Sectors[i];
                break;
            }
        }

        int fullTurnovers = 5;
        float pointerOffset = -90f;

        _finalAngle = fullTurnovers * 360 + (randomFinalAngle + pointerOffset);

        _isStarted = true;
        _previousCoinsAmount = _currentCoinsAmount;

        RefreshAllButtons(); // тут Close выключится
    }

    public void TurnWheelButtonClick()
    {
        if (_isStarted) return;

        if (GetSpins() <= 0)
        {
            RefreshAllButtons();
            return;
        }

        ValueSystem.SubstractionBalance(1, ValueType.Spin);
        YG2.SaveProgress();

        TurnWheel(true);
        RefreshAllButtons();
    }

    public void CancelSpinButtonClick()
    {
        if (!_isStarted)
        {
            RefreshAllButtons();
            return;
        }

        _isCancelledThisSpin = true;
        _isStarted = false;

        if (Circle != null)
            _startAngle = Circle.transform.eulerAngles.z;

        if (_refundSpinOnCancel)
        {
            ValueSystem.AddBalance(1, ValueType.Spin);
            YG2.SaveProgress();
        }

        RefreshAllButtons(); // тут Close включится
    }

    private void Update()
    {
        if (IsFreeTurnEnabled)
            UpdateFreeTurnTimer();

        if (!_isStarted)
            return;

        float maxLerpRotationTime = 4f;
        _currentLerpRotationTime += Time.deltaTime;

        if (_currentLerpRotationTime > maxLerpRotationTime || (Circle != null && Circle.transform.eulerAngles.z == _finalAngle))
        {
            _currentLerpRotationTime = maxLerpRotationTime;
            _isStarted = false;
            _startAngle = _finalAngle % 360;

            if (!_isCancelledThisSpin)
            {
                if (_finalSector != null && _finalSector.RewardCallback != null)
                    _finalSector.RewardCallback.Invoke();

                StartCoroutine(HideCoinsDelta());
            }

            RefreshAllButtons(); // тут Close включится
        }
        else
        {
            float t = _currentLerpRotationTime / maxLerpRotationTime;
            t = t * t * t * (t * (6f * t - 15f) + 10f);

            float angle = Mathf.Lerp(_startAngle, _finalAngle, t);
            if (Circle != null)
                Circle.transform.eulerAngles = new Vector3(0, 0, angle);
        }
    }

    public void RewardCoins(int awardCoins)
    {
        _currentCoinsAmount += awardCoins;
        StartCoroutine(UpdateCoinsAmount());
    }

    private IEnumerator HideCoinsDelta()
    {
        yield return new WaitForSeconds(1f);
    }

    private IEnumerator UpdateCoinsAmount()
    {
        const float seconds = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < seconds)
        {
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        _previousCoinsAmount = _currentCoinsAmount;
    }

    public void SetNextFreeTime()
    {
        _timerRemainingHours = TimerMaxHours;
        _timerRemainingMinutes = TimerMaxMinutes;
        _timerRemainingSeconds = TimerMaxSeconds;

        _nextFreeTurnTime = new DateTime(Convert.ToInt64(
                PlayerPrefs.GetString(LAST_FREE_TURN_TIME_NAME, DateTime.Now.Ticks.ToString())))
            .AddHours(TimerMaxHours)
            .AddMinutes(TimerMaxMinutes)
            .AddSeconds(TimerMaxSeconds);

        _isFreeTurnAvailable = false;
    }

    private void UpdateFreeTurnTimer()
    {
        if (_isFreeTurnAvailable)
            return;

        _timerRemainingHours = (int)(_nextFreeTurnTime - DateTime.Now).Hours;
        _timerRemainingMinutes = (int)(_nextFreeTurnTime - DateTime.Now).Minutes;
        _timerRemainingSeconds = (int)(_nextFreeTurnTime - DateTime.Now).Seconds;

        _isFreeTurnAvailable =
            _timerRemainingHours <= 0 && _timerRemainingMinutes <= 0 && _timerRemainingSeconds <= 0;
    }

    public void Buy1Spin()
    {
        if (ValueSystem.SubstractionBalance(99, ValueType.Diamond))
        {
            ValueSystem.AddBalance(1, ValueType.Spin);
            YG2.SaveProgress();
            RefreshAllButtons();
        }
    }

    public void Buy5Spin()
    {
        if (ValueSystem.SubstractionBalance(299, ValueType.Diamond))
        {
            ValueSystem.AddBalance(5, ValueType.Spin);
            YG2.SaveProgress();
            RefreshAllButtons();
        }
    }

    public void GiveReward(string id)
    {
        switch (id)
        {
            case "luckyblock_legendary":
                if (PlayersManager.instance.realPlayer.tsunamiReference.playerBase.FindFreePoint() != null)
                    PlayersManager.instance.realPlayer.tsunamiReference.playerBase.SpawnLuckyBlockToFreePoint(luckyBrainrot);
                break;

            case "money_100000":
                ValueSystem.AddBalance(100000, ValueType.Coin);
                break;
            case "money_50000":
                ValueSystem.AddBalance(50000, ValueType.Coin);
                break;
            case "money_500000":
                ValueSystem.AddBalance(500000, ValueType.Coin);
                break;
            case "money_20000":
                ValueSystem.AddBalance(20000, ValueType.Coin);
                break;
        }
    }
}

[Serializable]
public class FortuneWheelSector : System.Object
{
    public GameObject ValueTextObject;
    public int RewardValue = 100;

    [Range(0, 100)]
    public int Probability = 100;

    public UnityEvent RewardCallback;
}

#if UNITY_EDITOR
[CustomEditor(typeof(FortuneWheelManager))]
public class FortuneWheelManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        FortuneWheelManager myScript = (FortuneWheelManager)target;
    }
}
#endif
