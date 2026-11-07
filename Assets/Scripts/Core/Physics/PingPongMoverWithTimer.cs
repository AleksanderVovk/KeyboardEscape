using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class PingPongMoverWithTimer : MonoBehaviour
{
    [Header("Настройки движения")]
    [Tooltip("Объект, который будет двигаться")]
    public Transform targetObject;

    [Tooltip("Точка A (начало)")]
    public Transform pointA;

    [Tooltip("Точка B (конец)")]
    public Transform pointB;

    [Tooltip("Время движения от A до B в секундах")]
    public float moveDuration = 5f;

    [Header("UI")]
    [Tooltip("TMP_Text для отображения таймера (осталось времени до точки B)")]
    public TMP_Text timerText;

    private float _elapsedTime;
    private bool _isMoving = false;

    private void Reset()
    {
        // Если не задано в инспекторе, попробуем взять текст с этого же объекта
        if (timerText == null)
            timerText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        if (targetObject == null || pointA == null || pointB == null)
        {
            Debug.LogError("[PingPongMoverWithTimer] Не все ссылки заполнены в инспекторе!");
            return;
        }

        // Сразу ставим объект в точку A
        targetObject.position = pointA.position;

        _elapsedTime = 0f;
        _isMoving = true;

        if (timerText != null)
            timerText.text = moveDuration.ToString("F2");
    }

    private void Update()
    {
        if (!_isMoving || targetObject == null || pointA == null || pointB == null)
            return;

        _elapsedTime += Time.deltaTime;

        // Время, которое осталось до точки B
        float remainingTime = Mathf.Max(0f, moveDuration - _elapsedTime);

        // Обновляем текст таймера: Секунды.Милисекунды (два знака)
        if (timerText != null)
        {
            timerText.text = remainingTime.ToString("F2");
        }

        // Если время вышло — телепортируем в A и сбрасываем таймер
        if (_elapsedTime >= moveDuration)
        {
            targetObject.position = pointA.position;
            _elapsedTime = 0f;
            _isMoving = true;
            return;
        }

        // Вычисляем нормализованное время t от 0 до 1
        float t = _elapsedTime / moveDuration;

        // Двигаем объект от A к B
        targetObject.position = Vector3.Lerp(pointA.position, pointB.position, t);
    }
}