using UnityEngine;
using YG;

public class SafeZone : MonoBehaviour
{
    public GameObject block;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            
            if (player.thisRealPlayer)
            {
                if (!YG2.saves.playerData.vipStatus)
                    ShopTabsScroll.instance.ScrollToTopInstant();
            }
        }
    }
}
