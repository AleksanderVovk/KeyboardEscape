using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

public class UpgradeShopItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image icon;

    [SerializeField] private TMP_Text currentSpeed;
    [SerializeField] private TMP_Text nextSpeed;

    [SerializeField] private TMP_Text shortAmountAdd;

    [Header("Soft (Coin) UI")]
    [SerializeField] private TMP_Text priceText_soft;
    [SerializeField] private Button buyButton_soft;

    [Header("Hard (Diamond) UI")]
    [SerializeField] private TMP_Text priceText_hard;
    [SerializeField] private Button buyButton_hard;

    private System.Action _onBuySoft;
    private System.Action _onBuyHard;

    public void SetBuyHandlers(System.Action onBuySoft, System.Action onBuyHard)
    {
        _onBuySoft = onBuySoft;
        _onBuyHard = onBuyHard;

        if (buyButton_soft != null)
        {
            buyButton_soft.onClick.RemoveAllListeners();
            buyButton_soft.onClick.AddListener(() => _onBuySoft?.Invoke());
        }

        if (buyButton_hard != null)
        {
            buyButton_hard.onClick.RemoveAllListeners();
            buyButton_hard.onClick.AddListener(() => _onBuyHard?.Invoke());
        }
    }

    public void Rebind(UpgradeDefinition def, UpgradeState st, bool canBuySoft, bool canBuyHard)
    {
        string upgradeName = def.effectType == UpgradeEffectType.AddSpeed
            ? UTF.ReadLanguageDirectory(LangList.instance.speed)
            : UTF.ReadLanguageDirectory(LangList.instance.carry);

        if (icon != null) icon.sprite = def.icon;

        const float fakePerReal = 100f;

        titleText.text = "+" + def.displayAmount + " " + upgradeName;
        shortAmountAdd.text = "+" + def.displayAmount;

        if (def.effectType == UpgradeEffectType.AddSpeed)
        {
            float curReal = YG2.saves.playerData.attributes.speed;
            float nxtReal = curReal + def.realAmount;

            int curFake = Mathf.RoundToInt(curReal * fakePerReal);
            int nxtFake = Mathf.RoundToInt(nxtReal * fakePerReal);

            currentSpeed.text = curFake + " " + upgradeName;
            nextSpeed.text = nxtFake + " " + upgradeName;
        }
        else
        {
            int cur = YG2.saves.playerData.shop.carrySlots;
            int nxt = cur + def.displayAmount;

            currentSpeed.text = cur + " " + upgradeName;
            nextSpeed.text = nxt + " " + upgradeName;
        }

        // цены: soft из state, hard статичная из definition
        long softPrice = st.nextPriceSoft;
        long hardPrice = def.startPriceHard;

        if (priceText_soft != null)
            priceText_soft.text = "$" + UTF.AbbreviateNumber(softPrice);

        if (priceText_hard != null)
            priceText_hard.text = UTF.manager.TextToSprite("\U0001F602") + UTF.AbbreviateNumber(hardPrice);

        bool isMax = st.level >= def.maxLevel;

        if (buyButton_soft != null)
            buyButton_soft.interactable = !isMax && canBuySoft;
    }
}
