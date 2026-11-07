using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

public class EquipmentShopUI : MonoBehaviour
{
    [Header("Контейнеры")]
    public Transform trailContainer;
    public Transform objectContainer;

    [Header("UI элемента товара")]
    public EquipmentProductView productViewPrefab;
    public EquipmentProductView productViewPrefab_aura;

    [Header("Товары")]
    public ShopItemSO[] allShopItems;

    [Header("Игрок")]
    public GameObject player;
    public TrailRenderer playerTrail;
    public Transform objectParent;

    [Header("Текст множителя скорости")]
    public TMP_Text speedMultiplierText;

    [Header("Уведомления")]
    public LanguageDirectory notifyNotEnoughMoney = new LanguageDirectory(
        "Недостаточно денег",
        "Not enough money",
        "Yetersiz bakiye"
    );

    public LanguageDirectory notifyAlreadyPurchased = new LanguageDirectory(
        "Уже куплено",
        "Already purchased",
        "Zaten satın alındı"
    );

    private Dictionary<string, ShopItemSO> itemsDatabase;
    private SavesYG saves;
    private ShopItemSO currentEquippedTrail;
    private ShopItemSO currentEquippedObject;

    private void Awake()
    {
        itemsDatabase = allShopItems.ToDictionary(item => item.itemId);
        saves = YG2.saves;

        if (saves.equipmentShop == null)
        {
            saves.equipmentShop = new EquipmentShopData();
        }

        InitializeShopData();
    }

    private void Start()
    {
        RefreshShop();
        ApplySavedEquipment();
        UpdateSpeedMultiplierText();
    }

    private void InitializeShopData()
    {
        var trailItems = allShopItems.Where(x => x.itemType == ShopItemType.Trail).ToArray();
        var objectItems = allShopItems.Where(x => x.itemType == ShopItemType.Object).ToArray();

        if (saves.equipmentShop.trailCategory.items == null ||
            saves.equipmentShop.trailCategory.items.Length != trailItems.Length)
        {
            saves.equipmentShop.trailCategory.items = new ShopItemData[trailItems.Length];
            for (int i = 0; i < trailItems.Length; i++)
            {
                saves.equipmentShop.trailCategory.items[i] = new ShopItemData
                {
                    itemId = trailItems[i].itemId,
                    purchased = trailItems[i].priceWins == 0,
                    equipped = false
                };
            }
        }

        if (saves.equipmentShop.objectCategory.items == null ||
            saves.equipmentShop.objectCategory.items.Length != objectItems.Length)
        {
            saves.equipmentShop.objectCategory.items = new ShopItemData[objectItems.Length];
            for (int i = 0; i < objectItems.Length; i++)
            {
                saves.equipmentShop.objectCategory.items[i] = new ShopItemData
                {
                    itemId = objectItems[i].itemId,
                    purchased = objectItems[i].priceWins == 0,
                    equipped = false
                };
            }
        }

        YG2.SaveProgress();
    }

    private void ApplySavedEquipment()
    {
        var trailCategory = saves.equipmentShop.trailCategory.items;
        if (trailCategory != null)
        {
            foreach (var itemData in trailCategory)
            {
                if (itemData.equipped && itemsDatabase.TryGetValue(itemData.itemId, out var item))
                {
                    currentEquippedTrail = item;
                    ApplyTrail(item);
                    break;
                }
            }
        }

        var objectCategory = saves.equipmentShop.objectCategory.items;
        if (objectCategory != null)
        {
            foreach (var itemData in objectCategory)
            {
                if (itemData.equipped && itemsDatabase.TryGetValue(itemData.itemId, out var item))
                {
                    currentEquippedObject = item;
                    ApplyObject(item);
                    break;
                }
            }
        }
    }

