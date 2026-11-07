using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class NotificationView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text messageText;

    [Header("Colors")]
    [SerializeField] private Color errorColor = Color.red;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color colorWin = Color.yellow;
    [SerializeField] private Color infoColor = Color.white;

    [Header("Animation")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float showDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.2f;
    [SerializeField] private float lifetime = 2.5f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Sequence showSequence;
    private Sequence hideSequence;
    private bool isHiding;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        showSequence?.Kill();
        hideSequence?.Kill();
        CancelInvoke();

        isHiding = false;
        canvasGroup.alpha = 0f;
        rect.localScale = Vector3.zero;
    }

    public void Setup(Sprite icon, string text, NotificationType type)
    {
        isHiding = false;

        if (messageText != null)
        {
            messageText.text = text;
            messageText.color = GetColorByType(type);
        }

        if (iconImage != null)
        {
            if (icon == null)
            {
                iconImage.enabled = false;
            }
            else
            {
                iconImage.enabled = true;
                iconImage.sprite = icon;
            }
        }

        if (useAnimation)
            PlayShowAnimation();
        else
        {
            canvasGroup.alpha = 1f;
            rect.localScale = Vector3.one;
        }

        if (lifetime > 0f)
            Invoke(nameof(StartHide), lifetime);
    }

    private Color GetColorByType(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Error:
                return errorColor;
            case NotificationType.Success:
                return successColor;
            case NotificationType.Win:
                return colorWin;
            case NotificationType.Info:
            default:
                return infoColor;
        }
    }

    private void PlayShowAnimation()
    {
        if (rect == null) return;

        showSequence?.Kill();
        hideSequence?.Kill();

        rect.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        showSequence = DOTween.Sequence()
            .Join(rect.DOScale(1f, showDuration).SetEase(showEase))
            .Join(canvasGroup.DOFade(1f, showDuration))
            .SetUpdate(true);

        UISound.manager.PlayPop(true);
    }

    public void StartHide()
    {
        if (!gameObject.activeInHierarchy || isHiding) return;

        isHiding = true;

        showSequence?.Kill();
        hideSequence?.Kill();
        CancelInvoke();

        if (useAnimation)
        {
            hideSequence = DOTween.Sequence()
                .Join(canvasGroup.DOFade(0f, hideDuration).SetEase(hideEase))
                .Join(rect.DOScale(0f, hideDuration).SetEase(hideEase))
                .OnComplete(HandleCompleteHide)
                .SetUpdate(true);
        }
        else
        {
            HandleCompleteHide();
        }
    }

    private void OnDisable()
    {
        showSequence?.Kill();
        hideSequence?.Kill();
        CancelInvoke();
        isHiding = false;
    }

    private void OnDestroy()
    {
        showSequence?.Kill();
        hideSequence?.Kill();
    }

    private void HandleCompleteHide()
    {
        gameObject.SetActive(false);

        if (NotificationManager.Instance != null)
            NotificationManager.Instance.RegisterHidden(this);
    }
}