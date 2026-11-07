using System.Collections;
using TMPro;
using UnityEngine;
using YG;
using DG.Tweening;
using System;

public class BrainrotPoint : MonoBehaviour
{
    public int id;
    public Player owner;

    public BrainrotPointData brainrotPointData = new BrainrotPointData();
    public BrainrotInWorld brainrotInWorld;

    [SerializeField] private Transform _brainrotSpawnPoint;

    // Balance display
    [SerializeField] private TMP_Text _balanceDisplay;
    [SerializeField] private Transform _balanceParent;

    private Coroutine _earningLoop;
    private bool _balanceDisplayVisible = true; // кешируем текущее состояние

    // Animation

    #region Animation Parameters
    [Header("SpawnPoint anim")]
    [SerializeField] private float popUp = 0.18f;        // высота подпрыгивания (локальная)
    [SerializeField] private float upTime = 0.10f;
    [SerializeField] private float downTime = 0.14f;

    [SerializeField] private Vector3 stretchScale = new Vector3(0.92f, 1.12f, 0.92f); // сплющить XZ, вытянуть Y
    [SerializeField] private float squashTime = 0.10f;
    [SerializeField] private Ease upEase = Ease.OutQuad;
    [SerializeField] private Ease downEase = Ease.OutBounce;

    private Tween _spawnPointTween;
    private float _baseLocalY;
    private Vector3 _baseLocalScale;
    #endregion

    private void Awake()
    {
        if (_brainrotSpawnPoint != null)
        {
            _baseLocalY = _brainrotSpawnPoint.localPosition.y;
            _baseLocalScale = _brainrotSpawnPoint.localScale;
        }
    }

    public void Initialization(Player owner)
    {
        this.owner = owner;
        UpdateBalanceDisplay(); // инициализируем видимость при старте
    }

    public void InitializationSave(BrainrotPointData brainrotPointData)
    {
        this.brainrotPointData = brainrotPointData ?? new BrainrotPointData();

        if (this.brainrotPointData.brainrotData != null &&
            !string.IsNullOrEmpty(this.brainrotPointData.brainrotData.id))
        {
            SetBrainrot(this.brainrotPointData.brainrotData, false);
        }

        UpdateBalanceDisplay();
    }

