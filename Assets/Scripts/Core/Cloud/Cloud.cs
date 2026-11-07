using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cloud : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Vector3 moveDirection = Vector3.forward;

    private Bounds meshBounds;

    private void Start()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshBounds = meshRenderer.bounds;
        }
    }

    private void Update()
    {
        Move();

        if (transform.position.z <= -350f)
        {
            ChangePosition();
        }
    }

    private void Move()
    {
        transform.Translate(moveDirection.normalized * moveSpeed * Time.deltaTime);
    }

    public void ChangePosition()
    {
        moveSpeed = Random.Range(5f, 10f);
        transform.position = CloudSystem.GetRandomPosition();
    }

}
