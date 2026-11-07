using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager instance;

    [Header("Refs")]
    public Player player;
    public FollowWorld followInteraction;

    [Header("Detection")]
    public float detectRadius = 3f;
    public float detectInterval = 0.2f;
    public LayerMask interactionLayer;

    [Header("State")]
    public Interaction currentInteraction;
    public bool isDown;

    [Space(20f)]
    [SerializeField] private TMP_Text _name;
    [SerializeField] private TMP_Text _description;
    [SerializeField] private Image _fillImage;

    [Header("UI Fade Settings")]
    [SerializeField] private float fadeSpeed = 3f;
    private bool isUIVisible = false;
    private bool isFading = false;

    [Header("Speed Settings")]
    [SerializeField] private float defaultSpeed = 2f;
    private float _currentSpeed;

    private readonly Collider[] _overlapResults = new Collider[32];
    private float _detectTimer;

    private void Awake()
    {
        instance = this;
        _currentSpeed = defaultSpeed;
    }

    private void Update()
    {
        if (player == null || !player.thisRealPlayer)
            return;

        HandleInput();

        _detectTimer -= Time.deltaTime;
        if (_detectTimer <= 0f)
        {
            _detectTimer = detectInterval;
            UpdateClosestInteraction();
        }

        UpdateFill();
        UpdateFade();
    }

    private void HandleInput()
    {
        if (!YG2.envir.isDesktop)
            return;

        if (Input.GetKeyDown(KeyCode.E))
            SetDownState(true);
        else if (Input.GetKeyUp(KeyCode.E))
            SetDownState(false);
    }

    private void UpdateFill()
    {
        if (isDown && currentInteraction != null && currentInteraction.canInteraction)
        {
            if (_fillImage.fillAmount < 1f)
                _fillImage.fillAmount += _currentSpeed * Time.deltaTime;
            else
                Action();
        }
        else
        {
            if (_fillImage.fillAmount > 0f)
                _fillImage.fillAmount -= _currentSpeed * Time.deltaTime;
        }
    }

    private void UpdateFade()
    {
        if (!isFading || followInteraction == null || followInteraction.canvasGroup == null)
            return;

        float targetAlpha = isUIVisible ? 1f : 0f;
        float currentAlpha = followInteraction.canvasGroup.alpha;

        followInteraction.canvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);

        if (Mathf.Abs(followInteraction.canvasGroup.alpha - targetAlpha) < 0.01f)
        {
            followInteraction.canvasGroup.alpha = targetAlpha;
            isFading = false;

            if (!isUIVisible)
                followInteraction.gameObject.SetActive(false);
        }
    }

    private void UpdateClosestInteraction()
    {
        if (player == null)
        {
            if (currentInteraction != null)
                NullInteraction();
            return;
        }

        Vector3 center = player.transform.position;

        int count = Physics.OverlapSphereNonAlloc(
            center,
            detectRadius,
            _overlapResults,
            interactionLayer,
            QueryTriggerInteraction.Collide
        ); // возвращает количество коллайдеров, записанных в буфер [web:234]

        Interaction closest = null;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            Collider col = _overlapResults[i];
            if (col == null) continue;

            Interaction it = col.GetComponentInParent<Interaction>();
            if (it == null || !it.canInteraction) continue;

            // ВАЖНО: дистанцию лучше считать до коллайдера/точки, а не до transform Interaction.
            Vector3 p = col.ClosestPoint(center);
            float sqr = (p - center).sqrMagnitude;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                closest = it;
            }
        }

        if (closest == null)
        {
            if (currentInteraction != null)
                NullInteraction();
            return;
        }

        // 1) Если интеракция сменилась — обновляем тексты/скорость/сбрасываем hold
        if (closest != currentInteraction)
        {
            currentInteraction = closest;

            _name.text = UTF.ReadLanguageDirectory(currentInteraction.interactionName);
            _description.text = UTF.ReadLanguageDirectory(currentInteraction.interactionDescription);

            _currentSpeed = currentInteraction.holdSpeed > 0 ? currentInteraction.holdSpeed : defaultSpeed;

            _fillImage.fillAmount = 0f;
            SetDownState(false);
        }

        // 2) ВСЕГДА обновляем target/offset и показываем UI, даже если interaction тот же самый
        Vector3 off = currentInteraction.offset;
        followInteraction.SetTarget(currentInteraction.transform, off, showInstant: true);

        if (!isUIVisible || !followInteraction.IsVisible())
            SetVisibleUI(true);
    }

    public void SetDownState(bool state) => isDown = state;

    public void SetVisibleUI(bool visible)
    {
        isUIVisible = visible;
        isFading = true;

        if (followInteraction != null && visible)
            followInteraction.gameObject.SetActive(true);
    }

    public void NullInteraction()
    {
        SetDownState(false);
        SetVisibleUI(false);

        currentInteraction = null;
        _name.text = "";
        _description.text = "";
        _currentSpeed = defaultSpeed;
        _fillImage.fillAmount = 0f;

        if (followInteraction != null)
            followInteraction.ClearTarget(hideInstant: true);
    }

    public void Action()
    {
        SetDownState(false);
        _fillImage.fillAmount = 0f;
        SetVisibleUI(false);

        if (currentInteraction == null || !currentInteraction.canInteraction)
            return;

        currentInteraction.Action(PlayersManager.instance.realPlayer);
        currentInteraction = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.transform.position, detectRadius);
    }
}
