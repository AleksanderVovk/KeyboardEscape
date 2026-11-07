using DamageNumbersPro;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

[System.Serializable]
public enum Gender
{
    None,
    Man,
    Women
}

public class Player : MonoBehaviour
{
    public GameObject player;
    public Transform spawnPoint;

    [Header("Атрибуты и скорость")]
    public PlayerSpeed playerSpeed;
    public PlayerCharacteristic playerCharacteristics;

    [Header("Партиклы")]
    public ParticleSystem stunParticle;
    public ParticleSystem giveMoneyParticle;
    public ParticleSystem teleportParticle;

    [Header("Игровые данные")]
    public int sessionWins;
    public Gender gender;
    public bool thisRealPlayer;

    [Space(10f)]
    [Header("Ссылки")]
    [Space(2.5f)]
    public Animator animator;

    public PlayerState playerState;
    public PlayerMovement playerMovement;
    public NPCController npcController;
    public PlayerController playerController;
    public AI ai;

    [Header("UI атрибутов")]
    public TMP_Text attribute_speed_view;
    public TMP_Text attribute_strength_view;

    [Header("Респаун")]
    public Transform checkpoint;

    private AudioSource _audioSource;
    [SerializeField] private AudioClip _stunClip;

    [Header("Никнейм")]
    public string nickname;
    public TMP_Text _nicknameView;

    [Header("Счёт и деньги")]
    public DamageNumber damageNumber;
    public int score;

    [Header("Инвентарь")]
    public Transform hand;

    [Header("TsunamiBrainrot")]
    public TsunamiReference tsunamiReference;
    public TutorialManager tutorialManager;

    public BrainrotReward brainrotReward;
    public PlayerCloth playerCloth;

    [Header("Death UI")]
    [SerializeField] private CanvasGroup deathPanel;
    [SerializeField] private GameObject playerVisualRoot;
    [SerializeField] private float hideModelDelay = 0.2f;
    [SerializeField] private float deathFadeInTime = 0.25f;
    [SerializeField] private float deathPanelHoldTime = 0.5f;
    [SerializeField] private float deathFadeOutTime = 0.25f;

    private CharacterController characterController;

    private Coroutine _fallRoutine;
    private bool _isFalling;
    private bool _isDead; // Защита от повторной смерти

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        tutorialManager = GetComponent<TutorialManager>();
        playerCloth = GetComponentInChildren<PlayerCloth>();
        characterController = GetComponent<CharacterController>();

        if (playerSpeed == null) playerSpeed = GetComponent<PlayerSpeed>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (playerCharacteristics == null) playerCharacteristics = GetComponent<PlayerCharacteristic>();

        if (thisRealPlayer)
        {
            playerController = GetComponent<PlayerController>();
        }
        else
        {
            ai = GetComponent<AI>();
        }

        if (deathPanel != null)
        {
            deathPanel.alpha = 0f;
            deathPanel.gameObject.SetActive(false);
        }

        if (playerVisualRoot == null && player != null)
        {
            playerVisualRoot = player;
        }

