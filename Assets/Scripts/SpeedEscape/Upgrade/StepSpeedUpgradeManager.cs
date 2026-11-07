using System;
using System.Collections.Generic;
using UnityEngine;
using YG;

public class StepSpeedUpgradeManager : MonoBehaviour
{
    public static StepSpeedUpgradeManager Instance;

    [Serializable]
    public class SpeedTier
    {
        public int requiredCups;
        public int speedPerStep = 1;
        public StepSpeedTrigger trigger;
    }

    [Header("Tiers")]
    [SerializeField] private List<SpeedTier> tiers = new List<SpeedTier>();

    [Header("Default")]
    [SerializeField] private int defaultSpeedPerStep = 1;

    public SpeedTier _currentTier;

    private void Awake()
    {
        Instance = this;

        // Сортируем тиры по requiredCups (на случай если в инспекторе не по порядку)
        tiers.Sort((a, b) => a.requiredCups.CompareTo(b.requiredCups));
    }

    private void OnEnable()
    {
        ValueSystem.OnBalanceChanged += OnBalanceChanged;
    }

    private void OnDisable()
    {
        ValueSystem.OnBalanceChanged -= OnBalanceChanged;
    }

    private void Start()
    {
        // При старте: выбираем лучший доступный тир
        AutoSelectBestTier();
        UpdateAllTriggersVisual();
        ApplyCurrentStepSpeed();
    }

    private void OnBalanceChanged()
    {
        // При изменении кубков: обновляем доступность и пересчитываем лучший тир
        UpdateAllTriggersVisual();
        AutoSelectBestTier();
    }

    public bool IsTriggerActive(StepSpeedTrigger trigger)
    {
        return _currentTier != null && _currentTier.trigger == trigger;
    }

    /// <summary>
    /// Автоматически выбрать лучший ДОСТУПНЫЙ тир.
    /// Всегда переключаем на лучший доступный тир.
    /// </summary>
    public void AutoSelectBestTier()
    {
        long cups = GetCups();

        // Находим все доступные тиры
        SpeedTier bestAvailableTier = null;

        for (int i = 0; i < tiers.Count; i++)
        {
            var tier = tiers[i];
            bool available = cups >= tier.requiredCups;

            if (available)
            {
                // Берём тир с максимальным requiredCups (лучший)
                if (bestAvailableTier == null || tier.requiredCups > bestAvailableTier.requiredCups)
                    bestAvailableTier = tier;
            }
        }

        // Всегда переключаем на лучший доступный тир
        _currentTier = bestAvailableTier;

        // Если всё ещё null (0 кубков и все тиры требуют > 0) — используем дефолт
        if (_currentTier == null)
        {
            Debug.Log("[StepSpeed] No tier available, using default speed");
        }

        UpdateAllTriggersVisual();
        ApplyCurrentStepSpeed();
    }

    /// <summary>
    /// Пройтись по всем тиру и обновить визуал (available/active) на триггерах.
    /// </summary>
    private void UpdateAllTriggersVisual()
    {
        long cups = GetCups();

        for (int i = 0; i < tiers.Count; i++)
        {
            var tier = tiers[i];
            bool available = cups >= tier.requiredCups;
            bool active = tier == _currentTier;

            if (tier.trigger != null)
                tier.trigger.SetVisualState(available, active);
        }
    }

    /// <summary>
    /// Игрок входит в триггер и вручную выбирает тир.
    /// </summary>
    public void EquipTier(StepSpeedTrigger trigger)
    {
        if (trigger == null)
            return;

        Debug.Log("Equip tier: " + trigger.name);

        long cups = GetCups();
        if (cups < trigger.RequiredCups)
        {
            trigger.ShowNeedCupsNotification();
            return;
        }

        var tier = FindTier(trigger);
        if (tier == null)
        {
            Debug.LogWarning($"Tier not found for trigger {trigger.name}");
            return;
        }

        // Запоминаем выбор игрока
        _currentTier = tier;

        // Применяем XP за шаг
        ApplyCurrentStepSpeed();

        // Обновляем визуал у ВСЕХ триггеров по новому _currentTier
        UpdateAllTriggersVisual();

        // Раз уж игрок выбрал — можно показать уведомление "Экипировано"
        trigger.ShowEquippedNotification();
    }

    private void ApplyCurrentStepSpeed()
    {
        int speed = _currentTier != null ? _currentTier.speedPerStep : defaultSpeedPerStep;
        PlayerSpeedLevelManager.instance?.SetUpgradeXpAmount(speed);

        Debug.Log($"[StepSpeed] Applied speed: {speed}, Tier: {(_currentTier != null ? _currentTier.requiredCups + " cups" : "null")}");
    }

    private SpeedTier FindTier(StepSpeedTrigger trigger)
    {
        for (int i = 0; i < tiers.Count; i++)
        {
            if (tiers[i].trigger == trigger)
                return tiers[i];
        }
        return null;
    }

    private long GetCups()
    {
        if (YG2.saves == null || YG2.saves.playerData == null)
            return 0;

        return YG2.saves.playerData.wallet.wins;
    }
}