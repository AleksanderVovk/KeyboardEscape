using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YG;

public class ArrowController : MonoBehaviour
{
    public static ArrowController instance;

    public Transform jail;

    [SerializeField] private LineRenderer _lineRenderer;

    private bool trainingStart;

    private void Awake()
    {
        instance = this;
    }

    public void SetArrowState(bool state)
    {
        trainingStart = state;
        _lineRenderer.enabled = state;
    }

    private void Update()
    {
        if (!trainingStart)
            return;

        Vector3 targetPosition;
        Vector3 playerPosition;

        targetPosition = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
        playerPosition = new Vector3(jail.position.x, 1f, jail.position.z);

        _lineRenderer.positionCount = 2;

        _lineRenderer.SetPosition(0, targetPosition);
        _lineRenderer.SetPosition(1, playerPosition);
    }
}
