using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class Dimensions
{
    public int length;
    public int width;
}

public class MathCore : MonoBehaviour
{
    public static MathCore instance;

    private void Awake()
    {
        instance = this;
    }

    public static Vector3 GetRandomPosition(Bounds bounds) => instance._GetRandomPosition(bounds);
    public Vector3 _GetRandomPosition(Bounds bounds)
    {
        // Генерируем случайные координаты в пределах границ
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomY = Random.Range(bounds.min.y, bounds.max.y);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, randomY, randomZ);
    }
}
