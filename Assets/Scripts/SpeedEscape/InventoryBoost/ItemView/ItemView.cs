using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

public class ItemView : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text nameText;
    public Image iconImage;
    public TMP_Text speedBonusText;
    public Image backgroundImage;

    [Header("Colors by Rarity")]
    public Color commonColor = new Color(0.7f, 0.7f, 0.7f);
    public Color uncommonColor = new Color(0.4f, 0.8f, 0.4f);
    public Color rareColor = new Color(0.4f, 0.4f, 1f);
    public Color epicColor = new Color(0.8f, 0.4f, 1f);
    public Color legendaryColor = new Color(1f, 0.8f, 0.4f);

    private ItemDefinition itemDefinition;
    private InventoryBoostUI inventoryUI;
    private bool isEquipped;

    public void Initialize(ItemDefinition definition, InventoryBoostUI inventoryUIInstance, bool equipped)
    {
        itemDefinition = definition;
        inventoryUI = inventoryUIInstance;
        isEquipped = equipped;

        // Заполняем данные
        nameText.text = UTF.ReadLanguageDirectory(definition.itemName);
        iconImage.sprite = definition.icon;
        speedBonusText.text = $"+{definition.speedBonusPercent}%";

        // Цвет фона по редкости
        backgroundImage.color = GetRarityColor(definition.rarity);

        Debug.Log($"[ItemView] Initialized: {definition.itemName}, Equipped: {isEquipped}");
    }

    private Color GetRarityColor(RareType rarity)
    {
        switch (rarity)
        {
            case RareType.Common: return commonColor;
            case RareType.Uncommon: return uncommonColor;
            case RareType.Rare: return rareColor;
            case RareType.Epic: return epicColor;
            case RareType.Legendary: return legendaryColor;
            default: return commonColor;
        }
    }

    public void OnClick()
    {
        Debug.Log($"[ItemView] OnClick called! Item: {itemDefinition.itemName}, IsEquipped: {isEquipped}");

        if (inventoryUI == null)
        {
            Debug.LogError("[ItemView] inventoryUI is null!");
            return;
        }

        if (itemDefinition == null)
        {
            Debug.LogError("[ItemView] itemDefinition is null!");
            return;
        }

        if (isEquipped)
        {
            Debug.Log($"[ItemView] Trying to unequip: {itemDefinition.itemId}");
            // Снять предмет
            inventoryUI.UnequipItem(itemDefinition);
        }
        else
        {
            Debug.Log($"[ItemView] Trying to equip: {itemDefinition.itemId}");
            // Экипировать предмет
            inventoryUI.EquipItem(itemDefinition);
        }
    }
}