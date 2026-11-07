using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YG;

public class RewardsPanel : MonoBehaviour
{
    public static RewardsPanel instance;

    [SerializeField] private List<DailyRewardCard> _dailyRewardCards;
    [SerializeField] private List<RewardData> _rewards;
    [SerializeField] private Button _claimButton;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private GameObject _timerObject;
    [SerializeField] private TextMeshProUGUI _timerButton;
    [SerializeField] private GameObject _noRewardObject;

    [SerializeField] private LuckyBrainrot _luckyBrainrot_cosmic;

    private float _updateTimerInterval = 1f;
    private float _timer;
    private bool _isUpdatingUI = false;   // защита от рекурсии

    [SerializeField] private LanguageDirectory _diamonds;
    [SerializeField] private LanguageDirectory _coins;

    [SerializeField] private Color _diamondColor;
    [SerializeField] private Color _coinColor;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (_timerObject.activeInHierarchy || _timerButton.gameObject.activeInHierarchy)
        {
            _timer += Time.deltaTime;
            if (_timer >= _updateTimerInterval)
            {
                _timer = 0f;
                UpdateTimerText();
            }
        }
    }

    public bool HasAvailableRewards()
    {
        return GetNextAvailableRewardIndex() >= 0;
    }

    public void OpenDailyReward()
    {
        Debug.Log("Start open Daily Reward");
        if (!HasAvailableRewards())
            return;
        Debug.Log("Open Daily Reward");

        if (YG2.saves.trainingData.training_brainrot_complete)
        {
            PageManager.OpenPageByName("DailyRewards");

            UISound.manager.PlayOpenDailyReward(false);
        }
    }

    private void Initialize()
    {
        // При открытии панели обновляем дни как в образце
        PlayerAttendance.UpdateAttendance();

        _claimButton.onClick.AddListener(ClaimNextReward);
        RefreshCards();
        UpdateState();
    }

    private void RefreshCards()
    {
        for (int i = 0; i < _dailyRewardCards.Count; i++)
        {
            _dailyRewardCards[i].Initialize(i, _rewards[i].Type, _rewards[i].amount);
        }
    }

    public void ClaimNextReward()
    {
        int nextRewardIndex = GetNextAvailableRewardIndex();

        switch (_rewards[nextRewardIndex].Type)
        {
            case TreasuresTypeGift.Brainrot:
            case TreasuresTypeGift.LuckyBrainrot:
                if (PlayersManager.instance.realPlayer.tsunamiReference.playerBase.FindFreePoint() == null)
                {
                    return;
                }

                break;
        }
        if (nextRewardIndex >= 0)
        {
            AddReward(nextRewardIndex);
            _dailyRewardCards[nextRewardIndex].MarkAsClaimed();

            // время клейма – для таймера (до следующего дня)
            YG2.saves.LastRewardClaimTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            YG2.saves.WasRewardCollectedOnSpecificDay[nextRewardIndex] = true;

            YG2.SaveProgress();
            UpdateUI();
        }

        PageManager.ClosePageByName("DailyRewards");
    }

    private int GetNextAvailableRewardIndex()
    {
        for (int i = 0; i < _dailyRewardCards.Count; i++)
        {
            if (i < YG2.saves.DaysUnlocked &&
                i < YG2.saves.WasRewardCollectedOnSpecificDay.Count &&
                !YG2.saves.WasRewardCollectedOnSpecificDay[i])
            {
                return i;
            }
        }
        return -1;
    }

    private void AddReward(int id)
    {
        Base playerBase = PlayersManager.instance.realPlayer.tsunamiReference.playerBase;

        switch (_rewards[id].Type)
        {
            case TreasuresTypeGift.Coins:
                ValueSystem.AddBalance(_rewards[id].amount, ValueType.Coin);
                RewardNotification.Show(_rewards[id].icon, UTF.ReadLanguageDirectory(_coins), "+" + _rewards[id].amount + "$", _coinColor, Color.white);
                break;
            case TreasuresTypeGift.Diamonds:
                ValueSystem.AddBalance(_rewards[id].amount, ValueType.Diamond);
                RewardNotification.Show(_rewards[id].icon, UTF.ReadLanguageDirectory(_diamonds), "+" + _rewards[id].amount, _diamondColor, Color.white);
                break;
            case TreasuresTypeGift.Brainrot:
                BrainrotBase brainrot = BrainrotManager.GetBrainrotById(_rewards[id].id);
                RewardNotification.Show(brainrot.icon, UTF.ReadLanguageDirectory(brainrot.brainrotName), BrainrotManager.GetBrainrotRareName(brainrot.brainrotRare), Color.white, BrainrotManager.GetBrainrotRareColor(brainrot.brainrotRare));
                playerBase.PlaceBrainrot(brainrot);
                break;
            case TreasuresTypeGift.LuckyBrainrot:

                playerBase.SpawnLuckyBlockToFreePoint(_luckyBrainrot_cosmic);
                break;
            default:
                Debug.LogError($"Unknown reward type: {_rewards[id].Type}");
                break;
        }

        UISound.manager.PlayWin(false);
        YG2.MetricaSend("claim_daily_reward", "day", id.ToString());
    }

    public void UpdateUI()
    {
        if (_isUpdatingUI) return;
        _isUpdatingUI = true;

        // Обновляем только визуалку и состояние кнопок/таймера
        RefreshCards();
        UpdateState();

        _isUpdatingUI = false;
    }

    private void UpdateState()
    {
        bool hasUnclaimed = GetNextAvailableRewardIndex() >= 0;

        Debug.Log($"[REWARDS DEBUG] DaysUnlocked={YG2.saves.DaysUnlocked}, " +
                  $"ClaimFlags=[{string.Join(",", YG2.saves.WasRewardCollectedOnSpecificDay.Take(7))}], " +
                  $"HasUnclaimed={hasUnclaimed}, NextIndex={GetNextAvailableRewardIndex()}");

        // Если есть награда, которую можно забрать:
        // кнопка активна, показываем _noRewardObject, таймер прячем.
        _claimButton.gameObject.SetActive(hasUnclaimed);

        if (hasUnclaimed)
        {
            _noRewardObject.SetActive(true);      // "Награда доступна"
            _timerObject.SetActive(false);        // таймер не нужен
            _timerButton.gameObject.SetActive(false);
        }
        else
        {
            // Наград нет → ждем следующую, показываем таймер
            _noRewardObject.SetActive(false);
            _timerObject.SetActive(true);
            _timerButton.gameObject.SetActive(true);
            UpdateTimerText();
        }
    }


    private void UpdateTimerText()
    {
        long lastClaimTime = YG2.saves.LastRewardClaimTime;

        // Если награду ещё ни разу не брали – можно либо показывать 00, 
        // либо полный день ожидания. Тут оставлю 00.
        if (lastClaimTime == 0)
        {
            _timerText.text = "00:00:00:00";
            _timerButton.text = "00:00:00:00";
            return;
        }

        long secondsUntilTomorrow = GetSecondsUntilTomorrow(lastClaimTime);

        if (secondsUntilTomorrow <= 0)
        {
            // Формально время вышло – до следующего календарного дня 0.
            // Но сама разблокировка следующего дня у тебя делается через PlayerAttendance.UpdateAttendance()
            // при следующем входе/рестарте, поэтому просто держим 00.
            _timerText.text = "00:00:00:00";
            _timerButton.text = "00:00:00:00";
            return;
        }

        int days = (int)(secondsUntilTomorrow / 86400);
        int hours = (int)((secondsUntilTomorrow % 86400) / 3600);
        int minutes = (int)((secondsUntilTomorrow % 3600) / 60);
        int secs = (int)(secondsUntilTomorrow % 60);

        string t = $"{days:D2}:{hours:D2}:{minutes:D2}:{secs:D2}";
        _timerText.text = t;
        _timerButton.text = t;
    }


    private long GetSecondsUntilTomorrow(long lastClaimTimestamp)
    {
        if (lastClaimTimestamp == 0)
            return 86400; // сутки по умолчанию

        // Время клейма в UTC
        DateTimeOffset claimDate = DateTimeOffset.FromUnixTimeSeconds(lastClaimTimestamp);
        DateTime claimUtcDate = claimDate.UtcDateTime.Date;      // полночь дня клейма в UTC

        // Полночь следующего дня в UTC
        DateTimeOffset tomorrowUtc = new DateTimeOffset(claimUtcDate.AddDays(1), TimeSpan.Zero);

        long tomorrowTimestamp = tomorrowUtc.ToUnixTimeSeconds();
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return Math.Max(0, tomorrowTimestamp - now);
    }

}


[Serializable]
public class RewardData
{
    public TreasuresTypeGift Type;
    public int amount;
    public string id;
    public Sprite icon;
}

public enum TreasuresTypeGift
{
    Coins = 10,
    Diamonds = 20,
    Brainrot = 30,
    LuckyBrainrot = 40
}
