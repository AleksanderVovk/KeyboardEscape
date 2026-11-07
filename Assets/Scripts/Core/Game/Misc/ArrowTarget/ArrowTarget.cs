using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowTarget : MonoBehaviour
{
    public Transform target;
    public GameObject targetBody;
    public float speed = 2.0f; // Скорость движения

    public bool lockY;

    private void Start()
    {
        ArrowState(false);
    }

    public void SetSpawnPosition()
    {
        transform.position = new Vector3(PlayersManager.GetRealPlayerPosition().x, transform.position.y, PlayersManager.GetRealPlayerPosition().z);
    }

    public void SetTarget(Transform _target)
    {
        target = _target;

        if (target == null)
        {
            ArrowState(false);
        }
    }

    public void ArrowState(bool state)
    {
        if (state)
            targetBody.SetActive(true);
        else
            targetBody.SetActive(false);
    }


    void Update()
    {
        if (target != null)
        {
            // Создаем вектор направления к целевому объекту с фиксированной высотой
            Vector3 targetPosition = new Vector3(target.position.x, transform.position.y, target.position.z);

            // Плавно передвигаем объект к целевой позиции
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        }
    }
}
