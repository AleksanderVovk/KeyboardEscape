using UnityEngine;

public class TeleportToSpawn : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Entity"))
        {
            Player player;

            player = other.gameObject.GetComponent<Player>();

            player.Fall();
        }
    }
}
