using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScalePingPong : MonoBehaviour
{
    public Vector3 startScale = new Vector3(1, 1, 1); // Начальный размер
    public Vector3 endScale = new Vector3(2, 2, 2);   // Конечный размер
    public float speed = 1.0f; // Скорость изменения размера

    private void Update()
    {
        // Используем синусоидальную функцию для плавного перехода между размерами
        float t = Mathf.Sin(Time.time * speed) * 0.5f + 0.5f; // Преобразуем синус в диапазон [0, 1]
        transform.localScale = Vector3.Lerp(startScale, endScale, t); // Интерполяция между startScale и endScale
    }
}
