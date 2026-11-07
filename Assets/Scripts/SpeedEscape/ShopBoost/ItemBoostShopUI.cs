using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

[Serializable]
public class ShopSlotConfig
{
    public RareType[] allowedRarities;
    public int quantity = 1;
}

public class ItemBoostShopUI : MonoBehaviour
{
    [Header("Shop Slots")]
    public ItemBoostProductView[] shopSlots;

    [Header("Timer UI")]
    public TMP_Text restockTimerText;

    [Header("Settings")]
    public float restockIntervalMinutes = 5f;

    [Header("Slot Configuration")]
    public ShopSlotConfig[] slotConfigs = new ShopSlotConfig[4];

    [Header("Notifications")]
    public LanguageDirectory notifyNotEnoughMoney = new LanguageDirectory(
        "Недостаточно денег",
        "Not enough money",
        "Yetersiz bakiye"
    );

    public LanguageDirectory notifyStockEmpty = new LanguageDirectory(
        "Товар раскуплен",
        "Item sold out",
        "Urun tukendi"
    );

    public LanguageDirectory notifyRestockComplete = new LanguageDirectory(
        "Товары в магазине обновились!",
        "Shop items have been refreshed!",
        "Magaza urunleri yenilendi!"
    );

    public LanguageDirectory uiTimerText = new LanguageDirectory(
        "Новые предметы через",
        "New items in",
        "Yeni urunler"
    );

    private SavesYG saves;
    private Dictionary<string, ItemDefinition> itemsDatabase;
    private float restockIntervalSeconds;

    private float autoSaveTimer = 0f;
    private const float autoSaveInterval = 10f;

    [Header("References")]
    public InventoryBoostUI inventoryUI;

    [SerializeField] private Color _restockColor;
    [SerializeField] private AudioClip _restockSound;

    private void Awake()
    {
        itemsDatabase = Resources.LoadAll<ItemDefinition>("GameData/ItemsBoost")
                    .ToDictionary(item => item.itemId);

        saves = YG2.saves;
        restockIntervalSeconds = restockIntervalMinutes * 60f;

        Debug.Log($"[Shop] Loaded {itemsDatabase.Count} items from database");
    }

    private void Start()
    {
        InitializeShop();
        CheckRestock();
        RefreshShopUI();
    }

    private void Update()
    {
        UpdateTimer();
        AutoSave();
    }

    private void UpdateTimer()
    {
        if (restockTimerText == null) return;

        // Уменьшаем время только если оно больше 0
        if (saves.shop.timeRemaining > 0)
        {
            saves.shop.timeRemaining -= Time.deltaTime;
        }

        float timeRemaining = Mathf.Max(0, saves.shop.timeRemaining);

        if (timeRemaining <= 0)
        {
            restockTimerText.text = "00:00";

            // Проверяем ресток только один раз при достижении 0
            if (saves.shop.timeRemaining <= 0)
            {
                saves.shop.timeRemaining = 0;
                CheckRestock();
            }
        }
        else
        {
            int minutes = (int)(timeRemaining / 60);
            int seconds = (int)(timeRemaining % 60);
            restockTimerText.text = $"{UTF.ReadLanguageDirectory(uiTimerText)} {minutes:00}:{seconds:00}";
        }
    }

    private float GetTimeRemaining()
    {
        return Mathf.Max(0, saves.shop.timeRemaining);
    }

    private void AutoSave()
    {
        autoSaveTimer += Time.deltaTime;

        if (autoSaveTimer >= autoSaveInterval)
        {
            autoSaveTimer = 0f;
            YG2.SaveProgress();
            Debug.Log($"[Shop] Auto-saved shop data. Time remaining: {saves.shop.timeRemaining}s");
        }
    }

    private void InitializeShop()
    {
        Debug.Log("[Shop] Initializing shop...");

        if (saves.shop == null)
        {
            Debug.LogError("[Shop] saves.shop is null! Creating new ShopData...");
            saves.shop = new ShopData();
        }

        if (saves.shop.items == null || saves.shop.items[0] == null)
        {
            Debug.Log("[Shop] Shop items array is null, creating new items...");

            if (saves.shop.items == null)
            {
                saves.shop.items = new ShopItemBoostData[4];
            }

            for (int i = 0; i < 4; i++)
            {
                saves.shop.items[i] = new ShopItemBoostData();
                Debug.Log($"[Shop] Created shop item slot {i}");
            }

            saves.shop.timeRemaining = restockIntervalSeconds;
            Debug.Log($"[Shop] Set initial timeRemaining to {saves.shop.timeRemaining}s");

            Debug.Log("[Shop] Shop initialization complete");

            YG2.SaveProgress();
            Debug.Log("[Shop] Saved initial shop data");
        }
        else
        {
            Debug.Log("[Shop] Shop items already initialized");
            Debug.Log($"[Shop] Time remaining from save: {saves.shop.timeRemaining}s");
        }
    }