    public void RefreshShop()
    {
        ClearContainer(trailContainer);
        ClearContainer(objectContainer);

        var trailItems = allShopItems
            .Where(x => x.itemType == ShopItemType.Trail)
            .OrderBy(x => x.priceWins)
            .ThenBy(x => x.priceDiamonds)
            .ToArray();

        var objectItems = allShopItems
            .Where(x => x.itemType == ShopItemType.Object)
            .OrderBy(x => x.priceWins)
            .ThenBy(x => x.priceDiamonds)
            .ToArray();

        for (int i = 0; i < trailItems.Length; i++)
        {
            var view = Instantiate(productViewPrefab, trailContainer);
            view.Initialize(trailItems[i], this, i, ShopItemType.Trail);
        }

        for (int i = 0; i < objectItems.Length; i++)
        {
            var view = Instantiate(productViewPrefab_aura, objectContainer);
            view.Initialize(objectItems[i], this, i, ShopItemType.Object);
        }
    }

    private void ClearContainer(Transform container)
    {
        if (container == null) return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }

    public void BuyItem(ShopItemSO item, ShopItemType type, ValueType valueType, int price)
    {
        var category = type == ShopItemType.Trail ? saves.equipmentShop.trailCategory : saves.equipmentShop.objectCategory;
        var itemData = category.items.FirstOrDefault(x => x.itemId == item.itemId);

        if (itemData == null)
        {
            Debug.LogError($"[Shop] Item {item.itemId} not found in save data!");
            return;
        }

        if (itemData.purchased)
        {
            NotificationManager.ShowNotification(UTF.ReadLanguageDirectory(notifyAlreadyPurchased), NotificationType.Error);
            return;
        }

        if (ValueSystem.SubstractionBalance(price, valueType))
        {
            itemData.purchased = true;
            YG2.SaveProgress();

            UISound.manager.PlayBuy(false);
            RefreshShop();

            Debug.Log($"[Shop] Purchased {item.itemId} for {price} {valueType}");
        }
        else
        {
            NotificationManager.ShowNotification(UTF.ReadLanguageDirectory(notifyNotEnoughMoney), NotificationType.Error);
        }
    }

    public void EquipItem(ShopItemSO item, ShopItemType type)
    {
        var category = type == ShopItemType.Trail ? saves.equipmentShop.trailCategory : saves.equipmentShop.objectCategory;
        var itemData = category.items.FirstOrDefault(x => x.itemId == item.itemId);

        if (itemData == null || !itemData.purchased)
        {
            Debug.LogError($"[Shop] Cannot equip {item.itemId} - not purchased!");
            return;
        }

        if (type == ShopItemType.Trail)
        {
            UnequipAll(ShopItemType.Trail);
            itemData.equipped = true;
            currentEquippedTrail = item;
            ApplyTrail(item);
        }
        else
        {
            UnequipAll(ShopItemType.Object);
            itemData.equipped = true;
            currentEquippedObject = item;
            ApplyObject(item);
        }

        YG2.SaveProgress();
        UISound.manager.PlayPop(false);
        RefreshShop();
        UpdateSpeedMultiplierText();

        Debug.Log($"[Shop] Equipped {item.itemId}");

        if (PlayerSpeedLevelManager.instance != null)
            PlayerSpeedLevelManager.instance.UpdateXpMultiplierFromEquipment();
    }

    public void UnequipItem(ShopItemSO item, ShopItemType type)
    {
        var category = type == ShopItemType.Trail ? saves.equipmentShop.trailCategory : saves.equipmentShop.objectCategory;
        var itemData = category.items.FirstOrDefault(x => x.itemId == item.itemId);

        if (itemData != null)
        {
            itemData.equipped = false;
            YG2.SaveProgress();

            if (type == ShopItemType.Trail)
            {
                currentEquippedTrail = null;
                RemoveTrail();
            }
            else
            {
                currentEquippedObject = null;
                RemoveObject();
            }

            RefreshShop();
            UpdateSpeedMultiplierText();

            Debug.Log($"[Shop] Unequipped {item.itemId}");
        }

        if (PlayerSpeedLevelManager.instance != null)
            PlayerSpeedLevelManager.instance.UpdateXpMultiplierFromEquipment();
    }

