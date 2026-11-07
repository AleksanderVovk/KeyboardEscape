using UnityEngine;
using UnityEngine.AI;

public class BrainrotGuardBot : MonoBehaviour
{
    public Player targetPlayer;
    public PlayerCarry targetPlayerCarry;
    public NavMeshAgent agent;
    public Animator animator;

    [Header("Aggro zone (trigger collider)")]
    public Collider patrolZoneCollider;

    [Header("Logic")]
    public float recheckInterval = 0.2f;
    public float attackDistance = 2.0f;
    public float giveUpDistance = 1.0f;

    [Header("Return point")]
    public Transform returnPoint; // если не задано, возьмём позицию самого бота

    private Vector3 _homePosition;

    private float _recheckTimer;
    private bool _isAggro;
    private bool _isAttacking;

    private enum State { Idle, Chase, Attack }
    private State _state = State.Idle;

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();

        if (patrolZoneCollider == null)
            patrolZoneCollider = GetComponent<Collider>();

        _homePosition = returnPoint != null
            ? returnPoint.position
            : transform.position;

        agent.stoppingDistance = attackDistance;
        agent.autoBraking = true;

        _state = State.Idle;
    }

    private void Update()
    {
        UpdateAnimator();

        if (agent == null || patrolZoneCollider == null) return;

        _recheckTimer -= Time.deltaTime;
        if (_recheckTimer <= 0f)
        {
            _recheckTimer = recheckInterval;

            CheckPlayersInZoneAndMaybeTeleport();
            UpdateAggroLogicMulti();
        }

        UpdateStateLogic();
    }

    // ---------- ПРОВЕРКА ИГРОКОВ В ЗОНЕ И ТЕЛЕПОРТ ----------

    private void CheckPlayersInZoneAndMaybeTeleport()
    {
        if (patrolZoneCollider == null) return;

        Bounds bounds = patrolZoneCollider.bounds;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player"); // [web:29]
        bool anyPlayerInside = false;

        for (int i = 0; i < players.Length; i++)
        {
            Transform t = players[i].transform;
            if (bounds.Contains(t.position))
            {
                anyPlayerInside = true;
                break;
            }
        }

        if (!anyPlayerInside)
        {
            TeleportToHome();
        }
    }

    private void TeleportToHome()
    {
        if (agent != null)
        {
            agent.ResetPath();
            agent.Warp(_homePosition);
        }
        else
        {
            transform.position = _homePosition;
        }

        _isAggro = false;
        _isAttacking = false;
        targetPlayer = null;
        targetPlayerCarry = null;
        _state = State.Idle;
    }

    // ---------- AGGRO НА ВСЕХ ИГРОКОВ В ЗОНЕ ----------

    private void UpdateAggroLogicMulti()
    {
        if (patrolZoneCollider == null) return;

        Bounds bounds = patrolZoneCollider.bounds;

        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player"); // [web:29]
        Player bestPlayer = null;
        PlayerCarry bestCarry = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < playerObjects.Length; i++)
        {
            Player p = playerObjects[i].GetComponent<Player>();
            if (p == null) continue;

            Vector3 pos = p.transform.position;
            if (!bounds.Contains(pos))
                continue;

            PlayerCarry carry = null;
            if (p.tsunamiReference != null)
                carry = p.tsunamiReference.playerCarry;

            // если тебе всё равно на brainrot — закомментируй этот блок
            // bool hasBrainrot = carry != null && carry.brainrotInHand != null;
            // if (!hasBrainrot) continue;

            float dist = Vector3.Distance(transform.position, pos);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestPlayer = p;
                bestCarry = carry;
            }
        }

        if (bestPlayer != null)
        {
            targetPlayer = bestPlayer;
            targetPlayerCarry = bestCarry;
            _isAggro = true;

            if (_state != State.Attack)
                _state = State.Chase;
        }
        else
        {
            _isAggro = false;
            targetPlayer = null;
            targetPlayerCarry = null;

            if (_state == State.Chase || _state == State.Attack)
            {
                _state = State.Idle;
                _isAttacking = false;
                agent.ResetPath();
            }
        }
    }

    // ---------- STATE LOGIC ----------

    private void UpdateStateLogic()
    {
        switch (_state)
        {
            case State.Idle:
                UpdateIdle();
                break;
            case State.Chase:
                UpdateChase();
                break;
            case State.Attack:
                // атака анимацией, логика в эвентах аниматора
                break;
        }
    }

    private void UpdateIdle()
    {
        // просто стоим на месте, агент никуда не идёт
        if (agent.hasPath)
            agent.ResetPath();
    }

    private void UpdateChase()
    {
        if (_isAttacking || patrolZoneCollider == null || targetPlayer == null) return;

        Bounds b = patrolZoneCollider.bounds;
        Vector3 playerPos = targetPlayer.transform.position;

        Vector3 clamped = ClampPositionToBounds(playerPos, b);
        agent.SetDestination(clamped);

        float dist = Vector3.Distance(transform.position, playerPos);
        if (dist <= attackDistance)
        {
            _state = State.Attack;
            _isAttacking = true;

            agent.ResetPath();
            //if (animator != null)
                //animator.SetTrigger("Attack");
        }
    }

    private static Vector3 ClampPositionToBounds(Vector3 pos, Bounds b)
    {
        float x = Mathf.Clamp(pos.x, b.min.x, b.max.x);
        float y = Mathf.Clamp(pos.y, b.min.y, b.max.y);
        float z = Mathf.Clamp(pos.z, b.min.z, b.max.z);
        return new Vector3(x, y, z);
    }

    public void Attack()
    {
        // вызывается эвентом из анимации
        if (targetPlayer == null) return;

        _isAttacking = false;
        if (_isAggro)
            _state = State.Chase;
        else
        {
            _state = State.Idle;
        }
    }

    public void EndAttack()
    {
        _isAttacking = false;
        if (_isAggro)
            _state = State.Chase;
        else
        {
            _state = State.Idle;
        }

        if (targetPlayer == null) return;

        float dist = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (dist <= attackDistance * 1.2f)
        {
            if (PlayersManager.instance != null && PlayersManager.instance.realPlayer != null)
                PlayersManager.instance.realPlayer.Fall();
        }
    }

    // ---------- ANIMATOR: Idle / Walk ----------

    private void UpdateAnimator()
    {
        if (animator == null || agent == null) return;

        float speed = agent.velocity.magnitude; // текущая скорость агента [web:39]

        bool isWalking = speed > 0.05f;

        // Предполагаем, что в аниматоре есть bool-параметр "IsWalking".
        // Idle — стейт по умолчанию, Walk — переход при IsWalking == true [web:35][web:38][web:40].
        animator.SetBool("IsWalking", isWalking);
    }

    private void OnDrawGizmosSelected()
    {
        if (patrolZoneCollider == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(patrolZoneCollider.bounds.center, patrolZoneCollider.bounds.size);
    }
}