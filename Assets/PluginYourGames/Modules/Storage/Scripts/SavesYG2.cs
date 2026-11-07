using System;
using System.Collections.Generic;
using UnityEngine;

namespace YG
{
    [Serializable]
    public class PlayerData
    {
        public Gender gender;

        public int speedLvl = 1;

        public int donateCount;
        public float playTime;

        public float bestTime;

        public Wallet wallet = new Wallet();

        // Новый общий опыт (может быть очень большим)
        public long totalXp = 0;

        // Новое: атрибуты игрока (скорость, сила и т.п.)
        public AdditionalAttributes attributes = new AdditionalAttributes();

        public int rebirthLevel;          // старт 0
        public double rebirthMultiplier = 1f; // текущий множитель, старт 1

        public PlayerBase playerBase = new PlayerBase();

        public UpgradesShopData shop = new UpgradesShopData();

        public bool vipStatus;

        public List<string> discoveredBrainrots = new List<string>(); // id брейнрота
        public List<string> discoveredIndexEntries = new List<string>();

        public List<string> boughtSkins = new List<string>();
        public string equippedSkinId;

        public List<string> purchasedTreadmills = new List<string>();

        public double x2XpTimeRemaining = 0d;
    }

    [Serializable]
    public class Wallet
    {
        public long money;
        public long diamonds;
        public long spins;
        public long wins;
    }

    public enum UpgradeId
    {
        Speed_1,
        Speed_5,
        Speed_10,
        Carry_1
    }

    [Serializable]
    public class ItemBoost
    {
        public string itemId;  // ID предмета из ScriptableObject

        public ItemBoost(string id)
        {
            itemId = id;
        }
    }


    [Serializable]
    public class Inventory
    {
        public List<ItemData> defaultItem = new List<ItemData>();
        //public List<FishData> fishs = new List<FishData>();
        //public List<FishingRodData> fishingRods = new List<FishingRodData>();
        public List<ItemBoost> storage = new List<ItemBoost>();        // Склад предметов
        public List<ItemBoost> equipped = new List<ItemBoost>();       // Экипированные предметы
    }

    [Serializable]
    public class UpgradeState
    {
        public UpgradeId id;
        public int level;            // сколько раз купили
        public long nextPriceSoft;  // цена за след. покупку (money)
        public long nextPriceHard; // если хочешь часть за diamonds (0 = не используется)
    }

    [Serializable]
    public class UpgradesShopData
    {
        public UpgradeState[] items = new UpgradeState[4];

        // Текущие итоговые значения (чтобы не пересчитывать на лету, но можно и пересчитывать)
        public int speedBonus;   // суммарный бонус скорости от покупок
        public int carrySlots;   // сколько слотов переноски (или бонус слотов)
    }


    [System.Serializable]
    public class TrainingData
    {
        public bool training_brainrot_pickup;
        public bool training_brainrot_complete;
    }

    [System.Serializable]
    public class Settings
    {
        public float sensitivity = 400f;
        public float volume = 1f;
        public float musicVolume = 0.35f;
    }

    [Serializable]
    public class PlayerBase
    {
        public int baseLevel = 0; 

        public BrainrotPointData[] brainrotPoints = new BrainrotPointData[30];
    }

    [Serializable]
    public class ShopItemBoostData
    {
        public string itemId;      // ID предмета
        public int count;          // Количество в магазине
        public bool purchased;     // Куплен ли предмет
    }

    [Serializable]
    public class ShopData
    {
        public ShopItemBoostData[] items = new ShopItemBoostData[4];  // 4 товара
        public float timeRemaining;  // Время последнего рестока
    }

    [Serializable]
    public class ShopItemData
    {
        public string itemId;
        public bool purchased;
        public bool equipped;
    }

    [Serializable]
    public class ShopCategoryData
    {
        public ShopItemData[] items = new ShopItemData[0];
    }

    [Serializable]
    public class EquipmentShopData
    {
        public ShopCategoryData trailCategory = new ShopCategoryData();
        public ShopCategoryData objectCategory = new ShopCategoryData();
    }

    [Serializable]
    public partial class SavesYG
    {
        public int idSave;

        public PlayerData playerData;
        public Settings settings;
        public TrainingData trainingData;

        // Daily Core
        public long LastLoginTime;
        public long LastRewardClaimTime;
        public int DaysUnlocked;
        public List<bool> WasRewardCollectedOnSpecificDay = new() { false, false, false, false, false, false, false };
        public EquipmentShopData equipmentShop;
        public Inventory inventory;

        public ShopData shop = new ShopData();

        public SavesYG()
        {
            playerData = new PlayerData();
            settings = new Settings();
            inventory = new Inventory();
            trainingData = new TrainingData();
            equipmentShop = new EquipmentShopData();

            playerData.boughtSkins.Add("Skin0");
            playerData.equippedSkinId = "Skin0";

            playerData.purchasedTreadmills.Add("treadmill_default");
        }
    }
}
