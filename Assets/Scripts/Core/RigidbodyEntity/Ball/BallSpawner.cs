using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    public static BallSpawner Instance;

    [Header("Prefab & Spawn")]
    public GameObject ballPrefab;
    public Transform spawnPoint;
    public float moveSpeed = 5f;

    private GameObject _ballInstance;
    private Vector3 _startPosition;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SpawnBall();
    }

    public void SpawnBall()
    {
        if (ballPrefab == null) return;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;

        // Если шара ещё нет — создаём один раз
        if (_ballInstance == null)
        {
            _ballInstance = Instantiate(ballPrefab, pos, Quaternion.identity);
        }
        else
        {
            // Если уже есть — просто телепортируем его назад
            _ballInstance.transform.position = pos;

            var rb = _ballInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        _startPosition = pos;
    }

    // Опционально: метод для триггера, который явно сбрасывает шар в старт
    public void ResetBallToStart()
    {
        if (_ballInstance == null) return;

        _ballInstance.transform.position = _startPosition;

        var rb = _ballInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}