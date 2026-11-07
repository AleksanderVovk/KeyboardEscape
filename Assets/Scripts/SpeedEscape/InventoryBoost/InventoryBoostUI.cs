using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

public class InventoryBoostUI : MonoBehaviour
{
    [Header("References")]
    public Transform storageContainer;      // Правая колонка - Склад
    public Transform equippedContainer;     // Левая колонка - Экипировано
    public ItemView itemViewPrefab;         // Префаб ItemView
    public TMP_Text totalSpeedText;         // Текст с общим % скорости
    public TMP_Text equipmentSpeedText;     // ← НОВЫЙ текст: "+0% Скорость (Предметы)"
    public TMP_Text equippedCountText;      // Текст "Экипировано X/10"
    public Button unequipAllButton;         // Кнопка "Снять все"
    public Button equipBestButton;          // Кнопка "Одеть лучшие"

    [Header("Settings")]
    public int maxEquippedItems = 10;

    [Header("Notifications")]
    public LanguageDirectory notifyMaxEquipped = new LanguageDirectory(
        "Экипировано максимум предметов",
        "Maximum items equipped",
        "Maksimum esya donatildi"
    );

    public LanguageDirectory notifyInventoryEmpty = new LanguageDirectory(
        "Инвентарь пуст",
        "Inventory is empty",
        "Envanter bos"
    );

    private SavesYG saves;
    private Dictionary<string, ItemDefinition> itemsDatabase;

    // ← Кэшируем последнее значение скорости, чтобы обновлять текст только при изменении
    private float lastEquipmentSpeedPercent = float.MinValue;

    private void Awake()
    {
        // Загружаем базу предметов (все ScriptableObject ItemDefinition из Resources/Items)
        itemsDatabase = Resources.LoadAll<ItemDefinition>("GameData/ItemsBoost")
            .ToDictionary(item => item.itemId);

        // Получаем сохранения
        saves = YG2.saves;
    }

    private void Start()
    {
        // Подписываем кнопки
        unequipAllButton.onClick.AddListener(UnequipAllItems);
        equipBestButton.onClick.AddListener(EquipBestItems);

        RefreshInventory();
    }

    /// <summary>
    /// Обновить весь инвентарь из сохранений
    /// </summary>
    public void RefreshInventory()
    {
        // Очищаем контейнеры
        foreach (Transform child in storageContainer)
            Destroy(child.gameObject);
        foreach (Transform child in equippedContainer)
            Destroy(child.gameObject);

        // Создаём предметы на складе
        foreach (var itemBoost in saves.inventory.storage)
        {
            if (itemsDatabase.TryGetValue(itemBoost.itemId, out var definition))
            {
                CreateItemView(definition, storageContainer, false);
            }
        }

        // Создаём экипированные предметы
        foreach (var itemBoost in saves.inventory.equipped)
        {
            if (itemsDatabase.TryGetValue(itemBoost.itemId, out var definition))
            {
                CreateItemView(definition, equippedContainer, true);
            }
        }

        UpdateTotalSpeed();
        UpdateEquipmentSpeedText();  // ← Обновляем текст скорости только здесь
        UpdateEquippedCount();
    }

    /// <summary>
    /// Создать ItemView в нужном контейнере
    /// </summary>
    private ItemView CreateItemView(ItemDefinition definition, Transform container, bool equipped)
    {
        var itemView = Instantiate(itemViewPrefab, container);
        itemView.Initialize(definition, this, equipped);
        return itemView;
    }

