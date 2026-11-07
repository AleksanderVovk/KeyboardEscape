using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

[System.Serializable]
public class AdditionalAttributes
{
    public float speed; // прибавка к PLAYER_DEFAULT_MOVE_SPEED
}

public class PlayerSpeed : MonoBehaviour
{
    [Header("Ссылки")]
    public Player player;
    public PlayerMovement playerMovement;
    public PlayerCharacteristic playerCharacteristics;

    [Header("Атрибуты")]
    public AdditionalAttributes additionalAttributes;

    [Header("Множители")]
    public bool x2Speed;

    [Header("Скорость за шаг")]
    [SerializeField] private float _stepSpeed = 1f;

    [Header("Панель изменения скорости")]
    [SerializeField] private TMP_Text _changeSpeed_maxValue;
    [SerializeField] private Slider _sliderChangeSpeed;

    [Header("UI - Display")]
    [SerializeField] private TMP_Text _speedValueText;

    private float? _speedOverride;

    private void Awake()
    {
        if (player == null) player = GetComponent<Player>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (playerCharacteristics == null) playerCharacteristics = GetComponent<PlayerCharacteristic>();
    }

    #region Инициализация

    public void InitRealPlayer()
    {
        if (YG2.saves != null && YG2.saves.playerData != null)
            additionalAttributes = YG2.saves.playerData.attributes;

        if (additionalAttributes == null)
            additionalAttributes = new AdditionalAttributes();

        _speedOverride = null;

        SyncSpeedPanel();
        ApplySpeedFromAttributes();
    }

    public void InitNPC(AI ai, NPCController npcController)
    {
        if (additionalAttributes == null)
            additionalAttributes = new AdditionalAttributes();

        float playerBonus = YG2.saves != null && YG2.saves.playerData != null && YG2.saves.playerData.attributes != null
            ? YG2.saves.playerData.attributes.speed
            : 0f;

        additionalAttributes.speed =
            (playerCharacteristics.PLAYER_DEFAULT_MOVE_SPEED + Random.Range(0f, 0.25f)) +
            Random.Range(playerBonus / 4f, playerBonus * 1.25f);

        float moveSpeed = playerCharacteristics.PLAYER_DEFAULT_MOVE_SPEED + additionalAttributes.speed;
        if (playerMovement != null)
            playerMovement.SetSpeed(moveSpeed);

        if (ai != null && ai.agent != null)
            ai.agent.speed = additionalAttributes.speed * 6f;
    }

    private void UpdateSpeedValueText()
    {
        if (_speedValueText == null) return;

        float add = additionalAttributes != null ? additionalAttributes.speed * 20f : 0f;
        _speedValueText.SetText("{0}", add);
    }

    #endregion

    #region UI Panel

    private void SyncSpeedPanel()
    {
        if (_sliderChangeSpeed == null || _changeSpeed_maxValue == null)
            return;

        float max = additionalAttributes != null ? additionalAttributes.speed : 0f;

        _sliderChangeSpeed.minValue = 0.1f;
        _sliderChangeSpeed.maxValue = max;
        _changeSpeed_maxValue.text = ((max * 10f) * 2).ToString("F0");

        if (!_speedOverride.HasValue)
            _sliderChangeSpeed.SetValueWithoutNotify(max);
        else
            _sliderChangeSpeed.SetValueWithoutNotify(_speedOverride.Value);
    }

    public void OnSpeedSliderChanged()
    {
        if (_sliderChangeSpeed == null)
            return;

        _speedOverride = _sliderChangeSpeed.value;
        ApplySpeedFromAttributes();
    }

    public void SetMaxSpeedAndUnlock()
    {
        _speedOverride = null;
        SyncSpeedPanel();
        ApplySpeedFromAttributes();
    }

    public void SetMinMoveSpeed()
    {
        _speedOverride = 0.1f;
        SyncSpeedPanel();
        ApplySpeedFromAttributes();
    }

    #endregion

    #region Применение скорости

    public void ApplySpeedFromAttributes()
    {
        if (playerMovement == null || playerCharacteristics == null)
            return;

        float add = _speedOverride ?? (additionalAttributes != null ? additionalAttributes.speed : 0f);
        float baseSpeed = playerCharacteristics.PLAYER_DEFAULT_MOVE_SPEED + add;
        float finalSpeed = x2Speed ? baseSpeed * 2f : baseSpeed;

        playerMovement.SetSpeed(finalSpeed);

        UpdateSpeedValueText();
    }

    public void SetX2Speed(bool value)
    {
        x2Speed = value;
        ApplySpeedFromAttributes();
    }

    public void SetStepSpeed(float value)
    {
        _stepSpeed = Mathf.Max(0f, value);

        Debug.Log("Скорость фара шага: " + _stepSpeed);
    }

    public float GetStepSpeed()
    {
        return _stepSpeed;
    }

    #endregion

    #region Прокачка скорости

    public void AddSpeed(float amount, bool save = true)
    {
        if (additionalAttributes == null)
            additionalAttributes = new AdditionalAttributes();

        additionalAttributes.speed += amount;

        if (YG2.saves != null && YG2.saves.playerData != null)
        {
            YG2.saves.playerData.speedLvl++;
            YG2.saves.playerData.attributes = additionalAttributes;
        }

        if (save)
            YG2.SaveProgress();

        _speedOverride = null;

        SyncSpeedPanel();
        ApplySpeedFromAttributes();
    }

    public void SetSpeedFromLevel()
    {
        if (YG2.saves == null || YG2.saves.playerData == null)
            return;

        int lvl = YG2.saves.playerData.speedLvl;

        if (additionalAttributes == null)
            additionalAttributes = new AdditionalAttributes();

        additionalAttributes.speed = lvl * 0.1f;
        YG2.saves.playerData.attributes = additionalAttributes;

        _speedOverride = null;

        SyncSpeedPanel();
        ApplySpeedFromAttributes();
    }

    #endregion

    #region Движение за шаг

    public float GetMoveSpeedPerStep()
    {
        float step = _stepSpeed > 0f ? _stepSpeed : 1f;
        return step;
    }

    public float GetFinalMoveSpeedPerStep()
    {
        float step = GetMoveSpeedPerStep();
        return x2Speed ? step * 2f : step;
    }

    #endregion

    #region Debug/Info

    public bool IsSpeedLocked() => _speedOverride.HasValue;
    public float GetLockedAdditionalSpeed() => _speedOverride.GetValueOrDefault();

    #endregion
}