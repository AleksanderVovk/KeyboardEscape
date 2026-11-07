using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

public class ItemBoostProductView : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text rarityText;
    public TMP_Text nameText;
    public TMP_Text countText;
    public TMP_Text speedBonusText;
    public TMP_Text priceText;
    public TMP_Text priceDiamondText;
    public Image iconImage;
    public Image backgroundImage;
    public Button buyButton;
    public Button buyForDiamonds;

    [Header("Rarity Backgrounds")]
    public Sprite commonBackground;
    public Sprite uncommonBackground;
    public Sprite rareBackground;
    public Sprite epicBackground;
    public Sprite legendaryBackground;

    [Header("Rarity Colors")]
    public Color commonColor = new Color(0.7f, 0.7f, 0.7f);
    public Color uncommonColor = new Color(0.4f, 0.8f, 0.4f);
    public Color rareColor = new Color(0.4f, 0.4f, 1f);
    public Color epicColor = new Color(0.8f, 0.4f, 1f);
    public Color legendaryColor = new Color(1f, 0.8f, 0.4f);

    private ItemDefinition itemDefinition;
    private ItemBoostShopUI shopUI;
    private int itemIndex;

    public void Initialize(ItemDefinition definition, ItemBoostShopUI shopUIInstance, int index)
    {
        itemDefinition = definition;
        shopUI = shopUIInstance;
        itemIndex = index;

        rarityText.text = UTF.ReadLanguageDirectory(GetRarityName(definition.rarity));
        rarityText.color = GetRarityColor(definition.rarity);

        nameText.text = UTF.ReadLanguageDirectory(definition.itemName);

        iconImage.sprite = definition.icon;

        backgroundImage.sprite = GetRarityBackground(definition.rarity);

        speedBonusText.text = $"+{definition.speedBonusPercent}% {UTF.ReadLanguageDirectory(new LanguageDirectory("Скорости", "Speed", "Hız"))}";
        speedBonusText.color = GetRarityColor(definition.rarity);

        priceText.text = UTF.AbbreviateNumber(definition.price.amount);
        priceDiamondText.text = UTF.AbbreviateNumber(definition.priceDiamonds);

        UpdateCountText();

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => BuyForWin(itemIndex));

        buyForDiamonds.onClick.RemoveAllListeners();
        buyForDiamonds.onClick.AddListener(() => BuyForDiamond(itemIndex));
    }

    public void UpdateCountText()
    {
        var shopItem = YG2.saves.shop.items[itemIndex];
        var config = shopUI.slotConfigs[itemIndex];
        countText.text = $"{shopItem.count}/{config.quantity}";
    }

    public void BuyForWin(int itemIndex)
    {
        shopUI.BuyItem(itemIndex, ValueType.Wins, itemDefinition.price.amount);
    }

    public void BuyForDiamond(int itemIndex)
    {
        shopUI.BuyItem(itemIndex, ValueType.Diamond, itemDefinition.priceDiamonds);
    }

    private LanguageDirectory GetRarityName(RareType rarity)
    {
        switch (rarity)
        {
            case RareType.Common:
                return new LanguageDirectory("Обычный", "Common", "Siradan");
            case RareType.Uncommon:
                return new LanguageDirectory("Необычный", "Uncommon", "Siradisi");
            case RareType.Rare:
                return new LanguageDirectory("Редкий", "Rare", "Nadir");
            case RareType.Epic:
                return new LanguageDirectory("Эпический", "Epic", "Epik");
            case RareType.Legendary:
                return new LanguageDirectory("Легендарный", "Legendary", "Efsanevi");
            default:
                return new LanguageDirectory("Обычный", "Common", "Siradan");
        }
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

    private Sprite GetRarityBackground(RareType rarity)
    {
        switch (rarity)
        {
            case RareType.Common: return commonBackground;
            case RareType.Uncommon: return uncommonBackground;
            case RareType.Rare: return rareBackground;
            case RareType.Epic: return epicBackground;
            case RareType.Legendary: return legendaryBackground;
            default: return commonBackground;
        }
    }

    public void SetPurchased(bool purchased)
    {
        buyButton.interactable = !purchased;

        if (purchased)
        {
            priceText.text = UTF.ReadLanguageDirectory(new LanguageDirectory("КУПЛЕНО", "PURCHASED", "SATIN ALINDI"));
        }
        else
        {
            UpdateCountText();
        }
    }
}