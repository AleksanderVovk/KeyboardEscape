using System.Collections.Generic;
using UnityEngine;

public class RandomObjectPlacer : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints; // Массив точек для размещения
    [SerializeField] private GameObject[] objectsToPlace; // Массив объектов для перемещения

    private void Start()
    {
        PlaceObjectsRandomly();
    }

    public void PlaceObjectsRandomly()
    {
        // Создаем копию списка точек, чтобы не изменять оригинальный массив
        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        // Перемешиваем массив объектов
        ShuffleArray(objectsToPlace);

        // Перемешиваем доступные точки
        ShuffleList(availablePoints);

        // Размещаем объекты на случайных свободных точках
        for (int i = 0; i < objectsToPlace.Length && i < availablePoints.Count; i++)
        {
            // Находим первую свободную точку в перемешанном списке
            for (int j = 0; j < availablePoints.Count; j++)
            {
                if (availablePoints[j].childCount == 0)
                {
                    PlaceObject(objectsToPlace[i], availablePoints[j]);
                    break;
                }
            }
        }
    }

    private void PlaceObject(GameObject obj, Transform point)
    {
        if (obj == null || point == null) return;

        // Сбрасываем родителя (если был)
        obj.transform.SetParent(null);

        // Устанавливаем позицию и случайное вращение
        obj.transform.position = point.position;
        obj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

        // Делаем объект дочерним к точке
        obj.transform.SetParent(point);
    }

    // Метод для перемешивания массива объектов
    private void ShuffleArray(GameObject[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            int randomIndex = Random.Range(i, array.Length);
            GameObject temp = array[randomIndex];
            array[randomIndex] = array[i];
            array[i] = temp;
        }
    }

    // Метод для перемешивания списка точек
    private void ShuffleList(List<Transform> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            Transform temp = list[randomIndex];
            list[randomIndex] = list[i];
            list[i] = temp;
        }
    }

    // Метод для проверки свободных точек
    public int GetFreePointsCount()
    {
        int count = 0;
        foreach (Transform point in spawnPoints)
        {
            if (point.childCount == 0) count++;
        }
        return count;
    }

    // Метод для размещения одного объекта в случайной свободной точке
    public void PlaceSingleObjectRandomly(GameObject obj)
    {
        if (obj == null) return;

        // Собираем все свободные точки
        List<Transform> freePoints = new List<Transform>();
        foreach (Transform point in spawnPoints)
        {
            if (point.childCount == 0)
            {
                freePoints.Add(point);
            }
        }

        // Если есть свободные точки - размещаем объект
        if (freePoints.Count > 0)
        {
            int randomIndex = Random.Range(0, freePoints.Count);
            PlaceObject(obj, freePoints[randomIndex]);
        }
        else
        {
            Debug.LogWarning("No free points available!");
        }
    }
}