using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleport : MonoBehaviour
{
    public Transform point;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Entity"))
        {
            Player player = other.gameObject.GetComponent<Player>(); ;

            if (player.thisRealPlayer)
            {
                player.TeleportPlayer(point.position);
            }
        }
    }

}
