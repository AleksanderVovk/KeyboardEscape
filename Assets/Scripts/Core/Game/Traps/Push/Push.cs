using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Push : MonoBehaviour
{
    public float force;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            print("Collision");
            PlayerController playerController = other.GetComponent<PlayerController>();

            Vector3 pushDirection = transform.position - other.transform.position;
            pushDirection.Normalize();

            playerController.PushPlayer(-pushDirection, force, transform.position);
        }
    }
}
