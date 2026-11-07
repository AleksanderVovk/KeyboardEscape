using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCReplayController : MonoBehaviour
{
    public enum ReplayMode
    {
        TransformOnly,
        NavMeshAgentDriven,
        ExactTransform
    }

    [Header("Replay")]
    [SerializeField] private string recordingName = "recording.bin";
    [SerializeField] private ReplayMode replayMode = ReplayMode.ExactTransform;
    [SerializeField] private bool loopReplay = false;
    [SerializeField] private bool autoLoadOnStart = false;

    [Header("Smoothing")]
    [SerializeField] private float positionFollowSpeed = 12f;
    [SerializeField] private float rootRotationSpeed = 12f;
    [SerializeField] private float bodyRotationSpeed = 14f;

    [Header("Advanced Smoothing")]
    [SerializeField] private bool enablePositionSmoothing = true;
    [SerializeField] private float positionSmoothTime = 0.08f;
    [SerializeField] private bool enableRotationSmoothing = true;
    [SerializeField] private float rotationSmoothTime = 0.06f;
    [SerializeField] private float velocitySmoothTime = 0.1f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Rigidbody rigidbodyTarget;
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private Transform bodyRoot;

    [Header("Animator Params")]
    [SerializeField] private string magnitudeParam = "Magnitude";
    [SerializeField] private string runningBool = "isRunning";
    [SerializeField] private string ladderBool = "isLadder";
    [SerializeField] private string pushedBool = "isPushed";
    [SerializeField] private string groundedBool = "isGrounded";

    private PlayerReplayData _data;
    private int _index;
    [SerializeField] private float _playTime;
    [SerializeField] private bool _isReplaying;
    private float _recordingDuration;

    private Vector3 _smoothedVelocity;
    private Vector3 _positionVelocity;
    private float _rotationVelocity;
    private Vector3 _velocitySmooth;

    // Публичные свойства для менеджера
    public bool IsReplaying => _isReplaying;
    public string GetCurrentRecording() => recordingName;
    public bool IsLoading { get; private set; }

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (rigidbodyTarget == null) rigidbodyTarget = GetComponent<Rigidbody>();
        if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
        if (bodyRoot == null) bodyRoot = transform;

        if (navMeshAgent != null)
            navMeshAgent.enabled = false;

        if (rigidbodyTarget != null)
        {
            rigidbodyTarget.useGravity = false;
            rigidbodyTarget.isKinematic = true;
        }

        _positionVelocity = Vector3.zero;
        _velocitySmooth = Vector3.zero;
    }

    private void Start()
    {
        if (autoLoadOnStart)
        {
            LoadAndPlay(recordingName);
        }
    }

    // Основной метод загрузки и воспроизведения
    public void LoadAndPlay(string name)
    {
        Debug.Log($"[NPCReplay] LoadAndPlay: {name}");

        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("[NPCReplay] Recording name is null or empty!");
            return;
        }

        // Освобождаем старую запись
        if (!string.IsNullOrEmpty(recordingName))
        {
            ReplayLibrary.MarkRecordingFree(recordingName);
        }

        recordingName = name;

#if UNITY_WEBGL && !UNITY_EDITOR
    // В WebGL - загружаем через WebGLReplayLoader
    StartCoroutine(LoadFromWebGL(name));
#else
        LoadAndPlaySync(name);
