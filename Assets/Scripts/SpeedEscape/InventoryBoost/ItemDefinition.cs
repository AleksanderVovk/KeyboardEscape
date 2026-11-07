using UnityEngine;

namespace YG
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "InventoryBoost/Item Boost")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemId;           // Уникальный ID для сохранений
        public LanguageDirectory itemName;         // Название предмета
        public Sprite icon;             // Иконка

        [Header("Stats")]
        public RareType rarity;           // Редкость
        public float speedBonusPercent; // Процент добавляемой скорости

        [Header("Shop")]
        public Price price;               // Стоимость в монетах
        public int priceDiamonds;
    }
}