using UnityEngine;

public class PingPongMoverWithWait : MonoBehaviour
{
    [Header("Настройки движения")]
    [Tooltip("Объект, который будет двигаться")]
    public Transform targetObject;

    [Tooltip("Точка A (начало)")]
    public Transform pointA;

    [Tooltip("Точка B (конец)")]
    public Transform pointB;

    [Tooltip("Время движения от A до B в секундах")]
    public float durationAtoB = 2f;

    [Tooltip("Время движения от B до A в секундах")]
    public float durationBtoA = 2f;

    [Tooltip("Время ожидания в точке B в секундах")]
    public float waitTimeAtB = 1f;

    [Tooltip("Время ожидания в точке A в секундах")]
    public float waitTimeAtA = 1f;

    // Внутренние переменные
    private enum MoveState
    {
        MovingAtoB,
        WaitingAtB,
        MovingBtoA,
        WaitingAtA
    }

    private MoveState currentState;
    private float elapsed;

    private void Reset()
    {
        // Если не задано в инспекторе, попробуем взять targetObject = этот объект
        if (targetObject == null)
            targetObject = transform;
    }

    private void Start()
    {
        if (targetObject == null || pointA == null || pointB == null)
        {
            Debug.LogError("[PingPongMoverWithWait] Не все ссылки заполнены в инспекторе!");
            return;
        }

        // Ставим объект в точку A
        targetObject.position = pointA.position;

        elapsed = 0f;
        currentState = MoveState.MovingAtoB;
    }

    private void Update()
    {
        if (targetObject == null || pointA == null || pointB == null)
            return;

        elapsed += Time.deltaTime;

        switch (currentState)
        {
            case MoveState.MovingAtoB:
                HandleMovingAtoB();
                break;

            case MoveState.WaitingAtB:
                HandleWaitingAtB();
                break;

            case MoveState.MovingBtoA:
                HandleMovingBtoA();
                break;

            case MoveState.WaitingAtA:
                HandleWaitingAtA();
                break;
        }
    }

    private void HandleMovingAtoB()
    {
        if (elapsed >= durationAtoB)
        {
            // Дошли до B
            targetObject.position = pointB.position;
            elapsed = 0f;
            currentState = MoveState.WaitingAtB;
            return;
        }

        float t = elapsed / durationAtoB;
        targetObject.position = Vector3.Lerp(pointA.position, pointB.position, t);
    }

    private void HandleWaitingAtB()
    {
        if (elapsed >= waitTimeAtB)
        {
            // Время ожидания вышло, начинаем движение к A
            elapsed = 0f;
            currentState = MoveState.MovingBtoA;
            return;
        }

        // Пока ждём, позиция не меняется (уже стоит в B)
        targetObject.position = pointB.position;
    }

    private void HandleMovingBtoA()
    {
        if (elapsed >= durationBtoA)
        {
            // Дошли до A
            targetObject.position = pointA.position;
            elapsed = 0f;
            currentState = MoveState.WaitingAtA;
            return;
        }

        float t = elapsed / durationBtoA;
        targetObject.position = Vector3.Lerp(pointB.position, pointA.position, t);
    }

    private void HandleWaitingAtA()
    {
        if (elapsed >= waitTimeAtA)
        {
            // Время ожидания вышло, начинаем движение к B
            elapsed = 0f;
            currentState = MoveState.MovingAtoB;
            return;
        }

        // Пока ждём, позиция не меняется (уже стоит в A)
        targetObject.position = pointA.position;
    }
}