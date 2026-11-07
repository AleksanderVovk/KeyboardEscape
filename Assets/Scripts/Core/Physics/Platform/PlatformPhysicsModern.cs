using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformPhysicsModern : MonoBehaviour
{
    public Transform[] waypoints; // ������ �����, �� ������� ��������� ����� ���������
    public float travelTime;      // ����� �������� ����� �������
    public bool canRotation;      // ���������� ��� �������� ������������� ���������
    public float rotationSpeed = 2f; // �������� ��������

    private Rigidbody rb;
    private int currentWaypointIndex = 0; // ������ ������� �����
    private Vector3 currentPos;
    private Vector3 previousPos; // ��� ������� �������� ���������
    private Vector3 platformVelocity; // �������� ���������

    private CharacterController cc;
    private Transform playerTransform; // ��������� ������
    private PlayerState _playerState;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        previousPos = rb.position; // �������������� ���������� �������
    }

    private void FixedUpdate()
    {
        // ��������� ���������� ������� ���������
        previousPos = currentPos;

        if (waypoints.Length == 0) return; // ���� ��� �����, �������

        // ������ ������� ������� ���������
        float t = Mathf.PingPong(Time.time / travelTime + currentWaypointIndex, 1);
        currentPos = Vector3.Lerp(waypoints[currentWaypointIndex].position,
                                   waypoints[(currentWaypointIndex + 1) % waypoints.Length].position, t);

        // ���������� ���������
        rb.MovePosition(currentPos);

        // ������� ������� � ��������� �����
        if (canRotation)
        {
            Vector3 targetDirection = waypoints[(currentWaypointIndex + 1) % waypoints.Length].position - transform.position;
            targetDirection.y = 0; // ���������� ������� �� ���������
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        // ���������, �������� �� �� ��������� �����
        if (t >= 0.99f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length; // ������� � ��������� �����
        }

        // ��������� �������� ���������
        platformVelocity = (currentPos - previousPos) / Time.fixedDeltaTime;

        if (_playerState == null)
            return;

        // ���� ����� �� ���������, ���������� ��� �������
        if (_playerState.onPlatform && cc != null)
        {
            // ���������� ������ � ��� �� ���������, ��� � ���������
            cc.Move(platformVelocity * Time.fixedDeltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            cc = other.GetComponent<CharacterController>();
            _playerState = other.GetComponent<PlayerState>();

            playerTransform = other.transform;
            _playerState.onPlatform = true;

            // ����������� ������ � ���������
            playerTransform.SetParent(transform, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // ���������� ������ �� ���������
            playerTransform.SetParent(null);
            cc = null;
            playerTransform = null;
            _playerState.onPlatform = false;

            _playerState = null;
        }
    }
}