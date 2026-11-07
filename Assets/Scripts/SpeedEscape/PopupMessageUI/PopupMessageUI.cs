using System.Collections;
using TMPro;
using UnityEngine;

public class PopupMessageUI : MonoBehaviour
{
    public static PopupMessageUI Instance;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Spawn Points")]
    [SerializeField] private Transform titleSpawnPoint;
    [SerializeField] private Transform descriptionSpawnPoint;

    [Header("Animation")]
    [SerializeField] private float appearDuration = 0.3f;
    [SerializeField] private float moveUpDuringAppear = 30f;

    [SerializeField] private float moveAfterAppearDuration = 0.35f;
    [SerializeField] private float moveUpAfterAppear = 40f;

    [Header("Pause Before Fade")]
    [SerializeField] private float pauseBeforeFade = 2f;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.12f;

    [Header("Delay")]
    [SerializeField] private float descriptionDelay = 0.12f;

    private CanvasGroup _titleCanvasGroup;
    private CanvasGroup _descriptionCanvasGroup;

    private Coroutine _popupCoroutine;

    private Vector3 _titleStartPosition;
    private Vector3 _descriptionStartPosition;

    private void Awake()
    {
        Instance = this;

        _titleCanvasGroup = GetOrAddCanvasGroup(titleText);
        _descriptionCanvasGroup = GetOrAddCanvasGroup(descriptionText);

        // Активируем тексты один раз.
        // Во время показа больше не используем SetActive.
        if (titleText != null)
            titleText.gameObject.SetActive(true);

        if (descriptionText != null)
            descriptionText.gameObject.SetActive(true);

        HideText(titleText, _titleCanvasGroup);
        HideText(descriptionText, _descriptionCanvasGroup);
    }

    public void ShowMessage(string title, string description)
    {
        if (titleText == null || descriptionText == null)
        {
            Debug.LogWarning(
                "[PopupMessage] Text references are not assigned."
            );
            return;
        }

        if (titleSpawnPoint == null || descriptionSpawnPoint == null)
        {
            Debug.LogWarning(
                "[PopupMessage] Spawn points are not assigned."
            );
            return;
        }

        if (_popupCoroutine != null)
        {
            StopCoroutine(_popupCoroutine);
            _popupCoroutine = null;
        }

        titleText.text = title;
        descriptionText.text = description;

        _popupCoroutine = StartCoroutine(ShowPopupCoroutine());
    }

    private IEnumerator ShowPopupCoroutine()
    {
        _titleStartPosition = titleSpawnPoint.position;
        _descriptionStartPosition = descriptionSpawnPoint.position;

        PrepareText(
            titleText,
            _titleCanvasGroup,
            _titleStartPosition
        );

        PrepareText(
            descriptionText,
            _descriptionCanvasGroup,
            _descriptionStartPosition
        );

        float singleTextDuration =
            appearDuration +
            moveAfterAppearDuration +
            pauseBeforeFade +
            fadeDuration;

        float totalDuration = Mathf.Max(
            singleTextDuration,
            descriptionDelay + singleTextDuration
        );

        bool titleFinished = false;
        bool descriptionFinished = false;

        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (!titleFinished)
            {
                titleFinished = AnimateText(
                    titleText,
                    _titleCanvasGroup,
                    _titleStartPosition,
                    elapsed,
                    0f
                );
            }

            if (!descriptionFinished)
            {
                descriptionFinished = AnimateText(
                    descriptionText,
                    _descriptionCanvasGroup,
                    _descriptionStartPosition,
                    elapsed,
                    descriptionDelay
                );
            }

            yield return null;
        }

        HideText(titleText, _titleCanvasGroup);
        HideText(descriptionText, _descriptionCanvasGroup);

        _popupCoroutine = null;
    }

    private bool AnimateText(
        TMP_Text text,
        CanvasGroup canvasGroup,
        Vector3 startPosition,
        float globalTime,
        float delay)
    {
        float localTime = globalTime - delay;

        // Задержка перед появлением
        if (localTime <= 0f)
        {
            text.transform.position = startPosition;
            text.transform.localScale = Vector3.zero;
            canvasGroup.alpha = 0f;

            return false;
        }

        float appearEnd = appearDuration;

        float moveEnd =
            appearEnd +
            moveAfterAppearDuration;

        float pauseEnd =
            moveEnd +
            pauseBeforeFade;

        float fadeEnd =
            pauseEnd +
            fadeDuration;

        // Этап 1:
        // Размер 0 -> 1 и движение вверх
        if (localTime <= appearEnd)
        {
            float t = Mathf.Clamp01(
                localTime / Mathf.Max(0.01f, appearDuration)
            );

            float easedT = EaseOutCubic(t);

            text.transform.position =
                startPosition +
                Vector3.up *
                Mathf.Lerp(
                    0f,
                    moveUpDuringAppear,
                    easedT
                );

            text.transform.localScale =
                Vector3.one * Mathf.Lerp(0f, 1f, easedT);

            canvasGroup.alpha = 1f;

            return false;
        }

        // Этап 2:
        // Размер уже 1, продолжаем движение вверх
        if (localTime <= moveEnd)
        {
            float t = Mathf.Clamp01(
                (localTime - appearEnd) /
                Mathf.Max(0.01f, moveAfterAppearDuration)
            );

            float easedT = EaseOutCubic(t);

            text.transform.position =
                startPosition +
                Vector3.up *
                (
                    moveUpDuringAppear +
                    Mathf.Lerp(
                        0f,
                        moveUpAfterAppear,
                        easedT
                    )
                );

            text.transform.localScale = Vector3.one;
            canvasGroup.alpha = 1f;

            return false;
        }

        // Этап 3:
        // Пауза перед исчезновением
        if (localTime <= pauseEnd)
        {
            text.transform.position =
                startPosition +
                Vector3.up *
                (
                    moveUpDuringAppear +
                    moveUpAfterAppear
                );

            text.transform.localScale = Vector3.one;
            canvasGroup.alpha = 1f;

            return false;
        }

        // Этап 4:
        // Быстрое исчезновение
        if (localTime <= fadeEnd)
        {
            float t = Mathf.Clamp01(
                (localTime - pauseEnd) /
                Mathf.Max(0.01f, fadeDuration)
            );

            text.transform.position =
                startPosition +
                Vector3.up *
                (
                    moveUpDuringAppear +
                    moveUpAfterAppear
                );

            text.transform.localScale = Vector3.one;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            return false;
        }

        // Анимация текста завершена
        text.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        return true;
    }

    private void PrepareText(
        TMP_Text text,
        CanvasGroup canvasGroup,
        Vector3 startPosition)
    {
        text.transform.position = startPosition;
        text.transform.localScale = Vector3.zero;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void HideText(
        TMP_Text text,
        CanvasGroup canvasGroup)
    {
        if (text == null || canvasGroup == null)
            return;

        text.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private CanvasGroup GetOrAddCanvasGroup(TMP_Text text)
    {
        if (text == null)
            return null;

        CanvasGroup canvasGroup =
            text.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                text.gameObject.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private float EaseOutCubic(float t)
    {
        float value = 1f - t;
        return 1f - value * value * value;
    }
}