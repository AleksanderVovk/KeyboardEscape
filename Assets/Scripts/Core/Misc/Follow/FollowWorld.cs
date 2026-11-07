using UnityEngine;

public class FollowWorld : MonoBehaviour
{
    [Header("Target")]
    public Transform lookAt;
    public Vector3 offset;

    [Header("UI")]
    public CanvasGroup canvasGroup;

    [SerializeField] private Camera _camera;

    private bool _visible;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // стартуем скрытыми, но компонент Ќ≈ выключаем
        SetVisible(false, instant: true);
    }

    private void Start()
    {
        if (_camera == null)
            _camera = Camera.main;
    }

    private void LateUpdate()
    {
        if (!_visible) return;

        if (lookAt == null)
        {
            // цель пропала Ч просто скрываемс€, но продолжаем жить
            SetVisible(false, instant: true);
            return;
        }

        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null) return;
        }

        Vector3 screenPos = _camera.WorldToScreenPoint(lookAt.position + offset);
        transform.position = screenPos;
    }

    public void SetTarget(Transform target, Vector3 newOffset, bool showInstant = true)
    {
        lookAt = target;
        offset = newOffset;
        SetVisible(target != null, instant: showInstant);
    }

    public void ClearTarget(bool hideInstant = true)
    {
        lookAt = null;
        SetVisible(false, instant: hideInstant);
    }

    public void SetVisible(bool visible, bool instant)
    {
        _visible = visible;

        if (canvasGroup == null) return;

        if (instant)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }

        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    public bool IsVisible() => _visible;
}
