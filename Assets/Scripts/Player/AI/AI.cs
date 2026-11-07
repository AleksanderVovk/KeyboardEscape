using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AI : MonoBehaviour
{
    public NavMeshAgent agent;
    private Player _player;

    [SerializeField] private float searchRadius = 10f;
    [SerializeField] private LayerMask carryObjectLayer;
    private float _searchCooldown = 1f;
    private float _lastSearchTime;

    private Transform _currentTarget;
    private Vector3 _wanderPoint;
    private float _wanderRadius = 20f;
    private float _wanderTimer = 5f;
    private float _timer;
    private bool _isWandering = false;

    // Добавленные переменные для системы остановок
    private bool _isResting = false;
    private float _restTimer = 0f;
    private float _nextRestTime = 0f;
    [SerializeField] private float minRestTime = 3f;
    [SerializeField] private float maxRestTime = 8f;
    [SerializeField] private float minMoveTimeBeforeRest = 10f;
    [SerializeField] private float maxMoveTimeBeforeRest = 30f;
    private float _moveTimeBeforeRest = 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        _player = GetComponent<Player>();
    }

    private void Start()
    {
        _timer = _wanderTimer;
        _wanderPoint = GetRandomPoint();
        agent.SetDestination(_wanderPoint);
        _isWandering = true;

        _player.animator.SetBool("isGrounded", true);

        // Устанавливаем случайное время до следующей остановки
        _moveTimeBeforeRest = Random.Range(minMoveTimeBeforeRest, maxMoveTimeBeforeRest);
    }

    private void Update()
    {
        if (_isResting)
        {
            HandleResting();
            return;
        }

        // Отслеживаем время движения перед отдыхом
        _moveTimeBeforeRest -= Time.deltaTime;
        if (_moveTimeBeforeRest <= 0f)
        {
            StartResting();
            return;
        }

        // Если нет цели - бродим
        if (_currentTarget == null)
        {
            _isWandering = true;
            Wander();
        }

        // Проверка достижения точки блуждания
        if (_isWandering && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            _wanderPoint = GetRandomPoint();
            agent.SetDestination(_wanderPoint);
        }

        UpdateAnimations();
    }

    private void HandleResting()
    {
        _restTimer -= Time.deltaTime;

        // Обновляем анимацию для состояния покоя
        _player.animator.SetFloat("Magnitude", 0f);
        _player.animator.SetBool("isRunning", false);

        if (_restTimer <= 0f)
        {
            EndResting();
        }
    }

    private void StartResting()
    {
        _isResting = true;
        _restTimer = Random.Range(minRestTime, maxRestTime);
        agent.isStopped = true;

        // Можно добавить анимацию остановки
        _player.animator.SetTrigger("StartResting");
    }

    private void EndResting()
    {
        _isResting = false;
        agent.isStopped = false;
        _moveTimeBeforeRest = Random.Range(minMoveTimeBeforeRest, maxMoveTimeBeforeRest);

        // Возобновляем движение к текущей цели
        if (_currentTarget != null)
        {
            agent.SetDestination(_currentTarget.position);
        }
        else if (_isWandering)
        {
            agent.SetDestination(_wanderPoint);
        }

        // Можно добавить анимацию возобновления движения
        _player.animator.SetTrigger("EndResting");
    }

    private void UpdateAnimations()
    {
        if (_isResting) return;

        _player.animator.SetFloat("Magnitude", agent.velocity.magnitude);
        _player.animator.SetBool("isRunning", agent.velocity.magnitude > 0.1f);
    }

    private void SearchForCarryObject()
    {
        /*
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, searchRadius, carryObjectLayer);
        foreach (var hitCollider in hitColliders)
        {
            CarryObject carryObject = hitCollider.GetComponent<CarryObject>();
            if (carryObject != null && !carryObject.isPickup)
            {
                if (_player.additionalAttributes.strength >= carryObject.needStrength)
                {
                    _currentTarget = carryObject.transform;
                    agent.SetDestination(_currentTarget.position);
                    _isWandering = false;
                    return;
                }
            }
        }
        */


        // Получаем список всех доступных объектов для переноски

        // Если не нашли подходящих объектов - сбрасываем цель
        _currentTarget = null;
    }

    private void MoveToSellPoint()
    {
        if (PlayersManager.instance != null && PlayersManager.instance.sellPoint != null)
        {
            _currentTarget = PlayersManager.instance.sellPoint;
            agent.SetDestination(_currentTarget.position);
        }
    }

    private void Wander()
    {
        _timer += Time.deltaTime;

        if (_timer >= _wanderTimer || (_isWandering && !agent.hasPath))
        {
            _wanderPoint = GetRandomPoint();
            agent.SetDestination(_wanderPoint);
            _timer = 0;
        }
    }

    private Vector3 GetRandomPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * _wanderRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        Vector3 finalPosition = transform.position;
        if (NavMesh.SamplePosition(randomDirection, out hit, _wanderRadius, 1))
        {
            finalPosition = hit.position;
        }

        return finalPosition;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(_wanderPoint, 0.5f);
    }
}