using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;
using YG.Utils.Pay;

public class TreadmillPurchaseWindow : MonoBehaviour
{
    public static TreadmillPurchaseWindow Instance;

    [Header("Window")]
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private RectTransform windowTransform;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text rareText;
    [SerializeField] private Image rareBackground;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private ImageLoadYG iconImage;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button closeButton;

    [SerializeField] private ImageLoadYG _currencyIcon;

    [Header("Open Animation")]
    [SerializeField] private float openDuration = 0.3f;
    [SerializeField] private float overshootDuration = 0.12f;
    [SerializeField] private float overshootScale = 1.2f;

    [Header("Close Animation")]
    [SerializeField] private float closeDuration = 0.2f;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.25f;

    private Treadmill _currentTreadmill;
    private Coroutine _animationCoroutine;
    private bool _isClosing;

    private void Awake()
    {
        Instance = this;

        if (windowRoot == null)
            windowRoot = gameObject;

        if (canvasGroup == null)
            canvasGroup = windowRoot.GetComponent<CanvasGroup>();

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(BuyCurrentTreadmill);
            buyButton.onClick.AddListener(BuyCurrentTreadmill);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        HideImmediately();
    }

    public void Show(Treadmill treadmill)
    {
        if (treadmill == null)
            return;

        Purchase data = YG2.PurchaseByID(treadmill.purchaseId);

        if (data == null)
        {
            Debug.LogError(
                $"[TreadmillWindow] Purchase not found: {treadmill.purchaseId}"
            );
            return;
        }

        if (rareText != null)
        {
            rareText.text = UTF.ReadLanguageDirectory(treadmill.treadmillRare);
            rareBackground.color = treadmill.treadmillRareColor;
            rareText.color = treadmill.treadmillRareColor;
        }

        _currentTreadmill = treadmill;
        _isClosing = false;

        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }

        // СНАЧАЛА активируем родительский объект
        if (windowRoot != null)
            windowRoot.SetActive(true);

        // Затем можно запускать ImageLoadYG.Load()
        UpdateContent(treadmill, data);

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        _animationCoroutine = StartCoroutine(AnimateOpen());
    }

    private void UpdateContent(Treadmill treadmill, Purchase data)
    {
        if (titleText != null)
        {
            titleText.text = data.title;
        }

        if (priceText != null)
            priceText.text = data.priceValue;

        if (iconImage != null)
        {
            if (!iconImage.gameObject.activeSelf)
                iconImage.gameObject.SetActive(true);

            iconImage.Load(data.imageURI);
        }

        if (_currencyIcon != null &&
            !string.IsNullOrEmpty(data.currencyImageURL))
        {
            if (!_currencyIcon.gameObject.activeSelf)
                _currencyIcon.gameObject.SetActive(true);

            _currencyIcon.Load(data.currencyImageURL);
        }
    }

    private void BuyCurrentTreadmill()
    {
        if (_isClosing)
            return;

        if (_currentTreadmill == null)
            return;

        if (TreadmillPurchaseManager.Instance == null)
        {
            Debug.LogError(
                "[TreadmillWindow] TreadmillPurchaseManager not found!"
            );
            return;
        }
        
        // Запрещаем повторные клики во время открытия платежного окна
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        TreadmillPurchaseManager.Instance.Buy(_currentTreadmill);
    }

    public void Close()
    {
        if (windowRoot == null || !windowRoot.activeSelf)
            return;

        if (_isClosing)
            return;

        _isClosing = true;

        if (_animationCoroutine != null)
            StopCoroutine(_animationCoroutine);

        _animationCoroutine = StartCoroutine(AnimateClose());
    }

    private IEnumerator AnimateOpen()
    {
        windowTransform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, openDuration);

        // 0 -> 1.2
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutCubic(t);

            windowTransform.localScale = Vector3.Lerp(
                Vector3.zero,
                Vector3.one * overshootScale,
                eased
            );

            canvasGroup.alpha = Mathf.Clamp01(
                elapsed / Mathf.Max(0.01f, fadeDuration)
            );

            yield return null;
        }

        windowTransform.localScale = Vector3.one * overshootScale;
        canvasGroup.alpha = 1f;

        // 1.2 -> 1
        elapsed = 0f;
        duration = Mathf.Max(0.01f, overshootDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutCubic(t);

            windowTransform.localScale = Vector3.Lerp(
                Vector3.one * overshootScale,
                Vector3.one,
                eased
            );

            yield return null;
        }

        windowTransform.localScale = Vector3.one;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        _animationCoroutine = null;
    }

    private IEnumerator AnimateClose()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Vector3 startScale = windowTransform.localScale;
        float startAlpha = canvasGroup.alpha;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, closeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseInCubic(t);

            windowTransform.localScale = Vector3.Lerp(
                startScale,
                Vector3.zero,
                eased
            );

            canvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                0f,
                Mathf.Clamp01(
                    elapsed / Mathf.Max(0.01f, fadeDuration)
                )
            );

            yield return null;
        }

        HideImmediately();

        _currentTreadmill = null;
        _animationCoroutine = null;
        _isClosing = false;
    }

    private void HideImmediately()
    {
        if (windowTransform != null)
            windowTransform.localScale = Vector3.zero;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (windowRoot != null)
            windowRoot.SetActive(false);
    }

    private float EaseOutCubic(float t)
    {
        float value = 1f - t;
        return 1f - value * value * value;
    }

    private float EaseInCubic(float t)
    {
        return t * t * t;
    }
}