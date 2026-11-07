using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

[System.Serializable]
public class TeleportStage
{
    public LanguageDirectory stageName;
    public Transform teleportPoint;

    public int priceWins;
    public int priceDiamonds;

    public Color lvlColor;
}

public class LvlTeleportShop : MonoBehaviour
{
    public static LvlTeleportShop Instance;

    [Header("Stages")]
    [SerializeField] private TeleportStage[] stages;

    [Header("UI")]
    [SerializeField] private Transform contentTransform;
    [SerializeField] private LvlTeleportView viewPrefab;

    [Header("Notifications")]
    [SerializeField]
    private LanguageDirectory teleportedText =
        new LanguageDirectory(
            "Телепортация успешна",
            "Teleportation successful",
            "Teleportasyon başarılı"
        );

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SpawnViews();
    }

    private void SpawnViews()
    {
        if (contentTransform == null || viewPrefab == null)
        {
            Debug.LogWarning(
                "[LvlTeleportShop] Content or prefab not assigned."
            );
            return;
        }

        foreach (Transform child in contentTransform)
            Destroy(child.gameObject);

        if (stages == null || stages.Length == 0)
        {
            Debug.LogWarning(
                "[LvlTeleportShop] No stages assigned."
            );
            return;
        }

        foreach (TeleportStage stage in stages)
        {
            LvlTeleportView view = Instantiate(
                viewPrefab,
                contentTransform
            );

            view.Initialize(stage, this);
        }
    }

    public void BuyStage(TeleportStage stage, int price, ValueType valueType)
    {
        if (stage == null)
            return;

        // Списываем цену
        if (ValueSystem.SubstractionBalance(price, valueType))
        {
            // Телепортируем игрока
            TeleportPlayer(stage.teleportPoint);

            NotificationManager.ShowNotification(
                UTF.ReadLanguageDirectory(teleportedText),
                NotificationType.Success
            );
        }
    }

    private void TeleportPlayer(Transform point)
    {
        if (point == null)
        {
            Debug.LogWarning(
                "[LvlTeleportShop] Teleport point is null."
            );
            return;
        }

        if (PlayersManager.instance == null ||
            PlayersManager.instance.realPlayer == null)
        {
            Debug.LogWarning(
                "[LvlTeleportShop] Real player not found."
            );
            return;
        }

        PlayersManager.instance.realPlayer.TeleportPlayer(point.position);

        Debug.Log(
            $"[LvlTeleportShop] Player teleported to {point.name}"
        );
    }
}