using System;
using UnityEngine;
using YG;

public class PlaytimeGiftRewardReceiver : MonoBehaviour
{
    /// <summary>
    /// Проверяет можно ли получить награду
    /// </summary>
    public bool CanReceiveGift(PlaytimeGiftData gift)
    {
        switch (gift.rewardType)
        {
            case PlaytimeGiftRewardType.Money:

                return true;

            case PlaytimeGiftRewardType.Brainrot:

                if (PlayersManager.instance.realPlayer.tsunamiReference.playerBase.FindFreePoint() != null)
                {
                    return true;
                }

                return false;

            case PlaytimeGiftRewardType.Diamonds:

                return true;

            case PlaytimeGiftRewardType.SpeedBonus:

                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Получает награду
    /// </summary>
    public void ReceiveGift(PlaytimeGiftData gift)
    {
        switch (gift.rewardType)
        {
            case PlaytimeGiftRewardType.Money:
                GiveMoney(gift.amount);
                break;

            case PlaytimeGiftRewardType.Brainrot:
                GiveBrainrot(gift.specialId);
                break;

            case PlaytimeGiftRewardType.Diamonds:
                GiveDiamonds(gift.amount);
                break;
            case PlaytimeGiftRewardType.SpeedBonus:
                GiveBonusSpeed(gift.amount);
                break;
            case PlaytimeGiftRewardType.Wins:
                break;
        }
    }

    private void GiveMoney(int amount)
    {
        ValueSystem.AddBalance(amount, ValueType.Coin);
    }

    private void GiveBrainrot(string specialId)
    {
        BrainrotBase brainrot = BrainrotManager.GetBrainrotById(specialId);
        PlayersManager.instance.realPlayer.tsunamiReference.playerBase.PlaceBrainrot(brainrot);
    }

    private void GiveDiamonds(int amount)
    {
        ValueSystem.AddBalance(amount, ValueType.Diamond);
    }

    private void GiveBonusSpeed(float amonut)
    {
        X2XpShop.Instance.GetX2SpeedBonus(amonut);
    }
}