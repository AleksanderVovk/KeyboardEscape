using TMPro;
using UnityEngine;
using DG.Tweening;

public class TutorialStepView : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;
    public TMP_Text text;

    [Header("Anim In")]
    public float fadeInTime = 0.15f;
    public float scaleTime = 0.20f;
    public float startScale = 0.85f;
    public Ease scaleEase = Ease.OutBack;

    [Header("Anim Out")]
    public float fadeOutTime = 0.20f;

    private CanvasGroup _cg;
    private Sequence _seq;

    private void Awake()
    {
        if (root == null) root = gameObject;

        _cg = root.GetComponent<CanvasGroup>();
        if (_cg == null) _cg = root.AddComponent<CanvasGroup>();

        HideInstant();
    }

    public void HideInstant()
    {
        KillAnim();
        root.SetActive(false);
        _cg.alpha = 0f;
        root.transform.localScale = Vector3.one;
    }

    public void Show(LanguageDirectory msg)
    {
        if (text == null || root == null) return;
        text.text = UTF.ReadLanguageDirectory(msg);

        root.SetActive(true);
        PlayShowAnim();
    }

    public void ShowForSeconds(LanguageDirectory msg, float seconds)
    {
        if (text == null || root == null) return;
        text.text = UTF.ReadLanguageDirectory(msg);

        root.SetActive(true);
        PlayShowThenAutoHide(seconds);
    }

    public void Hide()
    {
        KillAnim();
        if (root != null) root.SetActive(false);
    }

    private void PlayShowAnim()
    {
        KillAnim();

        Transform t = root.transform;
        t.localScale = Vector3.one * startScale;
        _cg.alpha = 0f;

        _seq = DOTween.Sequence(); // Sequence + Join/AppendInterval [web:176][web:191]
        _seq.Join(_cg.DOFade(1f, fadeInTime));
        _seq.Join(t.DOScale(1f, scaleTime).SetEase(scaleEase));
        _seq.Play();
    }

    private void PlayShowThenAutoHide(float seconds)
    {
        KillAnim();

        Transform t = root.transform;
        t.localScale = Vector3.one * startScale;
        _cg.alpha = 0f;

        _seq = DOTween.Sequence(); // [web:176]
        _seq.Join(_cg.DOFade(1f, fadeInTime));
        _seq.Join(t.DOScale(1f, scaleTime).SetEase(scaleEase));

        _seq.AppendInterval(Mathf.Max(0f, seconds)); // подождать N секунд [web:191]
        _seq.Append(_cg.DOFade(0f, fadeOutTime));
        _seq.OnComplete(() =>
        {
            if (root != null) root.SetActive(false);
            // нормализуем скейл, чтобы следующее появление было корректным
            if (root != null) root.transform.localScale = Vector3.one;
        });

        _seq.Play();
    }

    private void KillAnim()
    {
        if (_seq != null && _seq.IsActive())
        {
            _seq.Kill();
            _seq = null;
        }
    }
}
