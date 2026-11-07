using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YG;

public class PaymentsManager : MonoBehaviour
{
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
        // Ваш код для обработки покупки, например:
        string coinsKey = "coins";
        int coins = YG2.GetState(coinsKey);

        if (id == "50")
            YG2.SetState(coinsKey, coins + 50);
        else if (id == "250")
            YG2.SetState(coinsKey, coins + 250);
        else if (id == "1500")
            YG2.SetState(coinsKey, coins + 1500);
    }

    private void FailedPurchased(string id)
    {
        // Покупка не была совершена
    }
}
