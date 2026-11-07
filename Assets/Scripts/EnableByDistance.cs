using UnityEngine;

public class EnableByDistance : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float threshold = 20f;
    [SerializeField] private GameObject target;

    private void Awake()
    {
        if (target == null) target = gameObject;
    }

    private void Start()
    {
        player = PlayersManager.instance.realPlayer.transform;
    }

    private void Update()
    {
        if (player == null) return;

        float d = Vector3.Distance(player.position, transform.position); // [web:37]

        bool shouldBeActive = d >= threshold;
        if (target.activeSelf != shouldBeActive)
            target.SetActive(shouldBeActive); // [web:36]
    }
}
