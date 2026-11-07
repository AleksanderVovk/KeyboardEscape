using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger_OpenShop : MonoBehaviour
{
    [SerializeField] private ShopUpgradesController _shopUpgradesController;
    [SerializeField] private UpgradeEffectType _upgradeEffectType;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();

            if (player.thisRealPlayer)
            {
                _shopUpgradesController.OpenShopByCategory(_upgradeEffectType);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();

            if (player.thisRealPlayer)
            {
                PageManager.ClosePageByName("UpgradeShop");
            }
        }
    }
}