    private void CheckRestock()
    {
        float timeRemaining = saves.shop.timeRemaining;

        Debug.Log($"[Shop] CheckRestock - Time remaining: {timeRemaining}s, Interval: {restockIntervalSeconds}s");

        if (timeRemaining <= 0)
        {
            Debug.Log("[Shop] Restock condition met - performing restock!");
            RestockShop();
        }
        else
        {
            Debug.Log($"[Shop] Restock not ready - {timeRemaining}s remaining");
        }
    }

    private void RestockShop()
    {
        Debug.Log("[Shop] === STARTING RESTOCK ===");

        for (int i = 0; i < 4; i++)
        {
            var config = slotConfigs[i];

            Debug.Log($"[Shop] Slot {i} - Allowed rarities: {string.Join(", ", config.allowedRarities.Select(r => r.ToString()))}, Quantity: {config.quantity}");

            var itemsOfAllowedRarities = itemsDatabase.Values
                .Where(item => config.allowedRarities.Contains(item.rarity))
                .ToList();

            Debug.Log($"[Shop] Slot {i} - Found {itemsOfAllowedRarities.Count} items with allowed rarities");

            if (itemsOfAllowedRarities.Count > 0)
            {
                var randomItem = itemsOfAllowedRarities[UnityEngine.Random.Range(0, itemsOfAllowedRarities.Count)];

                // Сбрасываем purchased перед установкой нового товара
                saves.shop.items[i].purchased = false;
                saves.shop.items[i].itemId = randomItem.itemId;
                saves.shop.items[i].count = config.quantity;

                Debug.Log($"[Shop] Slot {i} - Selected item: {randomItem.itemId} ({randomItem.itemName}), Rarity: {randomItem.rarity}, Count: {config.quantity}");
            }
            else
            {
                Debug.LogWarning($"[Shop] Slot {i} - No items found with allowed rarities!");
            }
        }

        saves.shop.timeRemaining = restockIntervalSeconds;
        Debug.Log($"[Shop] Reset timeRemaining to {saves.shop.timeRemaining}s");

        YG2.SaveProgress();

        NotificationSingleManager.ShowNotification(UTF.ReadLanguageDirectory(notifyRestockComplete), _restockColor);
        UISound.manager.PlaySound(_restockSound, false);

        // Обновляем UI после рестока
        RefreshShopUI();

        Debug.Log("[Shop] === RESTOCK COMPLETE ===");
    }

    public void RefreshShopUI()
    {
        Debug.Log("[Shop] Refreshing shop UI...");

        for (int i = 0; i < 4; i++)
        {
            var shopItem = saves.shop.items[i];

            Debug.Log($"[Shop] Slot {i} - ItemId: {shopItem.itemId}, Count: {shopItem.count}, Purchased: {shopItem.purchased}");

            if (!string.IsNullOrEmpty(shopItem.itemId) && itemsDatabase.TryGetValue(shopItem.itemId, out var definition))
            {
                shopSlots[i].Initialize(definition, this, i);
                shopSlots[i].SetPurchased(shopItem.purchased);
                Debug.Log($"[Shop] Slot {i} - UI initialized with {definition.itemName}");
            }
            else
            {
                Debug.LogWarning($"[Shop] Slot {i} - Item not found in database or itemId is empty!");
            }
        }

        Debug.Log("[Shop] UI refresh complete");
    }

    public void BuyItem(int slotIndex, ValueType valueType, int price)
    {
        Debug.Log($"[Shop] BuyItem called for slot {slotIndex}");

        var shopItem = saves.shop.items[slotIndex];

        if (valueType != ValueType.Diamond)
        {
            if (shopItem.purchased)
            {
                Debug.LogWarning($"[Shop] Item in slot {slotIndex} already purchased!");
                NotificationManager.ShowNotification(UTF.ReadLanguageDirectory(notifyStockEmpty), NotificationType.Error);
                return;
            }
        }

        if (!itemsDatabase.TryGetValue(shopItem.itemId, out var definition))
        {
            Debug.LogError($"[Shop] Item {shopItem.itemId} not found in database!");
            return;
        }

        if (ValueSystem.SubstractionBalance(price, valueType))
        {
            Debug.Log($"[Shop] Purchase successful for slot {slotIndex}");
            Debug.Log($"[Shop] Item count before: {shopItem.count}");

            saves.inventory.storage.Add(new ItemBoost(shopItem.itemId));

            // Отнимаем товар если купили не за харду
            if (valueType != ValueType.Diamond)
                shopItem.count -= 1;


            Debug.Log($"[Shop] Item count after: {shopItem.count}");

            if (shopItem.count <= 0)
            {
                saves.shop.items[slotIndex].purchased = true;
                Debug.Log($"[Shop] All items purchased in slot {slotIndex} - marking as purchased");
            }

            YG2.SaveProgress();

            RefreshShopUI();

            if (inventoryUI != null)
            {
                inventoryUI.RefreshInventory();
                Debug.Log("[Shop] Updated inventory UI after purchase");
            }

            UISound.manager.PlayBuy(false);

            Debug.Log($"[Shop] Item {shopItem.itemId} added to inventory");
        }
    }

    [ContextMenu("ForceRestock")]
    public void ForceRestock()
    {
        Debug.Log("[Shop] FORCE RESTOCK called!");
        saves.shop.timeRemaining = 0;
        CheckRestock();
    }
}