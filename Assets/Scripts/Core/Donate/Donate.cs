using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using YG;

public class Donate : MonoBehaviour
{
    public static Donate manager;

    [SerializeField] private LuckyBrainrot luckyBlock_Legendary;
    [SerializeField] private LuckyBrainrot luckyBlock_Cosmic;
    [SerializeField] private LuckyBrainrot luckyBlock_Divine;

    //VIP
    [SerializeField] private GameObject[] _vipOffer;

    [SerializeField] private SafeZone[] _safeZones;

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        if (_vipOffer != null)
        {
            for (int i = 0; i < _vipOffer.Length; i++)
            {
                if (_vipOffer[i] != null)
                {
                    _vipOffer[i].gameObject.SetActive(!YG2.saves.playerData.vipStatus);
                }
            }
        }

        if (YG2.saves.playerData.vipStatus)
            UnlockSaveZone();
    }

    private void UnlockSaveZone()
    {
        for (int i = 0; i < _safeZones.Length; i++)
            _safeZones[i].block.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        YG2.onPurchaseSuccess += SuccessPurchased;
        YG2.onPurchaseFailed += FailedPurchased;
    }

    private void OnDisable()
    {
        YG2.onPurchaseSuccess -= SuccessPurchased;
        YG2.onPurchaseFailed -= FailedPurchased;
    }

    private void SuccessPurchased(string id)
    {
        switch (id)
        {
            case "vip":
                YG2.saves.playerData.vipStatus = true;

                for (int i = 0; i < _vipOffer.Length; i++)
                {
                    _vipOffer[i].gameObject.SetActive(!YG2.saves.playerData.vipStatus);
                }

                UnlockSaveZone();
                break;

            case "luckyblock_legendary":
                PlayersManager.instance.realPlayer.tsunamiReference.playerBase.SpawnLuckyBlockToFreePoint(luckyBlock_Legendary);
                break;
            case "luckyblock_cosmic":
                PlayersManager.instance.realPlayer.tsunamiReference.playerBase.SpawnLuckyBlockToFreePoint(luckyBlock_Cosmic);
                break;
            case "luckyblock_divine":
                PlayersManager.instance.realPlayer.tsunamiReference.playerBase.SpawnLuckyBlockToFreePoint(luckyBlock_Divine);
                break;

            case "coins_pack_1":
                ValueSystem.AddBalance(10000, ValueType.Coin);
                break;
            case "coins_pack_2":
                ValueSystem.AddBalance(50000, ValueType.Coin);
                break;
            case "coins_pack_3":
                ValueSystem.AddBalance(100000, ValueType.Coin);
                break;
            case "coins_pack_4":
                ValueSystem.AddBalance(250000, ValueType.Coin);
                break;
            case "coins_pack_5":
                ValueSystem.AddBalance(1000000, ValueType.Coin);
                break;

            case "diamonds_pack_1":
                ValueSystem.AddBalance(5, ValueType.Diamond);
                break;
            case "diamonds_pack_2":
                ValueSystem.AddBalance(20, ValueType.Diamond);
                break;
            case "diamonds_pack_3":
                ValueSystem.AddBalance(100, ValueType.Diamond);
                break;
            case "diamonds_pack_4":
                ValueSystem.AddBalance(200, ValueType.Diamond);
                break;
            case "diamonds_pack_5":
                ValueSystem.AddBalance(500, ValueType.Diamond);
                break;
            case "x2_10_minutes":
                X2XpShop.Instance.GetX2SpeedBonus(600);
                break;
            case "x2_30_minutes":
                X2XpShop.Instance.GetX2SpeedBonus(1800);
                break;
            case "x2_1_hours":
                X2XpShop.Instance.GetX2SpeedBonus(3600);
                break;
            case "200m_speed":
                PlayerSpeedLevelManager.instance.GiveAdminXP(200000000);
                break;
            case "500m_speed":
                PlayerSpeedLevelManager.instance.GiveAdminXP(500000000);
                break;
            case "1b_speed":
                PlayerSpeedLevelManager.instance.GiveAdminXP(1000000000);
                break;
        }

        YG2.saves.playerData.donateCount++;
        //YG2.SetLeaderboard("TopDonate", YG2.saves.playerData.donateCount);

        YG2.MetricaSend("buy_iap", id, new Dictionary<string, object>
            {
                { "donate_count", YG2.saves.playerData.donateCount }
            });

        YG2.SaveProgress();
    }

    public static void Buy(string id) => manager._Buy(id);
    public void _Buy(string id)
    {
        switch (id)
        {
            case "luckyblock_legendary":
            case "luckyblock_cosmic":
            case "luckyblock_divine":
                if (PlayersManager.instance.realPlayer.tsunamiReference.playerBase.FindFreePoint() == null)
                {
                    return;
                }
                break;
        }

        YG2.BuyPayments(id);
    }

    private void FailedPurchased(string id)
    {
        LanguageDirectory messageNotification = new LanguageDirectory
                     (
                     "Покупка не была совершена",
                     "The purchase was not completed",
                     "Satin alma islemi yapilmadi"
                     );

        NotificationManager.ShowNotification(UTF.ReadLanguageDirectory(messageNotification), NotificationType.Error);
    }
}
