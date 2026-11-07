using System.Linq;
using UnityEngine;

[System.Serializable]
public enum ItemCategory
{
    None,
    Fish,
    FishingRod,
}

public class ItemsManager : MonoBehaviour
{
    public static ItemsManager instance;

    public Item[] allItems;
    public Item[] fishItems;
    public Item[] fishingRodItems;

    [SerializeField] private LanguageDirectory[] _categoriesNames;
    [SerializeField] private LanguageDirectory[] _rareTypeNames;
    [SerializeField] private Color[] _rareTypeColors;

    private void Awake() => instance = this;

    public void InitializationItems()
    {
        allItems = Resources.LoadAll<Item>("GameData/Items");

        fishItems = allItems.Where(item => item.category == ItemCategory.Fish).ToArray();
        fishingRodItems = allItems.Where(item => item.category == ItemCategory.FishingRod).ToArray();

        Debug.Log("InitializationItem");

        SotringAllItems();
    }

    public void SotringAllItems()
    {
        allItems = allItems.OrderBy(item => item.id).ToArray();
    }

    public static Item GetItemById(string id) => instance._GetItemById(id);
    public Item _GetItemById(string id)
    {
        return allItems.FirstOrDefault(p => p.id == id);
    }

    public static string GetCategoryNameById(int id) => instance._GetCategoryNameById(id);
    private string _GetCategoryNameById(int id)
    {
        return UTF.ReadLanguageDirectory(_categoriesNames[id]);
    }

    public static string GetRareNames(RareType rareType) => instance._GetRareNames(rareType);
    private string _GetRareNames(RareType rareType)
    {
        return UTF.ReadLanguageDirectory(_rareTypeNames[(int)rareType]);
    }

    public static Color GetRareColors(RareType rareType) => instance._GetRareColors(rareType);
    private Color _GetRareColors(RareType rareType)
    {
        return _rareTypeColors[(int)rareType];
    }
}
