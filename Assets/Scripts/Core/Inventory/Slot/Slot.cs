using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    [Header("Slot Data")]
    public int slotId;
    public bool isSelected;
    public bool isQuickInventory;

    [Header("Item_FR Data")]
    public ItemData itemData;
    public Item item;

    [Header("Mutations")]
    //[SerializeField] private SlotMutationGroup mutationGroup;

    [Header("References")]
    private Image _slotImage;
    [SerializeField] private Image _iconView;
    [SerializeField] private TMP_Text _mainInformation;

    [Header("View")]
    [SerializeField] private Sprite _slotDefault;
    [SerializeField] private Sprite _slotSelected;

    [Header("Mutations UI")]
    [SerializeField] private RectTransform _mutationContainer; // Сюда будут падать иконки
    [SerializeField] private Image _mutationIconPrefab;       // Префаб иконки мутации (просто Image)

    [SerializeField] private bool _notAction;

    private GameObject _tutorialFinger;

    private void Awake()
    {
        _slotImage = GetComponent<Image>();
    }

    public void SetData(ItemData _itemData)
    {
        itemData = _itemData;

        item = ItemsManager.GetItemById(_itemData.itemId);

        UpdateSlotView();

        ClearMutationIcons();

        /*
        if (itemData is FishData fishData)
        {
            SpawnMutationIcons(fishData);
        }
        */
    }

    //public void TargetStatus(bool state) => PlayersManager.instance.realPlayer.raycastController.SetRayState(state);

    public void Select()
    {
        // Если слот помечен как быстрый — обрабатываем как быстрый (по slotId)
        if (isQuickInventory)
        {
            Inventory.SelectSlot(slotId);
            return;
        }

        // Для обычных (main) слотов — выбираем через itemData (инвентарь)
        if (itemData != null)
        {
            Inventory.SelectItemInInventory(itemData);
        }
    }


    public void SetSelect(bool select)
    {
        isSelected = select;

        // print(isSelected);

        if (_slotImage != null && _slotSelected != null && _slotDefault != null)
            _slotImage.sprite = isSelected ? _slotSelected : _slotDefault;
    }

    public void UpdateSlotView()
    {
        bool hasItem = item != null;

        _iconView.gameObject.SetActive(hasItem);

        if (!hasItem) return;

        if (item != null)
        {
            if (item.icon != null)
            {
                _iconView.sprite = item.icon;
            }
        }

        /*if (itemData is FishData fishData)
        {
            _mainInformation.text = "[" + fishData.weight.ToString("F1") + UTF.ReadLanguageDirectory(LangList.language.kg) + "]";
            return;
        }*/

        if (itemData is ItemData defaultItem)
        {
            _mainInformation.text = "";

            if (defaultItem.amount > 1)
                _mainInformation.text = "x" + defaultItem.amount;

            return;
        }
    }

    /*
    private void SpawnMutationIcons(FishData fishData)
    {
        if (fishData.appliedMutations == null || _mutationContainer == null) return;

        foreach (string mKey in fishData.appliedMutations)
        {
            Mutation mut = MutationManager.instance.GetMutationByKey(mKey);

            // Если у мутации есть спрайт — создаем иконку
            if (mut != null && mut.sprite != null)
            {
                Image newIcon = Instantiate(_mutationIconPrefab, _mutationContainer);
                newIcon.sprite = mut.sprite;
                // Можно также подкрашивать иконку цветом мутации, если нужно:
                // newIcon.color = mut.textColor; 
            }
        }
    }
    */

    private void ClearMutationIcons()
    {
        if (_mutationContainer == null) return;

        foreach (Transform child in _mutationContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void Clear()
    {
        isSelected = false;
        itemData = null;
        item = null;

        _mainInformation.text = "";

        ClearMutationIcons();

        UpdateSlotView();
    }
}
