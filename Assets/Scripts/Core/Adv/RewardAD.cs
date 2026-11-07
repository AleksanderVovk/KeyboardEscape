using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YG;

public class RewardAD : MonoBehaviour
{
    public static RewardAD manager;

    public Bonus currentBonus;
    public BrainrotReward brainrotReward;

    private void Awake()
    {
        manager = this;
    }

    private void OnEnable()
    {
        YG2.onRewardAdv += OnReward;
    }

    private void OnDisable()
    {
        YG2.onRewardAdv -= OnReward;
    }

    public void MyRewardAdvShow_Default(string id)
    {
        YG2.RewardedAdvShow(id);
    }

    public void MyRewardAdvShow_Bonus(string id, Bonus bonus)
    {
        YG2.RewardedAdvShow(id);

        currentBonus = bonus;
    }

    private void OnReward(string id)
    {
        if (id == "coins_200")
        {
            ValueSystem.AddBalance(1000, ValueType.Coin);

            YG2.MetricaSend("show_rewardedAD", "coins", new Dictionary<string, object>
            {
                { "reward", "200" },
            });

            Debug.Log("ShowRewarded: coins_200");
        }
        if (id == "diamonds_2")
        {
            ValueSystem.AddBalance(1, ValueType.Diamond);

            YG2.MetricaSend("show_rewardedAD", "diamonds", new Dictionary<string, object>
            {
                { "reward", "2" },
            });

            Debug.Log("ShowRewarded: diamonds_5");
        }
        if (id == "0")
        {
            if (currentBonus != null)
                BonusManager.instance.AddBouns(currentBonus);
        }
        if (id == "get_brainrot_back")
        {
            brainrotReward.OnReturnPressed();
        }

        YG2.MetricaSend("Show_RewardAD");
    }
}
