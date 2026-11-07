using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using YG;

public class SkinUI : MonoBehaviour
{
    [Header("Info")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;

    [Header("State")]
    [SerializeField] private GameObject lockObject;

    [SerializeField] private Button actionButton;
    private Image _buttonImage;

    [SerializeField] private TMP_Text actionButtonText;

    [SerializeField] private Sprite _buttonState_Equiped;
    [SerializeField] private Sprite _buttonState_Equip;
    [SerializeField] private Sprite _buttonState_CanBuy;
    [SerializeField] private Sprite _buttonState_CantBuy;

    private Skin skin;
    private SkinManager skinManager;

    public void Setup(Skin skinData, SkinManager manager)
    {
        skin = skinData;
        skinManager = manager;

        _buttonImage = actionButton.GetComponent<Image>();

        if (icon != null)
            icon.sprite = skin != null ? skin.icon : null;

        if (nameText != null)
        {
            nameText.text = skin != null && skin.skinName != null
                ? UTF.ReadLanguageDirectory(skin.skinName)
                : (skin != null ? skin.name : string.Empty);
        }

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionClick);
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (skin == null || skinManager == null)
        {
            Debug.Log($"[SkinUI] skin or manager is null: skin={skin}, manager={skinManager}");
            return;
        }

        bool bought = skinManager.IsBought(skin);
        bool equipped = skinManager.IsEquipped(skin);

        if (lockObject != null)
            lockObject.SetActive(!bought);

        if (actionButton != null)
            actionButton.interactable = !equipped;

        if (actionButtonText != null)
        {
            if (equipped)
            {
                // ОДЕТО
                actionButtonText.text = UTF.ReadLanguageDirectory(LangList.instance.equiped);
                Debug.Log("[SkinUI] Text: Equipped");
                _buttonImage.sprite = _buttonState_Equiped;
            }
            else if (bought)
            {
                // ОДЕРЬ (куплено, но не надето)
                actionButtonText.text = UTF.ReadLanguageDirectory(LangList.instance.equip);
                Debug.Log("[SkinUI] Text: Equip");
                _buttonImage.sprite = _buttonState_Equip;
            }
            else
            {
                // КУПИТЬ (не куплено)
                if (skin.price.valueType == ValueType.Coin)
                    actionButtonText.text = UTF.AbbreviateNumber(skin.price.amount) + UTF.manager.TextToSprite("\U0001F601");
                else
                    actionButtonText.text = skin.price.amount + UTF.manager.TextToSprite("\U0001F600");

                if (ValueSystem.HaveEnoughMoney(skin.price))
                    _buttonImage.sprite = _buttonState_CanBuy;
                else
                    _buttonImage.sprite = _buttonState_CantBuy;
            }
        }
        else
        {
            Debug.LogError("[SkinUI] actionButtonText is NULL in inspector!");
        }
    }

    public void ValueUpdate()
    {
        RefreshUI();
    }

    private void OnActionClick()    
    {
        if (skinManager == null || skin == null)
            return;

        bool bought = skinManager.IsBought(skin);
        bool equipped = skinManager.IsEquipped(skin);

        if (equipped)
            return;

        if (!bought)
            skinManager.TryBuySkin(skin);
        else
            skinManager.TryEquipSkin(skin, PlayersManager.instance.realPlayer, true);
    }
}