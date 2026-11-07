using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCController : MonoBehaviour
{
    public enum NPCState
    {
        Wander,
        GoToBrainrot,
        WaitAndPickup,
        GoToBase
    }

    [Header("Zones (Colliders with bounds)")]
    public Collider primaryBounds;
    public Collider secondaryBounds;

    [Range(0f, 1f)]
    public float primaryChance = 0.8f;

    [Header("Wander")]
    public float repathInterval = 2.5f;
    public float edgePadding = 0.5f;
    public float arrivalDistance = 0.6f;
    public int sampleTries = 12;
    public float navmeshSampleRadius = 8f;

    [Header("Spawn -> NavMesh")]
    public bool warpToNavMeshOnStart = true;
    public float warpSearchRadius = 5f;

    [Header("Path Debug")]
    public bool drawPathGizmos = true;

    [Header("Animation (optional)")]
    public Animator animator;
    public string speedParam = "Magnitude";
    public string groundedBool = "isGrounded";

    [Header("Brainrot AI")]
    public float searchRadius = 15f;
    public LayerMask brainrotLayer;
    public Base npcBase;
    public NPCBrainrotCarrier carrier;
    public float waitBeforePickup = 2f;
    public float pickupDistance = 1.2f;

    [Header("Wave Avoidance")]
    public float waveCheckRadius = 10f;
    public LayerMask waveLayer;
    public Transform[] safePoints;
    public float safePointArriveDistance = 0.5f;

    [Header("Anti-stuck")]
    public float stuckSpeedThreshold = 0.1f;   // скорость ниже этого считаем "стоянием"
    public float stuckTime = 3f;              // сколько секунд подряд можно "стоять"
    public float minRepathTime = 1.5f;        // минимальный интервал перед сменой задачи

    private NavMeshAgent _agent;
    private float _timer;
    private Collider _activeZone;

    private bool _isDead;
    private bool _movementPaused;

    public float magnitude;

    private NPCState _state = NPCState.Wander;
    private BrainrotInWorld _targetBrainrot;
    private Vector3 _currentDestination;
    private Coroutine _waitCoroutine;

    // состояние укрытия
    private bool _isAvoidingWave;
    private Vector3 _savedDestination;
    private NPCState _savedState;

    // анти‑залипание
    private float _stuckTimer;
    private Vector3 _lastPosition;
    private float _lastRepathTime;

    [SerializeField] private float jumpHeight = 2.0f;
    [SerializeField] private float jumpDuration = 0.45f;

    private bool _isTraversingLink;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (carrier == null) carrier = GetComponent<NPCBrainrotCarrier>();

        _agent.autoTraverseOffMeshLink = false;
    }

    private void OnEnable()
    {
        _timer = Random.Range(0f, repathInterval);
        _lastPosition = transform.position;
    }

    private IEnumerator TraverseOffMeshLinkParabola()
    {
        _isTraversingLink = true;

        OffMeshLinkData data = _agent.currentOffMeshLinkData;
        Vector3 startPos = _agent.transform.position;
        Vector3 endPos = data.endPos + Vector3.up * _agent.baseOffset;

        float normalizedTime = 0.0f;

        // можно включить анимацию прыжка
        if (animator != null)
            animator.SetTrigger("JumpInWalking"); // если есть такой параметр

        while (normalizedTime < 1.0f)
        {
            float yOffset = jumpHeight * 4.0f * (normalizedTime - normalizedTime * normalizedTime);
            _agent.transform.position =
                Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * Vector3.up;

            normalizedTime += Time.deltaTime / jumpDuration;
            yield return null;
        }

        _agent.CompleteOffMeshLink();
        _isTraversingLink = false;
    }

    public void StartNPC()
    {
        if (primaryBounds == null || secondaryBounds == null)
        {
            Debug.LogWarning("NPCController: bounds not set");
            return;
        }

        _isDead = false;
        _movementPaused = false;

        _agent.updatePosition = true;
        _agent.updateRotation = true;

        if (warpToNavMeshOnStart && !_agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, warpSearchRadius, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
            }
            else
            {
                Debug.LogWarning("NPCController: cannot find NavMesh near spawn");
                return;
            }
        }

        _agent.ResetPath();
        _agent.isStopped = false;

        _activeZone = ChooseZone();
        bool ok = TrySetRandomDestinationInZone(_activeZone);

        if (!ok)
            ok = TrySetRandomDestinationInZone(_activeZone == primaryBounds ? secondaryBounds : primaryBounds);

        _timer = repathInterval;

        _state = NPCState.Wander;
        _targetBrainrot = null;
        _currentDestination = _agent.destination;

        _isAvoidingWave = false;

        _stuckTimer = 0f;
        _lastPosition = transform.position;
        _lastRepathTime = Time.time;
    }

    private void Update()
    {
        if (_agent == null) return;

        magnitude = _agent.velocity.magnitude;

        if (_isDead || _movementPaused)
        {
            if (_agent.enabled) _agent.isStopped = true;
            UpdateAnimator(0f, grounded: true);
            return;
        }

        // если на OffMeshLink — обрабатываем прыжок и выходим
        if (_agent.isOnOffMeshLink && !_isTraversingLink)
        {
            StartCoroutine(TraverseOffMeshLinkParabola());
            return;
        }

        if (primaryBounds == null || secondaryBounds == null)
            return;

        // анти‑залипание
        CheckStuck();

        // ----- проверка волны (только если бежим за брейнротом или на базу) -----
        if (_state == NPCState.GoToBrainrot || _state == NPCState.GoToBase || _isAvoidingWave)
        {
            HandleWaveAvoidance();
        }

        if (_isAvoidingWave)
        {
            // пока в режиме укрытия, логика состояний не обновляется
            UpdateAnimator(_agent.velocity.magnitude, grounded: !_agent.isOnOffMeshLink);
            return;
        }

        switch (_state)
        {
            case NPCState.Wander:
                UpdateWander();
                break;
            case NPCState.GoToBrainrot:
                UpdateGoToBrainrot();
                break;
            case NPCState.WaitAndPickup:
                break;
            case NPCState.GoToBase:
                UpdateGoToBase();
                break;
        }

        UpdateAnimator(_agent.velocity.magnitude, grounded: !_agent.isOnOffMeshLink);
    }

    // ---------- ANTI-STUCK LOGIC ----------

    private void CheckStuck()
    {
        // если агент отключён или путь ещё строится — ничего не делаем
        if (!_agent.enabled || _agent.pathPending)
            return;

        float speed = _agent.velocity.magnitude;
        float distMoved = (transform.position - _lastPosition).sqrMagnitude;

        bool isAlmostNotMoving = speed < stuckSpeedThreshold && distMoved < 0.01f;

        if (isAlmostNotMoving && !_agent.isStopped)
        {
            _stuckTimer += Time.deltaTime;
        }
        else
        {
            _stuckTimer = 0f;
        }

        _lastPosition = transform.position;

        // если слишком долго стоим — сбрасываем задачу
        if (_stuckTimer >= stuckTime && Time.time - _lastRepathTime > minRepathTime)
        {
            Debug.Log($"NPCController: {name} seems stuck in state {_state}, repathing...");

            _stuckTimer = 0f;
            _lastRepathTime = Time.time;

            // общий фолбэк: переводим в Wander с новым рандом‑путём
            _state = NPCState.Wander;
            _targetBrainrot = null;
            _agent.ResetPath();
            _agent.isStopped = false;

            _activeZone = ChooseZone();
            TrySetRandomDestinationInZone(_activeZone);
        }
    }

    // ---------- WAVE AVOIDANCE ----------

    private void HandleWaveAvoidance()
    {
        bool waveInRadius = Physics.CheckSphere(transform.position, waveCheckRadius, waveLayer);

        if (waveInRadius)
        {
            if (!_isAvoidingWave)
            {
                // включаем режим укрытия только если бот реально занят:
                if (_state == NPCState.GoToBrainrot || _state == NPCState.GoToBase)
                {
                    Transform bestSafe = FindNearestSafePoint();
                    if (bestSafe != null)
                    {
                        _isAvoidingWave = true;
                        _savedDestination = _currentDestination;
                        _savedState = _state;

                        Vector3 targetPos = bestSafe.position;
                        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, navmeshSampleRadius, NavMesh.AllAreas))
                        {
                            _currentDestination = hit.position;
                            _agent.isStopped = false;
                            _agent.SetDestination(_currentDestination);
                        }
                    }
                }
            }
            else
            {
                bool closeEnough =
                    !_agent.pathPending &&
                    _agent.remainingDistance <= Mathf.Max(safePointArriveDistance, _agent.stoppingDistance);

                if (closeEnough)
                {
                    _agent.isStopped = true;
                }
            }
        }
        else
        {
            if (_isAvoidingWave)
            {
                _isAvoidingWave = false;
                _agent.isStopped = false;

                _state = _savedState;
                if (NavMesh.SamplePosition(_savedDestination, out NavMeshHit hit, navmeshSampleRadius, NavMesh.AllAreas))
                {
                    _currentDestination = hit.position;
                    _agent.SetDestination(_currentDestination);
                }
            }
        }
    }

    private Transform FindNearestSafePoint()
    {
        if (safePoints == null || safePoints.Length == 0) return null;

        float bestDist = float.MaxValue;
        Transform best = null;

        Vector3 pos = transform.position;
        for (int i = 0; i < safePoints.Length; i++)
        {
            Transform p = safePoints[i];
            if (p == null) continue;

            float d = (p.position - pos).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = p;
            }
        }

        return best;
    }

    // ---------- WANDER ----------

    private void UpdateWander()
    {
        _timer -= Time.deltaTime;

        // НПС хочет пойти за брейнротом ТОЛЬКО если:
        //  - он НЕ несёт брейнрот
        //  - и на базе есть свободное место
        if (carrier != null && !carrier.HasBrainrot && npcBase != null && npcBase.HasFreeBrainrotPoint())
        {
            if (TryFindNearestBrainrot(out _targetBrainrot))
            {
                if (_targetBrainrot != null)
                {
                    Vector3 targetPos = _targetBrainrot.transform.position;
                    if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, navmeshSampleRadius, NavMesh.AllAreas))
                    {
                        _currentDestination = hit.position;
                        _agent.isStopped = false;
                        _agent.SetDestination(_currentDestination);
                        _state = NPCState.GoToBrainrot;
                        _lastRepathTime = Time.time;
                        return;
                    }
                }
            }
        }

        bool arrived = !_agent.pathPending &&
                       _agent.remainingDistance <= Mathf.Max(arrivalDistance, _agent.stoppingDistance);

        if (_activeZone == null)
            _activeZone = ChooseZone();

        if (!_activeZone.bounds.Contains(transform.position))
        {
            TrySetRandomDestinationInZone(_activeZone);
            _timer = repathInterval;
            _lastRepathTime = Time.time;
        }
        else if (arrived && _timer <= 0f)
        {
            _activeZone = ChooseZone();
            TrySetRandomDestinationInZone(_activeZone);
            _timer = repathInterval;
            _lastRepathTime = Time.time;
        }
    }

    private bool TryFindNearestBrainrot(out BrainrotInWorld result)
    {
        result = null;

        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius, brainrotLayer);
        if (hits == null || hits.Length == 0) return false;

        float bestDist = float.MaxValue;
        BrainrotInWorld best = null;

        for (int i = 0; i < hits.Length; i++)
        {
            BrainrotInWorld br = hits[i].GetComponent<BrainrotInWorld>();
            if (br == null) continue;
            if (br.state != BrainrotState.InWorld) continue;

            float dist = (br.transform.position - transform.position).sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = br;
            }
        }

        if (best != null)
        {
            result = best;
            return true;
        }

        return false;
    }

    // ---------- GOTO BRAINROT ----------

    private void UpdateGoToBrainrot()
    {
        // Если пока идём, место на базе закончилось — возвращаемся к Wander
        if (npcBase == null || !npcBase.HasFreeBrainrotPoint())
        {
            _targetBrainrot = null;
            _state = NPCState.Wander;
            return;
        }

        if (_targetBrainrot == null || _targetBrainrot.state != BrainrotState.InWorld)
        {
            _state = NPCState.Wander;
            return;
        }

        Vector3 targetPos = _targetBrainrot.transform.position;
        if ((targetPos - _currentDestination).sqrMagnitude > 0.5f * 0.5f)
        {
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, navmeshSampleRadius, NavMesh.AllAreas))
            {
                _currentDestination = hit.position;
                _agent.isStopped = false;
                _agent.SetDestination(_currentDestination);
                _lastRepathTime = Time.time;
            }
        }

        float dist = Vector3.Distance(transform.position, _targetBrainrot.transform.position);
        bool closeEnough = dist <= pickupDistance;

        if (closeEnough)
        {
            if (_waitCoroutine != null)
                StopCoroutine(_waitCoroutine);

            _waitCoroutine = StartCoroutine(WaitAndPickupCoroutine());
            _state = NPCState.WaitAndPickup;
        }
    }

    private IEnumerator WaitAndPickupCoroutine()
    {
        _agent.isStopped = true;
        float t = 0f;

        while (t < waitBeforePickup)
        {
            // Если брейнрота уже нет или он не в мире — отменяем
            if (_targetBrainrot == null || _targetBrainrot.state != BrainrotState.InWorld)
            {
                _agent.isStopped = false;
                _state = NPCState.Wander;
                yield break;
            }

            // Если пока ждём, место на базе кончилось — тоже отменяем
            if (npcBase == null || !npcBase.HasFreeBrainrotPoint())
            {
                _agent.isStopped = false;
                _state = NPCState.Wander;
                yield break;
            }

            t += Time.deltaTime;
            yield return null;
        }

        if (_targetBrainrot != null &&
            carrier != null &&
            carrier.TryPickup(_targetBrainrot))
        {
            _targetBrainrot = null;

            _agent.isStopped = false;

            if (npcBase != null && npcBase.spawnPoint != null)
            {
                Vector3 basePos = npcBase.spawnPoint.position;
                if (NavMesh.SamplePosition(basePos, out NavMeshHit hit, navmeshSampleRadius, NavMesh.AllAreas))
                {
                    _currentDestination = hit.position;
                    _agent.SetDestination(_currentDestination);
                    _state = NPCState.GoToBase;
                    _lastRepathTime = Time.time;
                }
                else
                {
                    _state = NPCState.Wander;
                }
            }
            else
            {
                _state = NPCState.Wander;
            }
        }
        else
        {
            _agent.isStopped = false;
            _state = NPCState.Wander;
        }
    }

    // ---------- GOTO BASE ----------

    private void UpdateGoToBase()
    {
        // Если уже не несём брейнрот — смысла идти на базу нет
        if (carrier == null || !carrier.HasBrainrot)
        {
            _state = NPCState.Wander;
            return;
        }

        if (npcBase == null || npcBase.spawnPoint == null)
        {
            _state = NPCState.Wander;
            return;
        }

        Vector3 targetPos = npcBase.spawnPoint.position;
        if ((targetPos - _currentDestination).sqrMagnitude > 0.5f * 0.5f)
        {
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, navmeshSampleRadius, NavMesh.AllAreas))
            {
                _currentDestination = hit.position;
                _agent.isStopped = false;
                _agent.SetDestination(_currentDestination);
                _lastRepathTime = Time.time;
            }
        }

        bool closeEnough =
            !_agent.pathPending &&
            _agent.remainingDistance <= Mathf.Max(arrivalDistance, _agent.stoppingDistance);

        if (closeEnough)
        {
            _state = NPCState.Wander;
        }
    }

    // ---------- HELPERS ----------

    private Collider ChooseZone()
    {
        return (Random.value <= primaryChance) ? primaryBounds : secondaryBounds;
    }

    private bool TrySetRandomDestinationInZone(Collider zone)
    {
        if (zone == null) return false;

        Bounds b = zone.bounds;

        for (int i = 0; i < sampleTries; i++)
        {
            Vector3 candidate = RandomPointInBoundsXZ(b, edgePadding);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navmeshSampleRadius, NavMesh.AllAreas))
            {
                _currentDestination = hit.position;
                bool ok = _agent.SetDestination(_currentDestination);
                if (ok)
                {
                    _agent.isStopped = false;
                    _lastRepathTime = Time.time;
                    return true;
                }
            }
        }

        return false;
    }

    private Vector3 RandomPointInBoundsXZ(Bounds b, float padding)
    {
        Vector2 r = Random.insideUnitCircle;

        float halfX = Mathf.Max(0.01f, b.extents.x - padding);
        float halfZ = Mathf.Max(0.01f, b.extents.z - padding);

        Vector3 p = b.center;
        p.x += r.x * halfX;
        p.z += r.y * halfZ;
        p.y = transform.position.y;
        return p;
    }

    // ---------- ANIM / MISC ----------

    private void UpdateAnimator(float speed, bool grounded)
    {
        if (animator == null) return;

        if (!string.IsNullOrEmpty(speedParam))
            animator.SetFloat(speedParam, speed);

        if (!string.IsNullOrEmpty(groundedBool))
            animator.SetBool(groundedBool, grounded);

        animator.SetBool("isRunning", magnitude > 0f);
    }

    public void PauseMovement(bool pause)
    {
        _movementPaused = pause;
        if (_agent != null) _agent.isStopped = pause;
    }

    public void Death()
    {
        _isDead = true;
        PauseMovement(true);
    }

    private void OnDrawGizmos()
    {
        if (drawPathGizmos)
        {
            var agent = _agent != null ? _agent : GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                var path = agent.path;
                if (path != null && path.corners != null && path.corners.Length >= 2)
                {
                    Gizmos.color = Color.cyan;
                    for (int i = 0; i < path.corners.Length - 1; i++)
                    {
                        Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
                        Gizmos.DrawSphere(path.corners[i], 0.12f);
                    }
                    Gizmos.DrawSphere(path.corners[^1], 0.12f);
                }
            }
        }

        // радиус проверки волны
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, waveCheckRadius);
    }
}