using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Footsteps : MonoBehaviour
{
    public float walkStepDelay;
    public float runStepDelay;

    public float _lastStepDelay;

    private PlayerState _playerState;
    private Player _player;
    private NPCController _npcController;

    [SerializeField] private AudioClip[] _defaultClips;
    [SerializeField] private AudioSource _audioSource;

    private void Awake()
    {
        _playerState = GetComponent<PlayerState>();
        _player = GetComponent<Player>();
        _npcController = GetComponent<NPCController>();
    }

    private void Update()
    {
        runStepDelay = 0.35f / _player.playerMovement.currentMoveSpeed;

        FootstepsHandler();
    }

    private void FootstepsHandler()
    {
        if (!_playerState.isWalking && !_playerState.isRunning || !_playerState.isGrounded)
            return;

        if (_npcController != null)
        {
            if (_npcController.magnitude < 0.1f)
                return;
        }

        float currentStepDelay = _playerState.isRunning ? runStepDelay : walkStepDelay;

        if (currentStepDelay + _lastStepDelay < Time.time)
        {
            _audioSource.PlayOneShot(_defaultClips[Random.Range(0, _defaultClips.Length)]);

            _lastStepDelay = Time.time;

        }
    }
}
