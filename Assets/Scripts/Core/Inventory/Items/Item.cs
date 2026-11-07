using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Item_FR", menuName = "GameData/Item_FR/New Item_FR", order = 0)]
public class Item : ScriptableObject
{
    public string id;

    public LanguageDirectory itemName;
    public LanguageDirectory itemDescription;
    public RareType rareType;

    public ItemCategory category;

    public bool hideInMainInventory;

    public Sprite icon;

    public GameObject handPrefab;
}
