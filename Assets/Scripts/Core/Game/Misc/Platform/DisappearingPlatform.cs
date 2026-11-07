using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisappearingPlatform : MonoBehaviour
{
    public bool platformActive = true;
    private Renderer platformRenderer;
    [SerializeField] private Collider platformCollider;
    public float disappearDuration = 1f; // Время исчезновения
    public float reappearDelay = 2f;      // Задержка перед восстановлением
    public float reappearDuration = 1f;   // Время восстановления

    private Material _mat;
    private Color _startColor;

    private void Start()
    {
        platformRenderer = GetComponent<Renderer>();
        UpdateDefaultColor();
    }

    public void UpdateDefaultColor()
    {
        // Получаем начальный цвет и материал
        _mat = platformRenderer.material;
        _startColor = _mat.color;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, является ли объект игроком
        if (other.CompareTag("Player"))
        {
            if(platformActive)
                StartCoroutine(HandlePlatformDisappear());
        }
    }

    private IEnumerator HandlePlatformDisappear()
    {
        platformActive = false;

        Color endColor = new Color(_startColor.r, _startColor.g, _startColor.b, 0f); // Прозрачность до 0

        // Плавное исчезновение
        float elapsedTime = 0f;

        while (elapsedTime < disappearDuration)
        {
            float alpha = Mathf.Lerp(_startColor.a, 0f, elapsedTime / disappearDuration);
            _mat.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Выключаем коллайдер для предотвращения повторных триггеров
        platformCollider.enabled = false;

        // Устанавливаем конечный цвет
        _mat.color = endColor;

        // Ждем заданное время до восстановления
        yield return new WaitForSeconds(reappearDelay);

        // Плавное восстановление
        elapsedTime = 0f;
        while (elapsedTime < reappearDuration)
        {
            float alpha = Mathf.Lerp(0f, _startColor.a, elapsedTime / reappearDuration);
            _mat.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Устанавливаем цвет обратно
        _mat.color = _startColor;

        // Включаем коллайдер
        platformCollider.enabled = true;
        platformActive = true;
    }
}
