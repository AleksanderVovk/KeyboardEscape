using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using YG;
using Random = UnityEngine.Random;

[System.Serializable]
public enum RareType
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Mythical,
    Divine,
    Prismatic
}

[System.Serializable]
public enum ItemType
{
    Default,
    Fish
}

public class Inventory : MonoBehaviour
{
    public static Inventory instance;

    public Slot[] slots; // quick slots
    public List<Slot> existingSlots = new List<Slot>();
    public Slot currentSlot;

    public ItemCategory currentCategory;

    [SerializeField] private Player player;

    public List<ItemData> items = new List<ItemData>();

    public bool inventoryOpen;
    public bool needUpdateInventory;
    public Slot _slotPrefab;
    [SerializeField] private Transform _slotContainer;

    // TILTOOP
    [SerializeField] private GameObject _tooltipPanel;
    [SerializeField] private TMP_Text _tooltipText;
    [SerializeField] private CanvasGroup _tooltipCanvasGroup;
    private Tween _hideTooltipTween;

    [SerializeField] private LanguageDirectory _inventoryFull;

    // Debug
    [SerializeField] private TMP_InputField _inputField;

    [SerializeField] private LanguageDirectory _notEnoughSpace;

    private RainbowGradientText rainbowGradientText;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitializeInventory()
    {
        needUpdateInventory = true;

        LoadInventory();
    }

    public void LoadInventory()
    {
        items = new List<ItemData>();

        var save = YG2.saves.inventory;
        //items.AddRange(FilterValidItems(save.fishingRods));
        //items.AddRange(FilterValidItems(save.fishs));
        items.AddRange(FilterValidItems(save.defaultItem));

        SortingInventory();
    }

    private IEnumerable<ItemData> FilterValidItems<T>(List<T> list) where T : ItemData
    {
        return list?.Where(item => item != null && !item.isNull) ?? Enumerable.Empty<ItemData>();
    }

    public void SortingInventory()
    {
        // Получаем удочку
        //var fishingRod = items.FirstOrDefault(i => i is FishingRodData);

        // Убираем удочку из списка для сортировки, чтобы она осталась на первом месте
        //var otherItems = items.Where(i => !(i is FishingRodData)).ToList();

        // Сортируем остальные предметы по id / типу
        var otherItems = items
            .OrderBy(item => item.itemId)
            //.ThenByDescending(item => item is FishData)
            .ToList();

        items.Clear();

        // Ставим удочку в слот 0
        //if (fishingRod != null)
            //items.Add(fishingRod);

        items.AddRange(otherItems);

        UpdateSlots();
    }


    public void UpdateInventory(int itemCategroyId)
    {
        currentCategory = (ItemCategory)itemCategroyId;

        foreach (Transform child in _slotContainer)
            Destroy(child.gameObject);

        existingSlots.Clear();

        var filteredItems = items.Where(item =>
        {
            var itemInfo = ItemsManager.GetItemById(item.itemId);

            if (itemInfo == null)
                return false;
            if (currentCategory == ItemCategory.None)
                return !itemInfo.hideInMainInventory;

            return !itemInfo.hideInMainInventory && itemInfo.category == currentCategory;
        }).ToList();

        for (int i = 0; i < filteredItems.Count; i++)
        {
            Slot newSlot = Instantiate(_slotPrefab, _slotContainer.transform);
            newSlot.SetData(filteredItems[i]);

            // помечаем, что это слот основного инвентаря (не быстрый)
            newSlot.isQuickInventory = false;
            newSlot.slotId = -1; // явный маркер, что это не быстрый слот

            existingSlots.Add(newSlot);
        }

    }

    public void UpdateSlotByItem(ItemData data)
    {
        foreach (var slot in slots)
        {
            if (slot.itemData == data)
            {
                if (data == null || data.isNull || data.amount <= 0)
                {
                    slot.Clear();
                    slot.gameObject.SetActive(false);
                }
                else
                {
                    slot.SetData(data);
                    slot.gameObject.SetActive(true);
                }
                break;
            }
        }

        UpdateSlotsVisibility();
    }

    private void UpdateSlotsVisibility()
    {
        foreach (var slot in slots)
        {
            slot.gameObject.SetActive(slot.itemData != null && !slot.itemData.isNull);
        }

        // if (slots.All(s => !s.gameObject.activeSelf) && slots.Length > 0)
        // {
        //     slots[0].gameObject.SetActive(true);
        // }
    }

