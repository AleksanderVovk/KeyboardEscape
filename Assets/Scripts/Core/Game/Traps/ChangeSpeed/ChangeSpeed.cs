using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeSpeed : MonoBehaviour
{
    public float multiple;
    public bool positive;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Entity"))
        {
            PlayerMovement playerMovement = other.gameObject.GetComponent<PlayerMovement>();

            if (playerMovement != null)
            {
                playerMovement.ChangeSpeed(multiple, positive, false);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Entity"))
        {
            PlayerMovement playerMovement = other.gameObject.GetComponent<PlayerMovement>();

            if (playerMovement != null)
            {
                playerMovement.ChangeSpeed(0, positive, true);
            }
        }
    }
}