        _isDead = false;
    }

    #region Деньги и партиклы

    public void SpawnMoneyView(float value)
    {
        Vector3 position = new Vector3(transform.position.x, transform.position.y + 3, transform.position.z);
        damageNumber.Spawn(position, value);
    }

    public void PlayGiveMoneyParticle()
    {
        giveMoneyParticle.Play();
    }

    #endregion

    #region Инициализация

    public void InitializationPlayer()
    {
        gender = YG2.saves.playerData.gender;
        tsunamiReference = GetComponent<TsunamiReference>();

        Debug.Log("Инициализация игрока");

        if (playerSpeed != null)
        {
            playerSpeed.InitRealPlayer();
            UpdateDisplayAttribute(false);
        }

        playerCloth.Initialization();
    }

    public void InitializationNPC()
    {
        //InitializationNickName();

        if (playerSpeed != null)
        {
            playerSpeed.InitNPC(ai, npcController);
        }

        playerCloth.ApplyRandomBotSkin();

        Debug.Log("Инициализация НПС");
    }

    public void InitializationBase(Base playerBase)
    {
        tsunamiReference = GetComponent<TsunamiReference>();
        if (tsunamiReference == null)
        {
            Debug.LogError("Player: TsunamiReference is missing");
            return;
        }

        tsunamiReference.Initialization(this, playerBase);

        spawnPoint = playerBase.spawnPoint;
        TeleportToSpawn();

        if (thisRealPlayer)
        {
            if (TutorialManager.instance != null)
                TutorialManager.instance.TryStartTutorial(playerBase.transform);
            else
                Debug.LogWarning("TutorialManager.instance is NULL (no TutorialManager in scene)");
        }

        if (!thisRealPlayer)
        {
            if (npcController == null)
            {
                Debug.LogError("Player: npcController is NULL on NPC player");
                return;
            }

            npcController.primaryBounds = playerBase.primaryBounds;
            npcController.secondaryBounds = playerBase.secondaryBounds;
            npcController.StartNPC();
        }
    }

    private void InitializationNickName()
    {
        if (_nicknameView != null)
        {
            _nicknameView.gameObject.SetActive(true);
            nickname = PlayersManager.GetRandomNickName();
            _nicknameView.text = nickname;
        }
    }

    #endregion

    #region Отображение атрибутов

    public void UpdateDisplayAttribute(bool needSave)
    {
        if (playerSpeed == null)
            return;

        var attributes = playerSpeed.additionalAttributes;

        if (attribute_speed_view != null)
        {
            float mainSpeed = (playerCharacteristics.PLAYER_DEFAULT_MOVE_SPEED + attributes.speed) * 1000f;
            attribute_speed_view.text = UTF.AbbreviateNumber(mainSpeed);
        }

        if (thisRealPlayer && needSave)
        {
            YG2.saves.playerData.attributes = attributes;
            YG2.SaveProgress();
        }

        playerSpeed.ApplySpeedFromAttributes();
    }

    #endregion

    #region Телепорт / падение / респаун

    public void TeleportPlayer(Vector3 position)
    {
        ControllerState(false);

        // Используем CharacterController для телепортации
        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;
        }
        else
        {
            transform.position = position;
        }

        ControllerState(true);
        _isDead = false; // Разрешаем смерть после телепортации
    }

    public void TeleportToSpawn()
    {
        TeleportPlayer(spawnPoint.position);

        if (npcController != null)
            npcController.Death();
    }

    public void Fall()
    {
        // Защита от повторной смерти до телепортации
        if (_isDead || _isFalling)
            return;

        _isDead = true;

        if (_fallRoutine != null)
            StopCoroutine(_fallRoutine);

        _fallRoutine = StartCoroutine(FallRoutine());
    }

    private IEnumerator FallRoutine()
    {
        _isFalling = true;

        stunParticle.Play();

        _audioSource.pitch = Random.Range(0.98f, 1f);
        _audioSource.PlayOneShot(_stunClip);

        ControllerState(false);

        if (playerVisualRoot != null)
        {
            var renderers = playerVisualRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
                r.enabled = false;
        }

        if (deathPanel != null)
        {
            deathPanel.gameObject.SetActive(true);
            deathPanel.alpha = 0f;

            yield return FadeCanvasGroup(deathPanel, 0f, 1f, 0.15f);
            yield return new WaitForSeconds(0.2f);
            yield return FadeCanvasGroup(deathPanel, 1f, 0f, 0.2f);

            deathPanel.gameObject.SetActive(false);
        }

        TeleportPlayer(spawnPoint.position);

        if (playerVisualRoot != null)
        {
            var renderers = playerVisualRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
                r.enabled = true;
        }

        if (npcController != null)
            npcController.Death();

        ControllerState(true);

        _isFalling = false;
        _fallRoutine = null;
    }

    public void Respawn()
    {
        if (checkpoint == null)
        {
            TeleportToSpawn();
        }
    }

    #endregion

    #region UI Fade

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float time = 0f;
        group.alpha = from;

        while (time < duration)
        {
            time += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }

        group.alpha = to;
    }

    #endregion

    #region Контроллер

    public void ControllerState(bool value)
    {
        // Управление только через CharacterController
        if (characterController != null)
        {
            characterController.enabled = value;
        }
    }

    #endregion

    private void ResetAnimator()
    {
        if (animator == null)
            return;

        animator.Rebind();
        animator.Update(0f);
    }
}