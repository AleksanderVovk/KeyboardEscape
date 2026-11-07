using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BrainrotReward : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panel;      // корневой объект UI
    [SerializeField] private Image iconImage;       // иконка брейнрота
    [SerializeField] private TMP_Text nameText;     // название брейнрота
    [SerializeField] private Button returnButton;   // кнопка "вернуть"
    [SerializeField] private Image fillImage;       // Image с FillAmount (Type = Filled)
    [SerializeField] private float duration = 8f;   // врем€ на реакцию

    [Header("Animation")]
    [SerializeField] private float showDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.2f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    private BrainrotInWorld _brainrot;
    private Player _player;
    private Coroutine _timerRoutine;

    private CanvasGroup _canvasGroup;
    private Tween _showTween;
    private Tween _hideTween;

    private void Awake()
    {
        if (panel != null)
        {
            _canvasGroup = panel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = panel.AddComponent<CanvasGroup>();

            // начальное состо€ние: спр€тано
            panel.SetActive(false);
            _canvasGroup.alpha = 0f;
            panel.transform.localScale = Vector3.one * 0.6f;
        }
    }

    public void Show(Player player, BrainrotInWorld brainrot)
    {
        _player = player;
        _brainrot = brainrot;

        // если брейнрота уже нет Ч сразу скрываемс€
        if (_brainrot == null)
        {
            HideInstant();
            return;
        }

        // заполн€ем UI
        if (iconImage != null)
            iconImage.sprite = _brainrot.brainrot.icon;

        if (nameText != null)
            nameText.text = UTF.ReadLanguageDirectory(_brainrot.brainrot.brainrotName);

        // настраиваем кнопку
        if (returnButton != null)
        {
            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(OnReturnPressed);
        }

        // стартовое заполнение
        if (fillImage != null)
            fillImage.fillAmount = 1f;

        // запускаем Ђвжухї по€влени€
        PlayShowAnimation();

        if (_timerRoutine != null)
            StopCoroutine(_timerRoutine);

        _timerRoutine = StartCoroutine(TimerRoutine());
    }

    private IEnumerator TimerRoutine()
    {
        float t = duration;

        while (t > 0f)
        {
            // если брейнрот уничтожен/исчез Ч закрываем UI
            if (_brainrot == null)
            {
                Hide();
                yield break;
            }

            t -= Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);

            if (fillImage != null)
                fillImage.fillAmount = normalized;

            yield return null;
        }

        // врем€ вышло, просто пр€чем UI
        Hide();
    }

    public void OnReturnPressed()
    {
        // если брейнрота уже нет Ч просто скрываем UI
        if (_brainrot == null || _player == null)
        {
            Hide();
            return;
        }

        // возвращаем брейнрот игроку в руки
        var carry = _player.tsunamiReference != null ? _player.tsunamiReference.playerCarry : null;

        if (carry != null)
        {
            carry.SetCarryObject(_brainrot);
        }

        Hide();
    }

    private void PlayShowAnimation()
    {
        if (panel == null || _canvasGroup == null)
            return;

        // останавливаем предыдущие твины
        _showTween?.Kill();
        _hideTween?.Kill();

        panel.SetActive(true);

        // начальные значени€ дл€ Ђвжухаї
        _canvasGroup.alpha = 0f;
        panel.transform.localScale = Vector3.one * 0.6f;

        _showTween = DOTween.Sequence()
            .Join(_canvasGroup.DOFade(1f, showDuration))
            .Join(panel.transform.DOScale(1f, showDuration))
            .SetEase(showEase);
    }

    private void PlayHideAnimation(System.Action onComplete)
    {
        if (panel == null || _canvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        _showTween?.Kill();
        _hideTween?.Kill();

        _hideTween = DOTween.Sequence()
            .Join(_canvasGroup.DOFade(0f, hideDuration))
            .Join(panel.transform.DOScale(0.6f, hideDuration))
            .SetEase(hideEase)
            .OnComplete(() =>
            {
                panel.SetActive(false);
                onComplete?.Invoke();
            });
    }

    public void Hide()
    {
        if (_timerRoutine != null)
        {
            StopCoroutine(_timerRoutine);
            _timerRoutine = null;
        }

        PlayHideAnimation(() =>
        {
            _brainrot = null;
            _player = null;
        });
    }

    private void HideInstant()
    {
        if (_timerRoutine != null)
        {
            StopCoroutine(_timerRoutine);
            _timerRoutine = null;
        }

        if (_showTween != null) _showTween.Kill();
        if (_hideTween != null) _hideTween.Kill();

        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;

        if (panel != null)
        {
            panel.transform.localScale = Vector3.one * 0.6f;
            panel.SetActive(false);
        }

        _brainrot = null;
        _player = null;
    }
}
