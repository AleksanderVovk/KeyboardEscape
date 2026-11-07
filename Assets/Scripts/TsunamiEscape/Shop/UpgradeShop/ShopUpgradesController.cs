using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YG;

public class ShopUpgradesController : MonoBehaviour
{
    public static event Action ShopChanged;

    [Header("UI")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private UpgradeShopItemView itemPrefab;

    [Header("Definitions (items)")]
    [SerializeField] private UpgradeDefinition[] definitions;

    // вьюшка + её индекс в definitions
    private readonly List<(UpgradeShopItemView view, int defIndex)> _views = new();

    [SerializeField] private Image _pageIcon;
    [SerializeField] private Sprite _iconSpeed;
    [SerializeField] private Sprite _iconStrength;

    [SerializeField] private Image _pageOverlay;
    [SerializeField] private Sprite _speedOverlay;
    [SerializeField] private Sprite _strengthOverlay;

    private void Start()
    {
        EnsureShopInitialized();
        // стартовый полный билд, если нужно
        BuildUI_All();
        RefreshAllViews();
    }

    private void OnEnable() => ShopChanged += RefreshAllViews;
    private void OnDisable() => ShopChanged -= RefreshAllViews;

    // Открыть магазин только с определённым типом улучшения (скорость/сила и т.п.)
    public void OpenShopByCategory(UpgradeEffectType category)
    {
        EnsureShopInitialized();
        BuildUIByCategory(category);
        RefreshAllViews();            // важно вызвать после билдa
        PageManager.OpenPageByName("UpgradeShop");

        switch (category)
        {
            case UpgradeEffectType.AddSpeed:
                _pageIcon.sprite = _iconSpeed;
                _pageOverlay.sprite = _speedOverlay;
                break;
            case UpgradeEffectType.AddCarrySlot:
                _pageIcon.sprite = _iconStrength;
                _pageOverlay.sprite = _strengthOverlay;
                break;
        }
    }

    // Полный UI (если где-то нужно)
    private void BuildUI_All()
    {
        ClearUI();
        for (int i = 0; i < definitions.Length; i++)
            CreateViewForDefinition(i);
    }

    // UI только по категории
    private void BuildUIByCategory(UpgradeEffectType category)
    {
        ClearUI();

        for (int i = 0; i < definitions.Length; i++)
        {
            if (definitions[i].effectType != category)
                continue;

            CreateViewForDefinition(i);
        }
    }

    private void ClearUI()
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);

        _views.Clear();
    }

    // создаём вью и запоминаем индекс definition
    private void CreateViewForDefinition(int defIndex)
    {
        var view = UnityEngine.Object.Instantiate(itemPrefab, contentRoot);

        // навешиваем обработчики покупки
        view.SetBuyHandlers(
            onBuySoft: () => TryBuy(defIndex, ValueType.Coin),
            onBuyHard: () => TryBuy(defIndex, ValueType.Diamond)
        );

        _views.Add((view, defIndex));
    }

    public void RefreshAllViews()
    {
        var shop = YG2.saves.playerData.shop;
        if (shop == null || shop.items == null) return;

        // обновляем только те вьюшки, которые реально существуют
        for (int i = 0; i < _views.Count; i++)
        {
            var (view, defIndex) = _views[i];

            var def = definitions[defIndex];
            var st = shop.items[defIndex];

            bool canSoft = CanBuy(def, st, ValueType.Coin);
            bool canHard = CanBuy(def, st, ValueType.Diamond);

            view.Rebind(def, st, canBuySoft: canSoft, canBuyHard: canHard);
        }
    }

    private bool CanBuy(UpgradeDefinition def, UpgradeState st, ValueType currency)
    {
        if (st.level >= def.maxLevel) return false;

        var wallet = YG2.saves.playerData.wallet;

        if (currency == ValueType.Coin)
            return wallet.money >= st.nextPriceSoft;

        if (currency == ValueType.Diamond)
            return wallet.diamonds >= def.startPriceHard; // HARD статична

        return false;
    }

    private void TryBuy(int index, ValueType currency)
    {
        var def = definitions[index];
        var shop = YG2.saves.playerData.shop;
        var st = shop.items[index];

        if (st.level >= def.maxLevel)
        {
            RefreshAllViews();
            return;
        }

        long price = (currency == ValueType.Coin)
            ? st.nextPriceSoft
            : def.startPriceHard;

        if (price <= 0)
        {
            RefreshAllViews();
            return;
        }

        if (!ValueSystem.SubstractionBalance(price, currency))
        {
            UISound.manager.PlayUpgrade(false);
            RefreshAllViews();
            return;
        }

        ApplyEffect(def);

        UISound.manager.PlayUpgrade(true);

        st.level++;
        st.nextPriceSoft = CalcPrice(def.startPrice, def.priceMultiplier, st.level);

        YG2.SaveProgress(); // сохранить изменения [web:5]
        ShopChanged?.Invoke();
    }

    private void ApplyEffect(UpgradeDefinition def)
    {
        switch (def.effectType)
        {
            case UpgradeEffectType.AddSpeed:
                PlayersManager.instance.realPlayer.playerSpeed.AddSpeed(def.realAmount, true);
                break;

            case UpgradeEffectType.AddCarrySlot:
                YG2.saves.playerData.shop.carrySlots += def.displayAmount;
                break;
        }
    }

    private void EnsureShopInitialized()
    {
        var shop = YG2.saves.playerData.shop;
        if (shop == null) YG2.saves.playerData.shop = new UpgradesShopData();
        shop = YG2.saves.playerData.shop;

        if (shop.items == null || shop.items.Length != definitions.Length)
            shop.items = new UpgradeState[definitions.Length];

        for (int i = 0; i < definitions.Length; i++)
        {
            var def = definitions[i];

            if (shop.items[i] == null || shop.items[i].id != def.id)
            {
                shop.items[i] = new UpgradeState
                {
                    id = def.id,
                    level = 0,
                    nextPriceSoft = def.startPrice
                };
            }
            else
            {
                if (shop.items[i].nextPriceSoft <= 0)
                    shop.items[i].nextPriceSoft = def.startPrice;
            }
        }

        YG2.SaveProgress(); // фиксируем инициализацию [web:5]
    }

    public void ResetShopToDefaults()
    {
        var shop = YG2.saves.playerData.shop;
        if (shop == null)
        {
            YG2.saves.playerData.shop = new UpgradesShopData();
            shop = YG2.saves.playerData.shop;
        }

        if (shop.items == null || shop.items.Length != definitions.Length)
            shop.items = new UpgradeState[definitions.Length];

        for (int i = 0; i < definitions.Length; i++)
        {
            var def = definitions[i];

            shop.items[i] = new UpgradeState
            {
                id = def.id,
                level = 0,
                nextPriceSoft = def.startPrice
            };
        }

        YG2.SaveProgress();
        ShopChanged?.Invoke();
    }

    private long CalcPrice(long startPrice, float mult, int level)
    {
        double value = startPrice * Math.Pow(mult, level);
        return (long)Math.Ceiling(value);
    }
}
