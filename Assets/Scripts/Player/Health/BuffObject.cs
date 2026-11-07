using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuffObject : MonoBehaviour
{
    public Image image; // Твой UI элемент Image
    public float fadeDuration = 1f; // Длительность фейд-эффекта
    public float hitScaleMultiplier = 1.2f; // Множитель для увеличения размера
    public float hitDuration = 0.1f; // Длительность увеличения размера

    private CanvasGroup canvasGroup; // Группа для управления прозрачностью
    public bool isShow;

    private float _startAlpha;
    private float _startScale;
    private Vector3 _originalScale;
    private Vector3 _targetScale;

    private void Awake()
    {
        // Создаем или получаем CanvasGroup на объекте
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0; // Начинаем с полной прозрачности

        _startScale = transform.localScale.x; // начальный масштаб
        _originalScale = image.transform.localScale; // Сохраняем исходный размер
        _targetScale = _originalScale * hitScaleMultiplier; // Целевой размер

        gameObject.SetActive(false); // Скрываем объект по умолчанию
    }

    private void Start()
    {

    }

    public void Show()
    {
        gameObject.SetActive(true);
        isShow = true;
        StopAllCoroutines();
        StartCoroutine(FadeTo(1, fadeDuration)); // Увеличиваем прозрачность до 1   
    }

    public void Hide()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeTo(0, fadeDuration)); // Уменьшаем прозрачность до 0
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        _startAlpha = canvasGroup.alpha;
        float targetScale = 1f; // конечный фиксированный масштаб
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime; // Время увеличивается
            canvasGroup.alpha = Mathf.Lerp(_startAlpha, targetAlpha, time / duration); // Линейная интерполяция

            // Устанавливаем масштаб
            float scale = Mathf.Lerp(_startScale, targetScale, time / duration);
            transform.localScale = new Vector3(scale, scale, scale); // Устанавливаем одинаковый масштаб по всем осям

            yield return null; // Ждем следующий кадр
        }

        canvasGroup.alpha = targetAlpha; // Устанавливаем цель достигнутой
        transform.localScale = new Vector3(targetScale, targetScale, targetScale); // Устанавливаем финальный масштаб

        if (targetAlpha == 0)
        {
            isShow = false;
            gameObject.SetActive(false); // Скрываем объект, если целевая альфа равна 0
        }
    }

    public void Hit()
    {
        if(gameObject.activeSelf)
            StartCoroutine(HitEffect());
    }

    private IEnumerator HitEffect()
    {
        // Увеличиваем размер
        float time = 0;
        while (time < hitDuration)
        {
            time += Time.deltaTime;
            image.transform.localScale = Vector3.Lerp(_originalScale, _targetScale, time / hitDuration); // Линейная интерполяция
            yield return null; // Ждем следующий кадр
        }

        // Возвращаемся к исходному размеру
        time = 0;
        while (time < hitDuration)
        {
            time += Time.deltaTime;
            image.transform.localScale = Vector3.Lerp(_targetScale, _originalScale, time / hitDuration); // Линейная интерполяция
            yield return null; // Ждем следующий кадр
        }

        image.transform.localScale = _originalScale; // Устанавливаем окончательный размер
    }
}
