using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

[System.Serializable]
public enum ValueType
{
    Coin,
    Diamond,
    Spin,
    Wins
}

[System.Serializable]
public class Price
{
    public int amount;
    public ValueType valueType;
}

public class ValueSystem : MonoBehaviour
{
    public static ValueSystem instance;

    // Событие для подписки на изменение баланса
    public static event Action OnBalanceChanged;

    [Space(15f)]

    [Header("Вид")]
    [Space(5f)]
    [SerializeField] private TMP_Text _balanceView_coin;
    [SerializeField] private TMP_Text _balanceView_diamond;
    [SerializeField] private TMP_Text _balanceView_spin;
    [SerializeField] private TMP_Text[] _balanceView_wins;

    public bool thisX2Money;

    [SerializeField] private BalancePopupPool balancePopup;
    [SerializeField] private ShopTabsScroll _shopTabsScroll;

    [SerializeField] private ShopUpgradesController _shopUpgradeControls;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        InitializationBalance();
    }

    public void InitializationBalance()
    {
        UpdateBalance();
    }

    public GameObject coinPrefab;
    public GameObject winsPrefab;
    public RectTransform spawnArea;

    public RectTransform targetPositionCoin;
    public RectTransform targetPositionWin;

    public int numberOfCoins = 10;
    public float spawnDelay = 0.2f;
    public float moveDuration = 1f;
    public float pullBackDistance = 50f;
    public float pullBackDuration = 0.3f;

    [SerializeField] private Canvas canvas;

    private void SpawnCoins(int amount, ValueType valueType)
    {
        GameObject prefab = null;
        Vector3 targetPosition = Vector3.zero;

        switch (valueType)
        {
            case ValueType.Coin:
                prefab = coinPrefab;
                targetPosition = targetPositionCoin.position;
                break;
        }

        for (int i = 0; i < amount; i++)
        {
            float delay = i * spawnDelay;

            DOVirtual.DelayedCall(delay, () =>
            {
                Vector2 spawnPosition = GetRandomPositionInRect(spawnArea);
                GameObject coin = Instantiate(prefab, spawnArea.transform);

                RectTransform coinRect = coin.GetComponent<RectTransform>();
                coinRect.anchoredPosition = spawnPosition;

                AnimateCoin(coinRect, targetPosition);
            });
        }
    }

    private Vector2 GetRandomPositionInRect(RectTransform rect)
    {
        Vector2 min = rect.rect.min;
        Vector2 max = rect.rect.max;
        return new Vector2(
            UnityEngine.Random.Range(min.x, max.x),
            UnityEngine.Random.Range(min.y, max.y)
        );
    }

    private void AnimateCoin(RectTransform coin, Vector3 targetPosition)
    {
        Vector2 spawnWorldPosition = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, coin.position);
        Vector2 targetWorldPosition = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, targetPosition);

        Vector2 pullBackWorldPosition = spawnWorldPosition + (Vector2.down * pullBackDistance);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(coin.parent as RectTransform, pullBackWorldPosition, canvas.worldCamera, out Vector2 pullBackLocalPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(coin.parent as RectTransform, targetWorldPosition, canvas.worldCamera, out Vector2 targetLocalPosition);

        Sequence sequence = DOTween.Sequence();

        sequence.Append(coin.DOAnchorPos(pullBackLocalPosition, pullBackDuration).SetEase(Ease.OutBack));
        sequence.Append(coin.DOAnchorPos(targetLocalPosition, moveDuration).SetEase(Ease.InOutQuad));
        sequence.OnComplete(() => Destroy(coin.gameObject));
    }

    public static void AddBalance(long amount, ValueType valueType) => instance._AddBalance(amount, valueType);
    public void _AddBalance(long amount, ValueType valueType)
    {
        long totalAmount;

        if (amount >= 5)
        {
            totalAmount = amount / 25;
            if (totalAmount > 10)
                totalAmount = 10;
        }
        else
        {
            totalAmount = 1;
        }

        switch (valueType)
        {
            case ValueType.Coin:
                YG2.saves.playerData.wallet.money += amount;
                PlayersManager.instance.realPlayer.SpawnMoneyView(amount);
                balancePopup.Show(amount);
                _shopUpgradeControls.RefreshAllViews();
                break;

            case ValueType.Diamond:
                YG2.saves.playerData.wallet.diamonds += amount;
                break;

            case ValueType.Spin:
                YG2.saves.playerData.wallet.spins += amount;
                FortuneWheelManager.instance.RefreshSpinButton();
                break;

            case ValueType.Wins:
                YG2.saves.playerData.wallet.wins += amount;
                YG2.SetLeaderboard("TopWin", (int)YG2.saves.playerData.wallet.wins);
                break;
        }

        UpdateBalance();

        UISound.manager.PlayGetMoney(false);
        YG2.SaveProgress();
    }

    public static bool SubstractionBalance(long amount, ValueType valueType) => instance._SubstractionBalance(amount, valueType);
    public bool _SubstractionBalance(long amount, ValueType valueType)
    {
        switch (valueType)
        {
            case ValueType.Coin:
                if (YG2.saves.playerData.wallet.money >= amount)
                {
                    YG2.saves.playerData.wallet.money -= amount;
                    UpdateBalance();
                    balancePopup.Show(-amount);
                    return true;
                }
                else
                {
                    _shopTabsScroll.OpenDonatePage(ValueType.Coin);
                }
                break;

            case ValueType.Diamond:
                if (YG2.saves.playerData.wallet.diamonds >= amount)
                {
                    YG2.saves.playerData.wallet.diamonds -= amount;
                    UpdateBalance();
                    return true;
                }
                else
                {
                    _shopTabsScroll.OpenDonatePage(ValueType.Diamond);
                }
                break;

            case ValueType.Spin:
                if (YG2.saves.playerData.wallet.spins >= amount)
                {
                    YG2.saves.playerData.wallet.spins -= amount;
                    UpdateBalance();
                    FortuneWheelManager.instance.RefreshSpinButton();
                    return true;
                }
                break;

            case ValueType.Wins:
                if (YG2.saves.playerData.wallet.wins >= amount)
                {
                    YG2.saves.playerData.wallet.wins -= amount;
                    YG2.SetLeaderboard("TopWin", (int)YG2.saves.playerData.wallet.wins);
                    UpdateBalance();
                    return true;
                }
                break;
        }

        switch (YG2.envir.language)
        {
            case "ru":
                NotificationManager.ShowNotification("У вас недостаточно средств!", NotificationType.Error);
                break;
            case "en":
                NotificationManager.ShowNotification("You don't have enough funds!", NotificationType.Error);
                break;
            case "tr":
                NotificationManager.ShowNotification("Yeterli paraniz yok!", NotificationType.Error);
                break;
        }

        return false;
    }

    public static bool HaveEnoughMoney(Price price) => instance._HaveEnoughMoney(price);
    public bool _HaveEnoughMoney(Price price)
    {
        switch (price.valueType)
        {
            case ValueType.Coin:

                if (YG2.saves.playerData.wallet.money >= price.amount)
                {
                    return true;
                }

                break;

            case ValueType.Diamond:

                if (YG2.saves.playerData.wallet.diamonds >= price.amount)
                {
                    return true;
                }

                break;
        }

        return false;
    }

    private void UpdateBalance()
    {
        _balanceView_coin.text = "$" + UTF.AbbreviateNumber(YG2.saves.playerData.wallet.money);
        _balanceView_diamond.text = YG2.saves.playerData.wallet.diamonds.ToString();
        _balanceView_spin.text = YG2.saves.playerData.wallet.spins.ToString();

        for(int i = 0; i < _balanceView_wins.Length; i++)
        {
            _balanceView_wins[i].text = UTF.AbbreviateNumber(YG2.saves.playerData.wallet.wins);
        }

        // Вызываем событие после обновления баланса
        OnBalanceChanged?.Invoke();
    }
}