    /// <summary>
    /// Экипировать предмет
    /// </summary>
    public void EquipItem(ItemDefinition definition)
    {
        Debug.Log($"[Inventory] EquipItem called: {definition.itemId}");
        Debug.Log($"[Inventory] Equipped count: {saves.inventory.equipped.Count}, Max: {maxEquippedItems}");
        Debug.Log($"[Inventory] Storage count: {saves.inventory.storage.Count}");

        // Проверка на максимум
        if (saves.inventory.equipped.Count >= maxEquippedItems)
        {
            Debug.LogWarning("[Inventory] Max items equipped!");
            NotificationManager.ShowNotification(UTF.ReadLanguageDirectory(notifyMaxEquipped), NotificationType.Error);
            return;
        }

        // Проверка: есть ли такой предмет на складе
        var itemInStorage = saves.inventory.storage.FirstOrDefault(i => i.itemId == definition.itemId);
        if (itemInStorage == null)
        {
            Debug.LogWarning($"[Inventory] Item {definition.itemId} not found in storage!");
            Debug.LogWarning($"[Inventory] Storage items: {string.Join(", ", saves.inventory.storage.Select(i => i.itemId))}");
            return;
        }

        Debug.Log($"[Inventory] Found item in storage, removing...");

        // Удаляем из склада
        saves.inventory.storage.Remove(itemInStorage);

        // Добавляем в экипировку (только ID)
        saves.inventory.equipped.Add(new ItemBoost(definition.itemId));

        Debug.Log($"[Inventory] Item equipped, new storage count: {saves.inventory.storage.Count}, equipped count: {saves.inventory.equipped.Count}");

        // Сохраняем
        YG2.SaveProgress();

        // Обновляем UI
        RefreshInventory();
    }

    /// <summary>
    /// Снять предмет
    /// </summary>
    public void UnequipItem(ItemDefinition definition)
    {
        Debug.Log($"[Inventory] UnequipItem called: {definition.itemId}");
        Debug.Log($"[Inventory] Equipped count: {saves.inventory.equipped.Count}");

        // Находим в экипировке
        var equippedItem = saves.inventory.equipped.FirstOrDefault(i => i.itemId == definition.itemId);
        if (equippedItem == null)
        {
            Debug.LogWarning($"[Inventory] Item {definition.itemId} not found in equipped!");
            Debug.LogWarning($"[Inventory] Equipped items: {string.Join(", ", saves.inventory.equipped.Select(i => i.itemId))}");
            return;
        }

        Debug.Log($"[Inventory] Found item in equipped, removing...");

        // Удаляем из экипировки
        saves.inventory.equipped.Remove(equippedItem);

        // Добавляем на склад
        saves.inventory.storage.Add(new ItemBoost(definition.itemId));

        Debug.Log($"[Inventory] Item unequipped, new storage count: {saves.inventory.storage.Count}, equipped count: {saves.inventory.equipped.Count}");

        // Сохраняем
        YG2.SaveProgress();

        // Обновляем UI
        RefreshInventory();
    }

    /// <summary>
    /// Подсчитать общий % скорости от экипировки (для PlayerSpeedLevelManager)
    /// </summary>
    public void UpdateTotalSpeed()
    {
        float totalSpeedPercent = CalculateEquipmentSpeedPercent();

        totalSpeedText.text = $"+{totalSpeedPercent}% {UTF.ReadLanguageDirectory(new LanguageDirectory("Скорости", "Speed", "Hız"))}";

        // ← ИСПРАВЛЕНО: вызываем SetInventoryXpBonus, а не SetEquipmentXpBonus
        if (PlayerSpeedLevelManager.instance != null)
            PlayerSpeedLevelManager.instance.SetInventoryXpBonus(totalSpeedPercent);

        Debug.Log($"[Inventory] Total speed bonus: {totalSpeedPercent}%");
    }

    /// <summary>
    /// Подсчитать % скорости от экипировки
    /// </summary>
    private float CalculateEquipmentSpeedPercent()
    {
        float totalSpeedPercent = 0f;

        foreach (var itemBoost in saves.inventory.equipped)
        {
            if (itemsDatabase.TryGetValue(itemBoost.itemId, out var definition))
            {
                totalSpeedPercent += definition.speedBonusPercent;
            }
        }

        return totalSpeedPercent;
    }