    public static void SelectSlot(int slotId) => instance._SelectSlot(slotId);
    public void _SelectSlot(int slotId)
    {
        if (slotId < 0 || slotId >= slots.Length) return;

        currentSlot = (currentSlot != null && slotId == currentSlot.slotId) ? null : slots[slotId];

        UpdateSlotSelection();
        EquipItem();

        PlayCarryAnimation(currentSlot != null);

        ShowTooltip(true);
    }

    public void RemoveItemFromHand()
    {
        currentSlot = null;
        EquipItem(); // это удалит все объекты из руки
        PlayCarryAnimation(false); // если хочешь анимацию "пустые руки"
    }

    private void PlayCarryAnimation(bool isCarrying)
    {
        player.animator.SetBool("isCarry", isCarrying);
        if (isCarrying)
        {
            player.animator.Play("Empty");
            player.animator.SetTrigger("Carry");
        }
    }

    public void UpdateSelectedSlotVisuals()
    {
        UpdateSlotSelection();
        EquipItem();
        PlayCarryAnimation(currentSlot != null);
    }

    public static void SelectItemInInventory(ItemData itemData) => instance._SelectItemInInventory(itemData);
    public void _SelectItemInInventory(ItemData itemData)
    {
        UpdateInventory(0);

        // Логика для быстрого слота
        Slot quickSlot = slots.FirstOrDefault(s => s?.itemData == itemData);
        if (quickSlot != null)
        {
            currentSlot?.SetSelect(false);
            currentSlot = quickSlot;
            currentSlot.SetSelect(true);

            UpdateSelectedSlotVisuals();
            ShowTooltip(true); // Здесь вызов был
            return;
        }

        // Логика для основного инвентаря
        Slot inventorySlot = existingSlots?.FirstOrDefault(s => s?.itemData == itemData);
        if (inventorySlot != null)
        {
            currentSlot?.SetSelect(false);
            currentSlot = inventorySlot;
            currentSlot.SetSelect(true);

            UpdateSelectedSlotVisuals();

            // ✅ ДОБАВЬТЕ ЭТУ СТРОКУ:
            ShowTooltip(true);

            return;
        }
    }




    private void UpdateSlotSelection()
    {
        foreach (var slot in slots)
        {
            slot.SetSelect(currentSlot != null && slot == currentSlot);
        }

        foreach (var slot in existingSlots)
        {
            if (!slot.gameObject.activeSelf) continue;
            slot.SetSelect(currentSlot != null && slot == currentSlot);
        }
    }

    private bool IsQuickSlot(Slot slot)
    {
        foreach (var s in slots)
        {
            if (s == slot) return true;
        }
        return false;
    }

    public bool CheckInventoryFull()
    {
        if (items.Count >= 100)
        {
            NotificationManager.ShowNotification(UTF.ReadLanguageDirectory(_inventoryFull));
            return true;
        }
        else
        {
            return false;
        }
    }

    public void OpenInventory(bool state)
    {
        inventoryOpen = state;
        if (state && needUpdateInventory) UpdateInventory((int)currentCategory);
    }

    private void SetTooltipTextGradient(RareType rareType)
    {
        if (rareType == RareType.Prismatic)
        {
            if (rainbowGradientText == null)
            {
                rainbowGradientText = _tooltipText.AddComponent<RainbowGradientText>();
                rainbowGradientText.cycleSpeed = 0.5f;
            }
        }
        else if (rainbowGradientText != null)
        {
            Destroy(rainbowGradientText);
        }
    }

