using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using YG;

public class SkinManager : MonoBehaviour
{
    public static SkinManager instance;

    [Header("Resources")]
    [SerializeField] private string skinsResourcesPath = "Costumization/Skins";

    [Header("UI")]
    private List<SkinUI> skinUIList = new List<SkinUI>();
    [SerializeField] private SkinUI skinUIPrefab;
    [SerializeField] private Transform contentParent;

    private readonly List<Skin> allSkins = new();
    private readonly List<SkinUI> skinUIs = new();

    private Skin currentEquippedSkin;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        RefreshAllSkinUI();
    }

    private void OnEnable()
    {
        ValueSystem.OnBalanceChanged += UpdateBalanceUI;
    }

    private void OnDisable()
    {
        ValueSystem.OnBalanceChanged -= UpdateBalanceUI;
    }

    public void UpdateBalanceUI()
    {
        foreach (SkinUI skinUI in skinUIs)
        {
            skinUI.ValueUpdate();
        }
    }

    public void Initialization()
    {
        LoadSkins();
        LoadSavedSkin();  
        SortSkinsByPrice();

        RefreshAllSkinUI();

        Debug.Log("Skin Initialization");
    }

    private void LoadSkins()
    {
        allSkins.Clear();

        Skin[] skins = Resources.LoadAll<Skin>(skinsResourcesPath);
        if (skins != null && skins.Length > 0)
            allSkins.AddRange(skins);
    }

    // Публичный метод для получения всех скинов (для ботов)
    public List<Skin> GetAllSkins()
    {
        return new List<Skin>(allSkins);
    }

    public void SortSkinsByPrice()
    {
        // Сортируем allSkins: сначала Coin, потом Diamond, внутри каждой группы по возрастанию цены
        allSkins.Sort((a, b) =>
        {
            if (a == null) return 1;
            if (b == null) return -1;

            // Сначала проверяем ValueType: Coin < Diamond
            if (a.price.valueType != b.price.valueType)
            {
                return a.price.valueType == ValueType.Coin ? -1 : 1;
            }

            // Если ValueType одинаковый, сортируем по цене (по возрастанию)
            return a.price.amount.CompareTo(b.price.amount);
        });

        // После сортировки обновляем UI
        SortSkinUIsBySkins();
    }

    private void SortSkinUIsBySkins()
    {
        // Очищаем текущий список UI
        ClearSpawnedUI();
        skinUIs.Clear();
        skinUIList.Clear();

        // Создаем UI заново в порядке сортированных скинов
        if (skinUIPrefab == null || contentParent == null)
            return;

        for (int i = 0; i < allSkins.Count; i++)
        {
            Skin skin = allSkins[i];
            if (skin == null)
                continue;

            SkinUI skinUI = Instantiate(skinUIPrefab, contentParent);
            skinUIList.Add(skinUI);
            skinUI.Setup(skin, this);
            skinUIs.Add(skinUI);
        }
    }

    private void ClearSpawnedUI()
    {
        if (contentParent == null)
            return;

        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
    }

    private void LoadSavedSkin()
    {
        if (YG.YG2.saves == null || YG.YG2.saves.playerData == null)
            return;

        string equippedId = YG.YG2.saves.playerData.equippedSkinId;
        if (string.IsNullOrEmpty(equippedId))
        {
            currentEquippedSkin = null;
            return;
        }

        currentEquippedSkin = GetSkinById(equippedId);
    }

    public void RefreshAllSkinUI()
    {
        for (int i = 0; i < skinUIs.Count; i++)
        {
            if (skinUIs[i] != null)
                skinUIs[i].RefreshUI();
        }
    }

    public bool IsBought(Skin skin)
    {
        if (skin == null || YG.YG2.saves == null || YG.YG2.saves.playerData == null)
            return false;

        return YG.YG2.saves.playerData.boughtSkins.Contains(GetSkinId(skin));
    }

    public bool IsEquipped(Skin skin)
    {
        if (skin == null || currentEquippedSkin == null)
            return false;

        return GetSkinId(currentEquippedSkin) == GetSkinId(skin);
    }

    public bool TryBuySkin(Skin skin)
    {
        if (skin == null)
            return false;

        if (IsBought(skin))
        {
            RefreshAllSkinUI();
            return false;
        }

        if (!ValueSystem.SubstractionBalance(skin.price.amount, skin.price.valueType))
            return false;

        if (!YG.YG2.saves.playerData.boughtSkins.Contains(GetSkinId(skin)))
            YG.YG2.saves.playerData.boughtSkins.Add(GetSkinId(skin));

        UISound.manager.PlayBuy(false);

        YG.YG2.SaveProgress();
        RefreshAllSkinUI();
        return true;
    }

    public bool TryEquipSkin(Skin skin, Player player, bool needPlayParticle)
    {
        if (skin == null)
            return false;

        if (player.thisRealPlayer)
        {
            if (!IsBought(skin))
                return false;

            currentEquippedSkin = skin;
            SaveEquippedSkinId(skin);

            UISound.manager.PlayPop(false);
        }

        player.playerCloth.EquipSkin(skin, needPlayParticle);

        if (player.thisRealPlayer)
            RefreshAllSkinUI();

        return true;
    }

    private void SaveEquippedSkinId(Skin skin)
    {
        YG.YG2.saves.playerData.equippedSkinId = GetSkinId(skin);
        YG.YG2.SaveProgress();
    }

    public Skin GetEquippedSkin()
    {
        return currentEquippedSkin;
    }

    public Skin GetSkinById(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        for (int i = 0; i < allSkins.Count; i++)
        {
            Skin skin = allSkins[i];

            if (skin == null)
                continue;

            if (GetSkinId(skin) == id)
                return skin;
        }

        return null;
    }

    public string GetSkinId(Skin skin)
    {
        if (skin == null)
            return string.Empty;

        if (!string.IsNullOrEmpty(skin.id))
            return skin.id;

        return skin.name;
    }
}