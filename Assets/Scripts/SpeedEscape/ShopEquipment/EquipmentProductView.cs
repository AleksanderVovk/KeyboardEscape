using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

public class EquipmentProductView : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text nameText;
    public TMP_Text speedMultiplierText;
    public Image iconImage;
    public Image backgroundImage;

    [Header("Buttons")]
    public Button buyWinsButton;
    public Button buyDiamondsButton;
    public Button actionButton;  // Одеть/Снять

    [Header("Button Text")]
    public TMP_Text buyWinsText;
    public TMP_Text buyDiamondsText;
    public TMP_Text actionButtonText;

    [Header("Button Backgrounds")]
    public Image actionButtonBackground;

    [Header("Colors")]
    public Color equippedColor = new Color(0.2f, 0.8f, 0.2f);
    public Color normalColor = Color.white;

    [Header("Text")]
    public LanguageDirectory equipText = new LanguageDirectory("НАДЕТЬ", "EQUIP", "GİY");
    public LanguageDirectory unequipText = new LanguageDirectory("СНЯТЬ", "UNEQUIP", "ÇIKAR");

    private ShopItemSO item;
    private EquipmentShopUI shopUI;
    private int itemIndex;
    private ShopItemType itemType;

    public void Initialize(ShopItemSO shopItem, EquipmentShopUI shopUIInstance, int index, ShopItemType type)
    {
        item = shopItem;
        shopUI = shopUIInstance;
        itemIndex = index;
        itemType = type;

        nameText.text = UTF.ReadLanguageDirectory(item.itemName);
        iconImage.sprite = item.icon;

        speedMultiplierText.text = $"x{item.speedMultiplier} {UTF.ReadLanguageDirectory(new LanguageDirectory("Повышение скорости", "Speed Boost", "Hız Artışı"))}";

        // Цены на кнопках
        buyWinsText.text = item.priceWins > 0 ? UTF.AbbreviateNumber(item.priceWins) : "FREE";
        buyDiamondsText.text = item.priceDiamonds > 0 ? UTF.AbbreviateNumber(item.priceDiamonds) : "FREE";

        // Подписка на кнопки
        buyWinsButton.onClick.RemoveAllListeners();
        buyWinsButton.onClick.AddListener(() => BuyForWins());

        buyDiamondsButton.onClick.RemoveAllListeners();
        buyDiamondsButton.onClick.AddListener(() => BuyForDiamonds());

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(() => OnActionButtonClicked());
            
        if (item.backgroundSprite != null)
            backgroundImage.sprite = item.backgroundSprite;
        else
            backgroundImage.color = item.backgroundColor;

        UpdateButtons();
    }

    private void BuyForWins()
    {
        shopUI.BuyItem(item, itemType, ValueType.Wins, item.priceWins);
    }

    private void BuyForDiamonds()
    {
        shopUI.BuyItem(item, itemType, ValueType.Diamond, item.priceDiamonds);
    }

    private void OnActionButtonClicked()
    {
        bool equipped = shopUI.IsEquipped(item, itemType);

        if (equipped)
        {
            shopUI.UnequipItem(item, itemType);
        }
        else
        {
            shopUI.EquipItem(item, itemType);
        }
    }

    public void UpdateButtons()
    {
        bool purchased = shopUI.IsPurchased(item, itemType);
        bool equipped = shopUI.IsEquipped(item, itemType);

        // Показываем/скрываем кнопки
        buyWinsButton.gameObject.SetActive(!purchased);
        buyDiamondsButton.gameObject.SetActive(!purchased);
        actionButton.gameObject.SetActive(purchased);

        if (purchased)
        {
            if (equipped)
            {
                actionButtonText.text = UTF.ReadLanguageDirectory(unequipText);
                actionButtonBackground.color = equippedColor;
            }
            else
            {
                actionButtonText.text = UTF.ReadLanguageDirectory(equipText);
                actionButtonBackground.color = normalColor;
            }
        }
    }

    private void OnEnable()
    {
        if (item != null && shopUI != null)
        {
            UpdateButtons();
        }
    }
}