    public void ShowTooltip(bool state)
    {
        if (_tooltipCanvasGroup == null && _tooltipPanel != null)
            _tooltipCanvasGroup = _tooltipPanel.GetComponent<CanvasGroup>();

        if (currentSlot == null || currentSlot.item == null || _tooltipText == null)
        {
            if (_tooltipPanel != null) _tooltipPanel.SetActive(false);
            return;
        }

        // 1. ПОЛУЧАЕМ ЦВЕТ РЕДКОСТИ ИЗ ITEMSMANAGER
        Color rareColor = ItemsManager.GetRareColors(currentSlot.item.rareType);
        string rareHex = UnityEngine.ColorUtility.ToHtmlStringRGB(rareColor);

        // Формируем раскрашенное название предмета
        string itemNameLocalized = UTF.ReadLanguageDirectory(currentSlot.item.itemName);
        string coloredBaseName = $"<color=#{rareHex}>{itemNameLocalized}</color>";

        // 2. ФОРМИРУЕМ ТЕКСТ МУТАЦИЙ (как было раньше)

        // Итоговый текст: [Цветная Редкость] Название (Цветные Мутации)
        _tooltipText.text = coloredBaseName;

        // --- ЛОГИКА ГРАДИЕНТА (ДЛЯ PRISMATIC) ---
        // Если предмет Prismatic, то цвет из <color> может перекрываться градиентом,
        // но для остальных типов редкости это будет работать идеально.
        if (currentSlot.item.category == ItemCategory.None || currentSlot.item.id == "16")
        {
            if (rainbowGradientText != null) Destroy(rainbowGradientText);
        }
        else
        {
            SetTooltipTextGradient(currentSlot.item.rareType);
        }

        // --- АНИМАЦИЯ ПОЯВЛЕНИЯ ---
        _tooltipPanel.SetActive(true);
        _tooltipPanel.transform.DOKill();
        if (_tooltipCanvasGroup != null) _tooltipCanvasGroup.DOKill();
        if (_hideTooltipTween != null) _hideTooltipTween.Kill();

        if (_tooltipCanvasGroup != null) _tooltipCanvasGroup.alpha = 1f;
        _tooltipPanel.transform.localScale = Vector3.one * 0.8f;
        _tooltipPanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

        _hideTooltipTween = DOVirtual.DelayedCall(3f, () =>
        {
            if (_tooltipCanvasGroup != null)
            {
                _tooltipCanvasGroup.DOFade(0f, 1f).OnComplete(() => _tooltipPanel.SetActive(false));
            }
            else _tooltipPanel.SetActive(false);
        });
    }


    public static void SaveInventory() => instance._SaveInventory();
    public void _SaveInventory()
    {
        var saveInventory = YG2.saves.inventory;

        saveInventory.defaultItem.Clear();

        foreach (var item in items)
        {
            if (item == null || item.isNull) continue;

            switch (item)
            {
                /*
                case FishingRodData fishingRod:
                    saveInventory.fishingRods.Add(new FishingRodData(fishingRod));
                    Debug.Log("Added in save: " + fishingRod.itemId + " (fishingRodData)");
                    break;

                case FishData fish:
                    saveInventory.fishs.Add(new FishData(fish));
                    Debug.Log("Added in save: " + fish.itemId + " (fishData)");
                    break;
                */
                default:
                    saveInventory.defaultItem.Add(item);
                    Debug.Log("Added in save: " + item.itemId + " (defaultItem)");
                    break;
            }
        }

        Debug.Log("Inventory Save");

        if (needUpdateInventory && inventoryOpen)
            UpdateInventory((int)currentCategory);

        YG2.SaveProgress();
    }

    public static void AddItem(ItemData newItem, bool needSave, bool addAtFirst = false) => instance?._AddItem(newItem, needSave, addAtFirst);
    public void _AddItem(ItemData newItem, bool needSave, bool addAtFirst = false)
    {
        if (newItem == null || newItem.isNull) return;

        if (items == null) return;

        Debug.Log("Add to inventory: " + newItem);

        newItem.isNull = false;
        ItemData itemToAdd = CreateCopy(newItem);

        //bool stackable = itemToAdd is SeedData || (itemToAdd is not HarvestedVegetable && itemToAdd is not PetData);
        bool stackable = false;

        ItemData existing = items.FirstOrDefault(i => i?.itemId == itemToAdd?.itemId && i?.GetType() == itemToAdd?.GetType());

        if (stackable && existing != null)
        {
            existing.amount += itemToAdd.amount;
            UpdateSlotByItem(existing);
        }
        else
        {
            if (addAtFirst && items.Count > 0)
                items.Insert(0, itemToAdd);
            else
                items.Add(itemToAdd);

            SortingInventory();
            UpdateSlots();
        }

        if (needSave) _SaveInventory();
    }

    /*public void SetItemToQuickSlot(Slot)
    {

    }*/

    public static void AddItemById(string id, int amount) => instance._AddItemById(id, amount);
    public void _AddItemById(string id, int amount)
    {
        if (string.IsNullOrEmpty(id) || amount <= 0) return;

        Item baseItem = ItemsManager.GetItemById(id);

        ItemData newItemData = null;

        switch (baseItem)
        {
            /*
            case Brainrot brainrot:
                newItemData = new BrainrotData(id, amount, Random.Range(0.1f, 5f), false);

                Debug.Log("[Inventory] Add BrainrotData");
                break;
            case Fish fish:
                newItemData = new FishData(id, amount, Random.Range(0.1f, 5f), false);
                break;
            */
            default:
                newItemData = new ItemData
                {
                    isNull = false,
                    itemId = id,
                    amount = amount
                };

                Debug.Log("[Inventory] Add Item_FR");
                break;
        }

        _AddItem(newItemData, true);
    }

