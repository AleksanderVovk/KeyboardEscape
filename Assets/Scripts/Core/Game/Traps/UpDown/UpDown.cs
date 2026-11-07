using UnityEngine;

public class UpDown : MonoBehaviour
{
    public float upperLimit = 3f;  // Верхняя граница
    public float lowerLimit = -3f;  // Нижняя граница
    public float speed = 2f;         // Скорость движения

    private float direction = 1f;    // Направление движения
    private float initialY;           // Начальная высота

    [SerializeField] private Transform _target;

    private void Start()
    {
        // Сохраняем начальную высоту объекта
        initialY = transform.position.y;
    }

    private void Update()
    {
        // Получаем текущее положение объекта
        Vector3 currentPosition = transform.position;

        // Изменяем положение по Y относительно начальной точки
        float posY = 0;
        if (_target == null)
            posY = initialY;
        else
            posY = _target.position.y + initialY;

        currentPosition.y = posY + Mathf.Clamp(Mathf.PingPong(Time.time * speed, upperLimit - lowerLimit) + lowerLimit, lowerLimit, upperLimit);

        // Обновляем позицию объекта
        transform.position = currentPosition;
    }
}
