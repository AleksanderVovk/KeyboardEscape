using System.Collections.Generic;
using UnityEngine;
using YG;

public class TreadmillPurchaseManager : MonoBehaviour
{
    public static TreadmillPurchaseManager Instance;

    private Treadmill _pendingTreadmill;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        EnsureSaveData();

        // Проверка необработанных покупок выполняется компонентом
        // ConsumePurchasesYG, если он установлен в проекте.
    }

    private void OnEnable()
    {
        YG2.onPurchaseSuccess += OnPurchaseSuccess;
        YG2.onPurchaseFailed += OnPurchaseFailed;
    }

    private void OnDisable()
    {
        YG2.onPurchaseSuccess -= OnPurchaseSuccess;
        YG2.onPurchaseFailed -= OnPurchaseFailed;
    }

    public void Buy(Treadmill treadmill)
    {
        if (treadmill == null)
            return;

        EnsureSaveData();

        if (IsPurchased(treadmill.purchaseId))
        {
            treadmill.ActivateForPlayer();
            CloseWindow();
            return;
        }

        _pendingTreadmill = treadmill;

        Debug.Log($"[TreadmillPurchase] Start purchase: {treadmill.purchaseId}");

        YG2.BuyPayments(treadmill.purchaseId);
    }

    private void OnPurchaseSuccess(string id)
    {
        Debug.Log($"[TreadmillPurchase] Purchase successful: {id}");

        Treadmill treadmill = _pendingTreadmill;

        if (treadmill == null || treadmill.purchaseId != id)
            treadmill = FindTreadmillByPurchaseId(id);

        if (treadmill == null)
        {
            Debug.LogWarning(
                $"[TreadmillPurchase] Treadmill not found for purchase ID: {id}"
            );
            return;
        }

        UnlockTreadmill(id);

        treadmill.MarkPurchased();

        CloseWindow();

        // Если игрок всё ещё стоит в триггере,
        // дорожка сразу начинает работать.
        if (treadmill.IsPlayerInside)
            treadmill.ActivateForPlayer();

        _pendingTreadmill = null;

        YG2.saves.playerData.donateCount++;
        YG2.SaveProgress();
    }

    private void OnPurchaseFailed(string id)
    {
        Debug.LogWarning($"[TreadmillPurchase] Purchase failed: {id}");

        _pendingTreadmill = null;

        NotificationManager.ShowNotification(
            UTF.ReadLanguageDirectory(
                new LanguageDirectory(
                    "Покупка не была совершена",
                    "The purchase was not completed",
                    "Satın alma işlemi tamamlanamadı"
                )
            ),
            NotificationType.Error
        );
    }

    public bool IsPurchased(string purchaseId)
    {
        EnsureSaveData();

        return YG2.saves.playerData.purchasedTreadmills.Contains(purchaseId);
    }

    private void UnlockTreadmill(string purchaseId)
    {
        EnsureSaveData();

        if (!YG2.saves.playerData.purchasedTreadmills.Contains(purchaseId))
        {
            YG2.saves.playerData.purchasedTreadmills.Add(purchaseId);
        }

        YG2.SaveProgress();

        Debug.Log($"[TreadmillPurchase] Unlocked: {purchaseId}");
    }

    private Treadmill FindTreadmillByPurchaseId(string purchaseId)
    {
        Treadmill[] treadmills = FindObjectsOfType<Treadmill>(true);

        foreach (Treadmill treadmill in treadmills)
        {
            if (treadmill != null && treadmill.purchaseId == purchaseId)
                return treadmill;
        }

        return null;
    }

    private void CloseWindow()
    {
        if (TreadmillPurchaseWindow.Instance != null)
            TreadmillPurchaseWindow.Instance.Close();
    }

    private void EnsureSaveData()
    {
        if (YG2.saves == null || YG2.saves.playerData == null)
            return;

        if (YG2.saves.playerData.purchasedTreadmills == null)
        {
            YG2.saves.playerData.purchasedTreadmills =
                new List<string>();
        }
    }
}