    private ItemData CreateCopy(ItemData item)
    {
        switch (item)
        {
            /*
            case FishingRodData rod:
                return new FishingRodData(rod);

            case FishData fish:
                return new FishData(fish);
            */
            default:
                return new ItemData
                {
                    itemId = item.itemId,
                    amount = item.amount,
                    uniqueId = item.uniqueId,
                    isNull = item.isNull
                };
        }
    }

    private bool _isUpdatingSlotsWithoutTooltip = false;
    public void UpdateSlots()
    {
        _isUpdatingSlotsWithoutTooltip = true;

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].slotId = i;
            slots[i].isQuickInventory = true;

            if (i < items.Count && items[i] != null)
            {
                // Если слот 0 — всегда удочка
                /*
                if (i == 0 && !(items[i] is FishingRodData))
                {
                    slots[i].Clear();
                    slots[i].gameObject.SetActive(false);
                }
                else
                {
                    slots[i].SetData(items[i]);
                    slots[i].gameObject.SetActive(true);
                }
                */

                slots[i].SetData(items[i]);
                slots[i].gameObject.SetActive(true);
            }
            else
            {
                slots[i].Clear();
                slots[i].gameObject.SetActive(false);
            }
        }

        if (currentSlot != null && (currentSlot.itemData == null || !items.Contains(currentSlot.itemData)))
            currentSlot = FindNearestNonEmptySlot(System.Array.IndexOf(slots, currentSlot));

        UpdateSlotSelection();
        EquipItem();
        AssignUniqueIds();

        _isUpdatingSlotsWithoutTooltip = false;
    }



    private void AssignUniqueIds()
    {
        for (int i = 0; i < items.Count; i++)
            items[i].uniqueId = i;
    }

    public static void SubstractionItem(ItemData itemData) => instance._SubstractionItem(itemData);
    private void _SubstractionItem(ItemData itemData)
    {
        if (itemData == null) return;

        bool wasSelected = currentSlot?.itemData == itemData;
        int selectedIndex = wasSelected ? System.Array.IndexOf(slots, currentSlot) : -1;

        if (itemData.amount > 1)
        {
            itemData.amount--;

            UpdateSlotByItem(itemData);
        }
        else
        {
            RemoveItem(itemData, false);
        }

        UpdateSlots();
        _SaveInventory();

        if (wasSelected)
        {
            if (selectedIndex >= 0 && selectedIndex < slots.Length &&
                slots[selectedIndex].itemData != null)
            {
                currentSlot = slots[selectedIndex];
            }
            else
            {
                currentSlot = FindNearestNonEmptySlot(selectedIndex);
            }
        }

        UpdateSelectedSlotVisuals();
    }

    private Slot FindNearestNonEmptySlot(int startIndex)
    {
        if (startIndex < 0 || startIndex >= slots.Length)
            startIndex = 0;

        // Проверяем слоты справа от стартового

        /*
        for (int i = startIndex; i < slots.Length; i++)
        {
            if (slots[i].itemData != null && !toolsId.Contains(slots[i].itemData.itemId))
                return slots[i];
        }

        // Проверяем слоты слева от стартового
        for (int i = startIndex - 1; i >= 0; i--)
        {
            if (slots[i].itemData != null && !toolsId.Contains(slots[i].itemData.itemId))
                return slots[i];
        }
        */

        for (int i = startIndex - 1; i >= 0; i--)
        {
            if (slots[i].itemData != null)
                return slots[i];
        }

        // Если ничего не найдено, возвращаем первый слот (даже если он пустой)
        return null;
    }

    private void ClearSlot(ItemData itemData)
    {
        foreach (var slot in slots)
        {
            if (slot.itemData == itemData)
            {
                slot.Clear();
                slot.gameObject.SetActive(false);
                break;
            }
        }
    }

    public static void RemoveItem(ItemData itemData, bool updateInventory) => instance._RemoveItem(itemData, updateInventory);
    public void _RemoveItem(ItemData itemData, bool updateInventory)
    {
        if (itemData == null) return;

        items.Remove(itemData);
        ClearSlot(itemData);

        RemoveFromSave(itemData);

        if (updateInventory)
        {
            UpdateSlots();
            currentSlot = null;
            UpdateSelectedSlotVisuals();
            SaveInventory();
        }
    }

    private void RemoveFromSave(ItemData item)
    {
        /*
        var inv = Saves.Instance.Save.inventory;

        inv.defaultItem.Remove(item);
        if (item is HarvestedVegetable veg) inv.harvestedVegetables.Remove(veg);
        else if (item is SeedData seed) inv.seeds.Remove(seed);
        else if (item is EggData egg) inv.eggs.Remove(egg);
        else if (item is PetData pet) inv.pets.Remove(pet);
        else if (item is DecorationObject dec) inv.decorations.Remove(dec);
        */
    }

    private void EquipItem()
    {
        foreach (Transform child in player.hand)
            Destroy(child.gameObject);

        if (currentSlot.item.handPrefab != null)
        {
            // 1. Спавним саму рыбу (или предмет) в руку
            GameObject handItem = Instantiate(currentSlot.item.handPrefab, player.hand, true);
            handItem.transform.localPosition = Vector3.zero;
            handItem.transform.localRotation = Quaternion.identity;

            // 2. Если это рыба, применяем масштаб и мутации
            /*
            if (currentSlot.itemData is FishData fish)
            {
                handItem.transform.localScale *= fish.weight;

                // ✅ ЛОГИКА СПАВНА ЧАСТИЦ МУТАЦИИ
                ApplyFishMutationEffects(fish, handItem.transform);
            }
            */
        }
    }

    /*
    private void ApplyFishMutationEffects(FishData fishData, Transform fishTransform)
    {
        if (fishData.appliedMutations == null || fishData.appliedMutations.Count == 0) return;

        Mutation bestMutation = null;
        float maxMultiplier = -1f;

        // 1. Ищем мутацию с самым большим priceMultiplier
        foreach (string mKey in fishData.appliedMutations)
        {
            Mutation mut = MutationManager.instance.GetMutationByKey(mKey);
            if (mut != null && mut.priceMultiplier > maxMultiplier)
            {
                maxMultiplier = mut.priceMultiplier;
                bestMutation = mut;
            }
        }

        if (bestMutation == null) return;

        // 2. ПРИМЕНЕНИЕ МАТЕРИАЛА
        if (bestMutation.material != null)
        {
            Renderer[] renderers = fishTransform.GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in renderers)
            {
                rend.material = bestMutation.material;
            }
        }

        // 3. СПАВН ЧАСТИЦ С КОРРЕКЦИЕЙ ЧЕРЕЗ TRANSFORM
        if (bestMutation.particleSystem != null)
        {
            ParticleSystem ps = Instantiate(bestMutation.particleSystem, fishTransform);
            ps.transform.localPosition = Vector3.zero;
            ps.transform.localRotation = Quaternion.identity;

            // Так как рыба уже имеет масштаб (например, 2.0), 
            // чтобы частицы были в два раза меньше рыбы, 
            // их ЛОКАЛЬНЫЙ масштаб относительно рыбы должен быть 0.5.
            ps.transform.localScale = Vector3.one * 0.5f;

            var main = ps.main;

            // Устанавливаем режим масштабирования Hierarchy, 
            // чтобы частицы корректно учитывали localScale своего объекта и рыбы.
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            // Если в префабе частицы настроены как "мировые" (Simulation Space = World),
            // то изменение масштаба через Transform может не сработать для размера самих искр.
            // В таком случае раскомментируйте строку ниже:
            // main.startSizeMultiplier = 1.0f; 

            ps.Play();
        }

    }
    */

    public static ItemType DefineTypeItem(Item _item) => instance._DefineTypeItem(_item);
    private ItemType _DefineTypeItem(Item _item)
    {
        /*
        if (_item == null) return ItemType.Default;
        if (_item is Seed) return ItemType.Seed;
        if (_item is Vegetable) return ItemType.Vegetable;
        if (_item is SprinklerItem) return ItemType.Sprinkler;
        if (_item is Egg) return ItemType.Egg;
        if (_item is Decoration) return ItemType.Decoration;
        */
        return ItemType.Default;
    }

    private void Update()
    {
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                _SelectSlot(i);
                break;
            }
        }
    }

    public static bool CheckFreeSlots(int count) => instance._CheckFreeSlots(count);
    private bool _CheckFreeSlots(int count)
    {
        bool value = (items.Count + count) <= 100;

        if (!value)
            NotificationManager.ShowNotification(UTF.ReadLanguageDirectory(_notEnoughSpace));

        return value;
    }
}