using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDraw : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public float tolerance = 0.1f; // Допустимое расстояние от границы
    private List<Vector3> points = new List<Vector3>();
    private EdgeCollider2D figureCollider;

    void Start()
    {
        // Получаем PolygonCollider2D, предполагая, что он прикреплен к фигуре
        figureCollider = GetComponent<EdgeCollider2D>();
    }

    void Update()
    {
        if (Input.GetMouseButton(0)) // Если нажата левая кнопка мыши
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0; // Устанавливаем z = 0
            if (points.Count == 0 || Vector3.Distance(mousePosition, points[points.Count - 1]) > tolerance)
            {
                points.Add(mousePosition);
                lineRenderer.positionCount = points.Count;
                lineRenderer.SetPosition(points.Count - 1, mousePosition);
            }
        }

        if (Input.GetMouseButtonUp(0)) // Если кнопка мыши отпущена
        {
            CheckDrawing();
        }
    }

    void CheckDrawing()
    {
        // Получаем точки границы из PolygonCollider2D
        Vector2[] boundaryPoints = figureCollider.points;
        int correctPoints = 0;

        // Переводим Vector2 в Vector3 для проверки
        foreach (Vector2 boundaryPoint in boundaryPoints)
        {
            Vector3 worldBoundaryPoint = figureCollider.transform.TransformPoint(boundaryPoint);
            foreach (Vector3 playerPoint in points)
            {
                if (Vector3.Distance(worldBoundaryPoint, playerPoint) < tolerance)
                {
                    correctPoints++;
                    break; // Находим один раз за точку границы, продолжаем
                }
            }
        }

        float percentage = (correctPoints / (float)boundaryPoints.Length) * 100;
        Debug.Log($"Правильность обводки: {percentage}%");

        // Здесь можно добавить логику, если игрок обвел фигуру правильно или нет
        if (percentage >= 80) // Например, если больше 80%
        {
            Debug.Log("Вы успешны! 🎉");
        }
        else
        {
            Debug.Log("Попробуйте снова! ❌");
        }

        // Очищаем точки после проверки
        points.Clear();
        lineRenderer.positionCount = 0; // Сбрасываем линию
    }
}
