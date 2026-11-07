using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YG;

public class CameraDistanceController : MonoBehaviour
{
    public static CameraDistanceController instance;

    public float zoomSpeed = 5f;       // Скорость приближения и отдаления камеры
    public float minZoom = 2f;         // Минимальное отдаление камеры
    public float maxZoom = 10f;        // Максимальное приближение камеры
    public float zoomDamping = 50f;    // Затухание скорости приближения и отдаления
    public float smoothZoomDuration = 0.5f; // Длительность плавного изменения зума
    public float defaultZoom = 8;
    public float saveZoom;

    private CinemachineVirtualCamera virtualCamera;
    private Cinemachine3rdPersonFollow framingTransposer;

    private bool isArrowActive = false;
    [SerializeField] private GameObject _arrowObject;

    [SerializeField] private InputsManager _inputsManager;

    public Vector3 defaultDamping;

    public bool canZoom;
    public bool blockZoom = false;

    private Coroutine smoothZoomCoroutine;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        framingTransposer = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();

        saveZoom = defaultZoom;
    }

    public void SetCanZoomValue(bool value)
    {
        canZoom = value;
    }

    // Новый метод для плавного изменения зума
    public static void SmoothZoomTo(float targetZoom) => instance._SmoothZoomTo(targetZoom);
    public void _SmoothZoomTo(float targetZoom)
    {
        print(saveZoom + " / " + targetZoom);
        if (targetZoom >= defaultZoom)
            if (saveZoom >= targetZoom)
                return;

        // Ограничиваем целевое значение зума
        targetZoom = Mathf.Clamp(targetZoom, saveZoom, maxZoom);

        // Если уже есть работающая корутина - останавливаем её
        if (smoothZoomCoroutine != null)
        {
            StopCoroutine(smoothZoomCoroutine);
        }

        // Запускаем новую корутину
        smoothZoomCoroutine = StartCoroutine(SmoothZoomCoroutine(targetZoom));
    }

    private IEnumerator SmoothZoomCoroutine(float targetZoom)
    {
        blockZoom = true;
        float startDistance = framingTransposer.CameraDistance;
        float elapsedTime = 0f;

        while (elapsedTime < smoothZoomDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / smoothZoomDuration);

            // Плавное изменение расстояния
            framingTransposer.CameraDistance = Mathf.Lerp(startDistance, targetZoom, t);

            yield return null;
        }

        // Убедимся, что достигли точного значения
        framingTransposer.CameraDistance = targetZoom;
        blockZoom = false;
        smoothZoomCoroutine = null;
    }

    private void Update()
    {
        if (canZoom && !blockZoom || YG2.envir.isDesktop && _inputsManager.screenClamped)
        {
            float scrollAmount = 0f;

            if (YG2.envir.isDesktop)
                scrollAmount = Input.GetAxis("Mouse ScrollWheel");
            else
                scrollAmount = SimpleInput.GetAxis("ScrollWheel");

            // Проверяем, что колесико мыши действительно было прокручено
            if (Mathf.Abs(scrollAmount) > 0.001f) // Используем небольшой порог для учета float-точности
            {
                // Зумирование камеры с ограничением
                float zoom = Mathf.Clamp(framingTransposer.CameraDistance - (scrollAmount * zoomSpeed),
                    minZoom, maxZoom);

                if (zoom >= 20)
                    SetArrowState(true);
                else if (zoom < 20)
                    SetArrowState(false);

                saveZoom = zoom;
                framingTransposer.CameraDistance = Mathf.Lerp(framingTransposer.CameraDistance, zoom, Time.deltaTime * zoomDamping);
            }
        }
    }

    private void SetArrowState(bool state)
    {
        if (state == isArrowActive)
            return;

        // Обновляем состояние
        isArrowActive = state;
        _arrowObject.SetActive(state);

        Debug.Log($"Изменяем состояние отображения стрелки дистанции персонажа: {(state ? "ВКЛ" : "ВЫКЛ")}");
    }
}