#endif
    }

    private IEnumerator LoadFromWebGL(string name)
    {
        IsLoading = true;
        Debug.Log($"[NPCReplay] WebGL loading: {name}");

        // Ждем пока загрузится WebGLReplayLoader
        yield return new WaitUntil(() => WebGLReplayLoader.Instance != null && WebGLReplayLoader.Instance.IsInitialized);

        // Проверяем, есть ли запись в кэше
        var data = WebGLReplayLoader.Instance.GetRecording(name);

        if (data != null && data.IsValid)
        {
            _data = data;
            IsLoading = false;
            InitializeReplay();
            Debug.Log($"[NPCReplay] ✅ WebGL loaded: {name}, frames={data.frames.Count}");
            yield break;
        }

        // Если нет - загружаем асинхронно
        bool loaded = false;
        WebGLReplayLoader.Instance.LoadRecording(name, (loadedData) =>
        {
            _data = loadedData;
            IsLoading = false;
            loaded = true;

            if (_data != null && _data.IsValid)
            {
                InitializeReplay();
                Debug.Log($"[NPCReplay] ✅ WebGL async loaded: {name}");
            }
            else
            {
                Debug.LogError($"[NPCReplay] ❌ Failed to load: {name}");
                _isReplaying = false;
            }
        });

        // Ждем загрузки
        yield return new WaitUntil(() => loaded);
    }

    // Синхронная загрузка (для редактора)
    private void LoadAndPlaySync(string name)
    {
        _data = PlayerReplayIO.Load(name);
        InitializeReplay();
    }

    // Асинхронная загрузка (для WebGL)
    private IEnumerator LoadAndPlayAsync(string name)
    {
        IsLoading = true;
        Debug.Log($"[NPCReplay] Async loading: {name}");

        // Используем ReplayResourceManager для загрузки
        yield return ReplayResourceManager.LoadRecordingAsync(name, (data) =>
        {
            _data = data;
            IsLoading = false;

            if (_data != null && _data.IsValid)
            {
                InitializeReplay();
                Debug.Log($"[NPCReplay] ✅ Async loaded: {name}, frames={_data.frames.Count}");
            }
            else
            {
                Debug.LogError($"[NPCReplay] ❌ Failed to load async: {name}");
                _isReplaying = false;
            }
        });

        // Если загрузка ещё не завершена - ждём
        while (IsLoading)
            yield return null;
    }

    // Инициализация воспроизведения после загрузки
    private void InitializeReplay()
    {
        Debug.Log($"[NPCReplay] Data: valid={_data != null && _data.IsValid}, frames={_data?.frames.Count}");

        _index = 0;
        _playTime = 0f;
        _smoothedVelocity = Vector3.zero;
        _positionVelocity = Vector3.zero;
        _velocitySmooth = Vector3.zero;

        _isReplaying = _data != null && _data.IsValid && _data.frames.Count > 0;

        if (_isReplaying)
        {
            _recordingDuration = _data.frames[_data.frames.Count - 1].time;
            ReplayLibrary.MarkRecordingBusy(recordingName);
            Debug.Log($"[NPCReplay] ✅ STARTED replay: {recordingName}, frames={_data.frames.Count}, duration={_recordingDuration:F2}s");
        }
        else
        {
            Debug.LogError($"[NPCReplay] ❌ Failed to load recording: {recordingName}");
        }
    }

    // Загрузка из уже готовых данных (для кэширования)
    public void LoadFromData(PlayerReplayData data, string name)
    {
        Debug.Log($"[NPCReplay] LoadFromData: {name}");

        if (!string.IsNullOrEmpty(recordingName))
        {
            ReplayLibrary.MarkRecordingFree(recordingName);
        }

        recordingName = name;
        _data = data;

        _index = 0;
        _playTime = 0f;
        _smoothedVelocity = Vector3.zero;
        _positionVelocity = Vector3.zero;
        _velocitySmooth = Vector3.zero;

        _isReplaying = _data != null && _data.IsValid && _data.frames.Count > 0;

        if (_isReplaying)
        {
            _recordingDuration = _data.frames[_data.frames.Count - 1].time;
            ReplayLibrary.MarkRecordingBusy(recordingName);
            Debug.Log($"[NPCReplay] ✅ Loaded from data: {recordingName}, frames={_data.frames.Count}");
        }
        else
        {
            Debug.LogError($"[NPCReplay] ❌ Invalid data: {name}");
        }
    }

    public void StopReplay()
    {
        _isReplaying = false;

        if (!string.IsNullOrEmpty(recordingName))
        {
            ReplayLibrary.MarkRecordingFree(recordingName);
            Debug.Log($"[NPCReplay] StopReplay - Marked FREE: {recordingName}");
        }

        if (navMeshAgent != null && navMeshAgent.enabled)
            navMeshAgent.isStopped = true;
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(recordingName))
        {
            ReplayLibrary.MarkRecordingFree(recordingName);
            Debug.Log($"[NPCReplay] OnDestroy - Marked FREE: {recordingName}");
        }
    }

    private void FixedUpdate()
    {
        if (!_isReplaying || _data == null || _data.frames.Count == 0)
        {
            if (_isReplaying)
            {
                Debug.LogWarning($"[NPCReplay] Data invalid during replay, stopping: {recordingName}");
                StopReplay();
            }
            return;
        }

        _playTime += Time.fixedDeltaTime;

        // Конец записи по времени
        if (_playTime >= _recordingDuration)
        {
            Debug.Log($"[NPCReplay] End of recording by time: {recordingName}, playTime={_playTime:F2}, duration={_recordingDuration:F2}");

            if (loopReplay)
            {
                _index = 0;
                _playTime = 0f;
                Debug.Log($"[NPCReplay] Looping: {recordingName}");
                return;
            }

            // Конец записи — явно помечаем и уведомляем менеджера
            _isReplaying = false;
            ReplayLibrary.MarkRecordingFree(recordingName);
            Debug.Log($"[NPCReplay] FINISHED replay: {recordingName}");

            if (BotReplayManager.Instance != null)
            {
                Debug.Log($"[NPCReplay] Notifying BotReplayManager about finish: {recordingName}");
                BotReplayManager.Instance.OnBotFinished(this);
            }

            return;
        }

        // Интерполяция между кадрами
        while (_index < _data.frames.Count - 2 && _data.frames[_index + 1].time <= _playTime)
            _index++;

        ReplayFrame a = _data.frames[_index];
        ReplayFrame b = _data.frames[Mathf.Min(_index + 1, _data.frames.Count - 1)];
        float t = Mathf.InverseLerp(a.time, b.time, _playTime);

        ApplyInterpolatedFrame(a, b, t);
    }

    private void ApplyInterpolatedFrame(ReplayFrame a, ReplayFrame b, float t)
    {
        Vector3 targetPos = Vector3.Lerp(a.position, b.position, t);
        Quaternion targetRootRot = Quaternion.Slerp(a.rootRotation, b.rootRotation, t);
        Quaternion targetBodyL = Quaternion.Slerp(a.bodyLocalRotation, b.bodyLocalRotation, t);
        Vector3 targetVel = Vector3.Lerp(a.velocity, b.velocity, t);

        if (replayMode == ReplayMode.ExactTransform)
        {
            if (enablePositionSmoothing)
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    targetPos,
                    ref _positionVelocity,
                    positionSmoothTime,
                    Mathf.Infinity,
                    Time.fixedDeltaTime
                );
            }
            else
            {
                transform.position = Vector3.Lerp(
                    transform.position,
                    targetPos,
                    positionFollowSpeed * Time.fixedDeltaTime
                );
            }

            if (enableRotationSmoothing)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRootRot,
                    Time.fixedDeltaTime / Mathf.Max(0.001f, rotationSmoothTime)
                );
            }
            else
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRootRot,
                    rootRotationSpeed * Time.fixedDeltaTime
                );
            }
        }
        else if (replayMode == ReplayMode.NavMeshAgentDriven)
        {
            if (navMeshAgent != null)
            {
                if (!navMeshAgent.enabled) navMeshAgent.enabled = true;
                navMeshAgent.updatePosition = true;
                navMeshAgent.updateRotation = false;
                navMeshAgent.isStopped = false;

                Vector3 smoothedNavVel = Vector3.SmoothDamp(
                    navMeshAgent.velocity,
                    targetVel,
                    ref _velocitySmooth,
                    velocitySmoothTime,
                    Mathf.Infinity,
                    Time.fixedDeltaTime
                );

                navMeshAgent.speed = new Vector2(smoothedNavVel.x, smoothedNavVel.z).magnitude;
                navMeshAgent.velocity = smoothedNavVel;
            }

            if (enableRotationSmoothing)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRootRot,
                    Time.fixedDeltaTime / Mathf.Max(0.001f, rotationSmoothTime)
                );
            }
            else
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRootRot,
                    rootRotationSpeed * Time.fixedDeltaTime
                );
            }
        }
        else
        {
            transform.position = targetPos;
            transform.rotation = targetRootRot;
        }

        if (bodyRoot != null)
        {
            bodyRoot.localRotation = Quaternion.Slerp(
                bodyRoot.localRotation,
                targetBodyL,
                bodyRotationSpeed * Time.fixedDeltaTime
            );
        }

        byte mergedFlags = (byte)(a.flags | b.flags);
        UpdateAnimator(targetVel, mergedFlags);

        _smoothedVelocity = Vector3.SmoothDamp(
            _smoothedVelocity,
            targetVel,
            ref _velocitySmooth,
            velocitySmoothTime,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );
    }

    private void UpdateAnimator(Vector3 velocity, byte flags)
    {
        if (animator == null) return;

        float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;
        bool isRunning = (flags & ReplayFlags.Run) != 0 || horizontalSpeed > 0.15f;

        if (!string.IsNullOrEmpty(magnitudeParam))
            animator.SetFloat(magnitudeParam, horizontalSpeed);

        if (!string.IsNullOrEmpty(runningBool))
            animator.SetBool(runningBool, isRunning);

        if (!string.IsNullOrEmpty(ladderBool))
            animator.SetBool(ladderBool, (flags & ReplayFlags.OnLadder) != 0);

        if (!string.IsNullOrEmpty(pushedBool))
            animator.SetBool(pushedBool, (flags & ReplayFlags.IsPushed) != 0);

        if (!string.IsNullOrEmpty(groundedBool))
            animator.SetBool(groundedBool, (flags & ReplayFlags.Grounded) != 0);

        animator.SetFloat("Velocity", velocity.y);

        if ((flags & ReplayFlags.Jump) != 0)
            animator.SetTrigger("JumpInAir");
    }

    // Метод для проверки доступности записи
    public bool IsRecordingAvailable(string name)
    {
        return ReplayResourceManager.RecordingExists(name);
    }

    // Загрузить запись из StreamingAssets (асинхронно)
    public void LoadFromStreamingAssets(string name, System.Action<bool> callback = null)
    {
        StartCoroutine(LoadFromStreamingAssetsCoroutine(name, callback));
    }

    private IEnumerator LoadFromStreamingAssetsCoroutine(string name, System.Action<bool> callback)
    {
        yield return ReplayResourceManager.LoadRecordingAsync(name, (data) =>
        {
            if (data != null && data.IsValid)
            {
                LoadFromData(data, name);
                callback?.Invoke(true);
            }
            else
            {
                Debug.LogError($"[NPCReplay] Failed to load from StreamingAssets: {name}");
                callback?.Invoke(false);
            }
        });
    }
}