    /// <summary>
    /// Обновить текст скорости от предметов (оптимизировано: только при изменении значения)
    /// </summary>
    public void UpdateEquipmentSpeedText()
    {
        float currentSpeedPercent = CalculateEquipmentSpeedPercent();

        // ← Обновляем текст только если значение изменилось
        if (!Mathf.Approximately(currentSpeedPercent, lastEquipmentSpeedPercent))
        {
            lastEquipmentSpeedPercent = currentSpeedPercent;

            // Форматируем как "+0% Скорость (Предметы)" или "+25% Скорость (Предметы)"
            if (currentSpeedPercent <= 0)
            {
                equipmentSpeedText.text = $"+0% {UTF.ReadLanguageDirectory(new LanguageDirectory("Скорость (Предметы)", "Speed (Items)", "Hız (Eşyalar)"))}";
            }
            else
            {
                equipmentSpeedText.text = $"+{currentSpeedPercent}% {UTF.ReadLanguageDirectory(new LanguageDirectory("Скорость (Предметы)", "Speed (Items)", "Hız (Eşyalar)"))}";
            }

            Debug.Log($"[Inventory] Equipment speed text updated: +{currentSpeedPercent}%");
        }
        else
        {
            Debug.Log($"[Inventory] Equipment speed unchanged, skipping text update: {currentSpeedPercent}%");
        }
    }

    /// <summary>
    /// Обновить счётчик экипированных предметов
    /// </summary>
    public void UpdateEquippedCount()
    {
        int count = saves.inventory.equipped.Count;
        equippedCountText.text = $"{UTF.ReadLanguageDirectory(new LanguageDirectory("Экипировано", "Equipped", "Donatildi"))} {count}/{maxEquippedItems}";
    }

    /// <summary>
    /// Добавить предмет по ID (например, из магазина или награды)
    /// </summary>
    public void AddItemById(string itemId)
    {
        if (!itemsDatabase.TryGetValue(itemId, out var definition))
        {
            Debug.LogError($"Предмет с ID {itemId} не найден!");
            return;
        }

        // Добавляем на склад
        saves.inventory.storage.Add(new ItemBoost(itemId));

        // Сохраняем
        YG2.SaveProgress();

        // Обновляем UI
        RefreshInventory();
    }

    /// <summary>
    /// Снять все предметы
    /// </summary>
    public void UnequipAllItems()
    {
        if (saves.inventory.equipped.Count == 0)
            return;

        // Перемещаем все предметы из экипировки на склад
        foreach (var equippedItem in saves.inventory.equipped.ToList())
        {
            saves.inventory.storage.Add(new ItemBoost(equippedItem.itemId));
        }

        saves.inventory.equipped.Clear();

        // Сохраняем
        YG2.SaveProgress();

        // Обновляем UI
        RefreshInventory();
    }

    /// <summary>
    /// Одеть лучшие предметы (по максимальному % скорости)
    /// </summary>
    public void EquipBestItems()
    {
        // Проверяем, есть ли предметы на складе
        if (saves.inventory.storage.Count == 0)
        {
            NotificationManager.ShowNotification(UTF.ReadLanguageDirectory(notifyInventoryEmpty), NotificationType.Error);
            return;
        }

        // Сортируем предметы на складе по скорости (по убыванию)
        var sortedItems = saves.inventory.storage
            .Select(item => new { Item = item, Definition = itemsDatabase[item.itemId] })
            .OrderByDescending(x => x.Definition.speedBonusPercent)
            .ToList();

        // Считаем, сколько ещё можно одеть
        int availableSlots = maxEquippedItems - saves.inventory.equipped.Count;

        if (availableSlots <= 0)
        {
            NotificationManager.ShowNotification(UTF.ReadLanguageDirectory(notifyMaxEquipped), NotificationType.Error);
            return;
        }

        // Берём топ N предметов
        int itemsToEquip = Mathf.Min(availableSlots, sortedItems.Count);

        for (int i = 0; i < itemsToEquip; i++)
        {
            var itemToEquip = sortedItems[i].Item;

            // Удаляем из склада
            saves.inventory.storage.Remove(itemToEquip);

            // Добавляем в экипировку
            saves.inventory.equipped.Add(new ItemBoost(itemToEquip.itemId));
        }

        // Сохраняем
        YG2.SaveProgress();

        // Обновляем UI
        RefreshInventory();
    }
}