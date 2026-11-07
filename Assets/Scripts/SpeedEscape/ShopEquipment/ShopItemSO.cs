using UnityEngine;

public enum ShopItemType
{
    Trail,
    Object
}

[CreateAssetMenu(fileName = "NewShopItem", menuName = "Shop/Shop Item")]
public class ShopItemSO : ScriptableObject
{
    [Header("Основное")]
    public string itemId;

    [Header("Названия (мультиязычность)")]
    public LanguageDirectory itemName;

    [Header("Фон")]
    public Sprite backgroundSprite;
    public Color backgroundColor;

    [Header("Тип предмета")]
    public ShopItemType itemType;

    [Header("Цены")]
    public int priceWins;
    public int priceDiamonds;

    [Header("Множитель скорости")]
    [Range(1f, 10f)]
    public float speedMultiplier = 1f;

    [Header("Для типа Trail")]
    public Gradient trailGradient;

    [Header("Для типа Object")]
    public GameObject objectPrefab;

    [Header("Иконка")]
    public Sprite icon;
}