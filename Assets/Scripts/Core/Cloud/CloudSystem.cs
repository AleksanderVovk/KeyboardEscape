using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudSystem : MonoBehaviour
{
    public static CloudSystem instance;

    [SerializeField] private Bounds _meshStartBounds;
    [SerializeField] private Bounds _meshFullBounds;

    [SerializeField] private MeshRenderer _startBounds;
    
    [SerializeField] private MeshRenderer _fullSkyBounds;

    [SerializeField] private Cloud[] _clouds;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        _meshStartBounds = _startBounds.bounds;
        _meshFullBounds = _fullSkyBounds.bounds;

        for (int i = 0; i < 40; i++)
        {
            Cloud newCloud = Instantiate(_clouds[Random.Range(0, _clouds.Length)], transform.position, Quaternion.identity);

            newCloud.moveSpeed = Random.Range(5f, 10f);
            newCloud.transform.position = GetStartRandomPoisition();
        }
    }

    public static Vector3 GetRandomPosition() => instance._GetRandomPosition();
    public Vector3 _GetRandomPosition()
    {
        // Генерируем случайные координаты в пределах границ
        float randomX = Random.Range(_meshStartBounds.min.x, _meshStartBounds.max.x);
        float randomY = Random.Range(_meshStartBounds.min.y, _meshStartBounds.max.y);
        float randomZ = Random.Range(_meshStartBounds.min.z, _meshStartBounds.max.z);

        return new Vector3(randomX, randomY, randomZ);
    }

    public Vector3 GetStartRandomPoisition()
    {
        // Генерируем случайные координаты в пределах границ
        float randomX = Random.Range(_meshFullBounds.min.x, _meshFullBounds.max.x);
        float randomY = Random.Range(_meshFullBounds.min.y, _meshFullBounds.max.y);
        float randomZ = Random.Range(_meshFullBounds.min.z, _meshFullBounds.max.z);

        return new Vector3(randomX, randomY, randomZ);
    }
}