    private void UnequipAll(ShopItemType type)
    {
        var category = type == ShopItemType.Trail ? saves.equipmentShop.trailCategory : saves.equipmentShop.objectCategory;

        foreach (var itemData in category.items)
        {
            itemData.equipped = false;
        }

        if (type == ShopItemType.Trail)
        {
            RemoveTrail();
        }
        else
        {
            RemoveObject();
        }
    }

    private void ApplyTrail(ShopItemSO item)
    {
        if (playerTrail == null)
        {
            Debug.LogError("[Shop] Player TrailRenderer is null! Assign it in inspector.");
            return;
        }

        playerTrail.enabled = true;

        if (item.trailGradient != null)
        {
            playerTrail.colorGradient = item.trailGradient;
        }
    }

    private void RemoveTrail()
    {
        if (playerTrail != null)
        {
            playerTrail.enabled = false;
        }
    }

    private void ApplyObject(ShopItemSO item)
    {
        RemoveObject();

        if (item.objectPrefab != null && objectParent != null)
        {
            var newObj = Instantiate(item.objectPrefab, objectParent);
            newObj.name = $"Object_{item.itemId}";
        }
    }

    private void RemoveObject()
    {
        if (objectParent != null)
        {
            for (int i = objectParent.childCount - 1; i >= 0; i--)
            {
                Destroy(objectParent.GetChild(i).gameObject);
            }
        }
    }

    private void UpdateSpeedMultiplierText()
    {
        if (speedMultiplierText == null) return;

        float totalMultiplier = GetTotalXpMultiplier();
        LanguageDirectory localizedText;

        if (totalMultiplier <= 1)
        {
            localizedText = new LanguageDirectory(
                $"Множитель x0 (След и Аура)",
                $"Multiplier x0 (Trail and Aura)",
                $"Carpan x0 (Iz ve Aura)"
            );
        }
        else
        {
            localizedText = new LanguageDirectory(
                $"Множитель x{totalMultiplier:F1} (След и Аура)",
                $"Multiplier x{totalMultiplier:F1} (Trail and Aura)",
                $"Carpan x{totalMultiplier:F1} (Iz ve Aura)"
);
        }

        speedMultiplierText.text = UTF.ReadLanguageDirectory(localizedText);
    }

    public bool IsEquipped(ShopItemSO item, ShopItemType type)
    {
        var category = type == ShopItemType.Trail ? saves.equipmentShop.trailCategory : saves.equipmentShop.objectCategory;
        var itemData = category.items.FirstOrDefault(x => x.itemId == item.itemId);
        return itemData != null && itemData.equipped;
    }

    public bool IsPurchased(ShopItemSO item, ShopItemType type)
    {
        var category = type == ShopItemType.Trail ? saves.equipmentShop.trailCategory : saves.equipmentShop.objectCategory;
        var itemData = category.items.FirstOrDefault(x => x.itemId == item.itemId);
        return itemData != null && itemData.purchased;
    }

    /// <summary>
    /// Возвращает общий множитель XP от экипированных предметов (След + Аура).
    /// ИСПРАВЛЕНО: множители складываются, а не умножаются
    /// </summary>
    public float GetTotalXpMultiplier()
    {
        // Базовый множитель = 1 (без бонусов)
        float totalMultiplier = 1f;

        // ← ИСПРАВЛЕНО: складываем бонусы, а не умножаем
        // След: x1.5 → +0.5, x2.0 → +1.0
        if (currentEquippedTrail != null)
        {
            totalMultiplier += (currentEquippedTrail.speedMultiplier - 1f);
        }

        // Аура: x1.3 → +0.3, x1.8 → +0.8
        if (currentEquippedObject != null)
        {
            totalMultiplier += (currentEquippedObject.speedMultiplier - 1f);
        }

        return totalMultiplier;
    }
}