    public void SetBrainrot(BrainrotData brainrotData, bool needSave)
    {
        if (brainrotData == null)
        {
            Debug.LogError($"SetBrainrot: brainrotData is NULL on point {name}");
            return;
        }

        if (string.IsNullOrEmpty(brainrotData.id))
        {
            Debug.LogError($"SetBrainrot: brainrotData.id is NULL/EMPTY on point {name}");
            return;
        }

        BrainrotBase brainrotBase = BrainrotManager.GetBrainrotById(brainrotData.id);
        if (brainrotBase == null)
        {
            Debug.LogError($"SetBrainrot: BrainrotBase with id '{brainrotData.id}' not found for point {name}");
            return;
        }

        if (brainrotBase.brainrotPrefab == null)
        {
            Debug.LogError($"SetBrainrot: brainrotPrefab is NULL for Brainrot '{brainrotBase.id}' on point {name}");
            return;
        }

        if (_brainrotSpawnPoint == null)
        {
            Debug.LogError($"SetBrainrot: _brainrotSpawnPoint is NULL on point {name}");
            return;
        }

        // дальше твоя старая логика, но brainrotBase уже есть:
        if (brainrotPointData == null)
            brainrotPointData = new BrainrotPointData();

        if (brainrotPointData.brainrotData == null)
            brainrotPointData.brainrotData = new BrainrotData();

        brainrotPointData.brainrotData = brainrotData;

        if (brainrotInWorld != null)
        {
            Destroy(brainrotInWorld.gameObject);
            brainrotInWorld = null;
        }

        BrainrotInWorld instance = Instantiate(
            brainrotBase.brainrotPrefab,
            _brainrotSpawnPoint.position,
            Quaternion.identity
        ).GetComponent<BrainrotInWorld>();

        instance.transform.SetParent(_brainrotSpawnPoint, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = _brainrotSpawnPoint.localRotation;

        brainrotInWorld = instance;

        brainrotInWorld.SetOwnerPoint(this);              // важная связка
        brainrotInWorld.SetBrainrot(brainrotBase, brainrotData);
        brainrotInWorld.ChangeBrainrotState(BrainrotState.InBase);

        if (!brainrotBase.IsLucky)
            StartEarning();      // лаки‑блок сам не фармит
        else
            StopEarning();

        if (owner.thisRealPlayer)
        {
            if (owner.tsunamiReference.playerBase != null)
                owner.tsunamiReference.playerBase.RefreshInfoDisplay();

            if (needSave)
            {
                YG2.MetricaSend("setup_brainrot");

                YG2.saves.playerData.playerBase.brainrotPoints[id] = brainrotPointData;
                YG2.SaveProgress();
            }
        }
    }

    public void OpenLuckyBrainrot(BrainrotInWorld luckyInstance)
    {
        if (luckyInstance == null) return;
        if (!luckyInstance.IsLuckyBlock) return;

        StopEarning();

        luckyInstance.StartLuckyRoll();
    }

    public void StartEarning()
    {
        StopEarning();
        if (brainrotInWorld != null && brainrotPointData?.brainrotData != null)
        {
            _earningLoop = StartCoroutine(EarningLoop());
        }
    }

    public void StopEarning()
    {
        if (_earningLoop != null)
        {
            StopCoroutine(_earningLoop);
            _earningLoop = null;
        }
    }

    private IEnumerator EarningLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (brainrotInWorld == null || brainrotPointData?.brainrotData == null)
            {
                StopEarning();
                yield break;
            }

            if (brainrotInWorld != null && brainrotInWorld.brainrot.earningPerSecond > 0)
            {
                if (owner.thisRealPlayer)
                {
                    brainrotPointData.balance += (long)Math.Round(
    brainrotInWorld.brainrot.earningPerSecond * RebirthManager.instance.CurrentMultiplier
);
                }
                else
                {
                    brainrotPointData.balance += brainrotInWorld.brainrot.earningPerSecond;
                }

                UpdateBalanceDisplay();
            }
        }
    }

    public long GetEarchPerSecond()
    {
        if (brainrotInWorld == null)
        {
            return 0;
        }

        return (long)Math.Round(
        brainrotInWorld.brainrot.earningPerSecond * RebirthManager.instance.CurrentMultiplier);
    }

    private void UpdateBalanceDisplay()
    {
        if (brainrotPointData == null)
            brainrotPointData = new BrainrotPointData();

        if (_balanceDisplay == null) return;

        // если забыли проставить parent — используем объект текста как контейнер
        if (_balanceParent == null)
            _balanceParent = _balanceDisplay.transform;

        bool shouldShow = brainrotPointData.balance > 0;

        // Меняем активность безопасно (и только если реально меняется)
        if (_balanceDisplayVisible != shouldShow)
        {
            if (_balanceParent != null)
                _balanceParent.gameObject.SetActive(shouldShow);

            _balanceDisplayVisible = shouldShow;
        }

        if (shouldShow)
            _balanceDisplay.text = "$" + UTF.AbbreviateNumber(brainrotPointData.balance);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();

            if (player == owner && player.thisRealPlayer)
            {
                if (brainrotPointData.balance > 0)
                {
                    ValueSystem.AddBalance(brainrotPointData.balance, ValueType.Coin);
                    PlaySpawnPointPop();

                    brainrotPointData.balance = 0;
                    UpdateBalanceDisplay();
                }
            }
        }
    }

    private void OnDisable()
    {
        StopEarning();
    }

    #region Animation
    private void PlaySpawnPointPop()
    {
        if (_brainrotSpawnPoint == null) return;

        // убиваем предыдущее и жёстко возвращаем базу, чтобы не накапливалось
        _spawnPointTween?.Kill();
        _brainrotSpawnPoint.DOKill();
        _brainrotSpawnPoint.localPosition = new Vector3(
            _brainrotSpawnPoint.localPosition.x, _baseLocalY, _brainrotSpawnPoint.localPosition.z);
        _brainrotSpawnPoint.localScale = _baseLocalScale;

        float upY = _baseLocalY + popUp;

        Sequence seq = DOTween.Sequence();

        // Вверх + squash (быстро)
        seq.Append(_brainrotSpawnPoint.DOLocalMoveY(upY, upTime).SetEase(upEase));
        seq.Join(_brainrotSpawnPoint.DOScale(Vector3.Scale(_baseLocalScale, stretchScale), squashTime).SetEase(Ease.OutQuad));

        // Вниз + возврат скейла (чуть дольше)
        seq.Append(_brainrotSpawnPoint.DOLocalMoveY(_baseLocalY, downTime).SetEase(downEase));
        seq.Join(_brainrotSpawnPoint.DOScale(_baseLocalScale, downTime).SetEase(Ease.OutQuad));

        _spawnPointTween = seq;
    }
    #endregion

    public void ClearBrainrot(bool needSave, bool destroyInstance = true)
    {
        StopEarning();

        // Убираем инстанс
        if (destroyInstance && brainrotInWorld != null)
            Destroy(brainrotInWorld.gameObject); // удалится в конце кадра [web:338]

        brainrotInWorld = null;

        // Чистим данные слота
        if (brainrotPointData == null) brainrotPointData = new BrainrotPointData();
        brainrotPointData.balance = 0;
        brainrotPointData.brainrotData = null;

        UpdateBalanceDisplay();

        // Чистим в сейве (только если это сейв реального игрока)
        if (needSave && owner != null && owner.thisRealPlayer)
        {
            YG2.saves.playerData.playerBase.brainrotPoints[id] = brainrotPointData; // пустой слот
            YG2.SaveProgress();
        }
    }
}
