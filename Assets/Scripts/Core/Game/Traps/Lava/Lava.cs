using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lava : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Entity"))
        {
            Entity entity = other.gameObject.GetComponent<Entity>();
            entity.onFire = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Entity"))
        {
            Entity entity = other.gameObject.GetComponent<Entity>();
            entity.onFire = false;
        }
    }
}
