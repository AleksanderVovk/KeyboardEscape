using DG.Tweening;
using TMPro;
using UnityEngine;

public class BalancePopupView : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private RectTransform rect;

    private Tween _tween;

    private void Reset()
    {
        rect = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();
        text = GetComponentInChildren<TMP_Text>();
    }

    public void Play(long delta, float inTime, float outTime, float floatUp)
    {
        gameObject.SetActive(true);

        bool plus = delta >= 0;
        text.color = plus ? new Color(0.20f, 1f, 0.25f, 1f) : new Color(1f, 0.25f, 0.25f, 1f);
        text.text = (plus ? "+" : "-") + UTF.AbbreviateNumber(System.Math.Abs(delta)) + "$";

        rect.anchoredPosition = Vector2.zero; // строго в точке spawnPoint
        rect.localScale = Vector3.one * 0.9f;
        group.alpha = 0f;

        _tween?.Kill();
        rect.DOKill();
        group.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.Append(group.DOFade(1f, inTime).SetEase(Ease.OutQuad));
        seq.Join(rect.DOScale(1f, inTime).SetEase(Ease.OutBack));

        seq.Append(rect.DOAnchorPosY(floatUp, outTime).SetEase(Ease.OutQuad)); // от нуля вверх
        seq.Join(group.DOFade(0f, outTime).SetEase(Ease.InQuad));

        seq.OnComplete(() => gameObject.SetActive(false));
        _tween = seq;
    }
}
