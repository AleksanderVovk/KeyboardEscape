using System.Collections;
using TMPro;
using UnityEngine;

public class NotificationSingleView : MonoBehaviour
{
    [Header("References")]
    public TMP_Text textField;
    public CanvasGroup canvasGroup;
    public RectTransform rectTransform;

    [Header("Settings")]
    public float showDuration = 2f;      // Сколько висит уведомление
    public float fadeDuration = 0.3f;    // Длительность исчезновения

    [Header("PopUp Animation")]
    public float popupScaleFrom = 0f;    // Начальный масштаб (0)
    public float popupScaleOvershoot = 1.1f;  // Перелёт (1.1)
    public float popupScaleTo = 1f;      // Конечный масштаб (1.0)
    public float popupDuration = 0.25f;  // Длительность PopUp анимации

    private Coroutine _lifetimeRoutine;

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (textField == null)
            textField = GetComponentInChildren<TMP_Text>();
    }

    /// <summary>
    /// Инициализировать уведомление
    /// </summary>
    public void Initialize(string text, Color textColor)
    {
        textField.text = text;
        textField.color = textColor;

        // Сбрасываем состояние
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        rectTransform.localScale = Vector3.one * popupScaleFrom;

        if (_lifetimeRoutine != null)
            StopCoroutine(_lifetimeRoutine);

        _lifetimeRoutine = StartCoroutine(ShowAndFadeRoutine());
    }

    private IEnumerator ShowAndFadeRoutine()
    {
        // === PopUp анимация + Fade In ===
        float elapsed = 0f;

        // Фаза 1: 0 → 1.1 (рост с перелётом)
        float phase1Duration = popupDuration * 0.6f;  // 60% времени на рост
        while (elapsed < phase1Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / phase1Duration;

            // Масштаб: 0 → 1.1
            float currentScale = Mathf.Lerp(popupScaleFrom, popupScaleOvershoot, t);
            rectTransform.localScale = Vector3.one * currentScale;

            // Альфа: 0 → 1
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        // Фаза 2: 1.1 → 1.0 (возврат к норме)
        float phase2Duration = popupDuration * 0.4f;  // 40% времени на возврат
        elapsed = 0f;

        while (elapsed < phase2Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / phase2Duration;

            // Масштаб: 1.1 → 1.0 (плавно)
            float currentScale = Mathf.Lerp(popupScaleOvershoot, popupScaleTo, t);
            rectTransform.localScale = Vector3.one * currentScale;

            yield return null;
        }

        // Фиксируем финальное состояние
        rectTransform.localScale = Vector3.one * popupScaleTo;
        canvasGroup.alpha = 1f;

        // === Ждём заданное время ===
        yield return new WaitForSeconds(showDuration);

        // === Fade Out (только прозрачность, масштаб не меняем) ===
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_lifetimeRoutine != null)
            StopCoroutine(_lifetimeRoutine);
    }
}