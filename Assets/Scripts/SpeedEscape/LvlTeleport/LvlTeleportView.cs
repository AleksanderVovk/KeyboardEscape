using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LvlTeleportView : MonoBehaviour
{
    [Header("UI")]
    private Image _background;

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText_wins;
    [SerializeField] private TMP_Text priceText_diamonds;

    [SerializeField] private Button buyButton_wins;
    [SerializeField] private Button buyButton_diamonds;

    private TeleportStage _stage;
    private LvlTeleportShop _shop;

    public void Initialize(TeleportStage stage, LvlTeleportShop shop)
    {
        _background = GetComponent<Image>();

        _stage = stage;
        _shop = shop;

        if (nameText != null)
            nameText.text = UTF.ReadLanguageDirectory(stage.stageName);

        if (priceText_wins != null)
            priceText_wins.text = UTF.AbbreviateNumber(stage.priceWins);

        if (priceText_diamonds != null)
            priceText_diamonds.text = UTF.AbbreviateNumber(stage.priceDiamonds);

        if (_background != null)
            _background.color = _stage.lvlColor;


        if (buyButton_wins != null)
        {
            buyButton_wins.onClick.RemoveAllListeners();
            buyButton_wins.onClick.AddListener(BuyForWins);
        }


        if (buyButton_wins != null)
        {
            buyButton_diamonds.onClick.RemoveAllListeners();
            buyButton_diamonds.onClick.AddListener(BuyForDiamonds);
        }
    }

    private void BuyForWins()
    {
        OnBuyClicked(_stage.priceWins, ValueType.Wins);
    }

    private void BuyForDiamonds()
    {
        OnBuyClicked(_stage.priceDiamonds, ValueType.Diamond);
    }

    private void OnBuyClicked(int price, ValueType valueType)
    {
        if (_shop != null && _stage != null)
            _shop.BuyStage(_stage, price, valueType);
    }
}