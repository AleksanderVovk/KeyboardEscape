using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

public struct Bounds2D
{
    public float minX, maxX, minY, maxY;
}

public static class XPTextSettings
{
    public static Bounds2D TargetBounds;
    public static float PopupCenterX;
    public static float PopupCenterY;
    public static float FlyDuration = 0.4f;
    public static float DownDuration = 0.15f;
    public static float DownDistance = 100f;
    public static float ScaleFrom = 0f;
    public static float ScaleTo = 1f;
    public static int PoolSize = 12;
}

public class PlayerSpeedLevelManager : MonoBehaviour
{
    public static PlayerSpeedLevelManager instance;

    [Header("Links")]
    public PlayerSpeed playerSpeed;
    public EquipmentShopUI equipmentShopUI;

    [Header("XP Gain While Moving")]
    [SerializeField] private float xpAddInterval = 1f;
    [SerializeField] private long xpPerInterval = 5;

    [Header("UI")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private TMP_Text totalExpText;
    [SerializeField] private Slider expSlider;

    [Header("Slider Smooth")]
    [SerializeField] private float sliderSmoothSpeed = 8f;

    private bool _isMoving;
    private int _currentLevel = 1;
    private long _currentXP = 0;
    private long _requiredXP;
    private Coroutine _xpRoutine;
    private Coroutine _sliderRoutine;

    private long _xpSinceLastSave = 0;
    private const int SaveEveryXpGainCount = 10;

    public long upgradeXpAmount = 1;

    // Бонус от экипировки (След + Аура) — в процентах
    public double equipmentXpBonusPercent = 0f;

    // Бонус от предметов инвентаря — в процентах
    public double inventoryXpBonusPercent = 0f;

    // ← НОВОЕ: Множитель от перерождения (rebirth)
    public double rebirthXpMultiplier = 1f;

    public double temporaryXpMultiplier = 1d;

    public LanguageDirectory levelUpLang = new LanguageDirectory(
        "Повышение уровня",
        "Level Up",
        "Seviye Atlandı"
    );

    public LanguageDirectory levelLang = new LanguageDirectory(
        "Уровень",
        "Level",
        "Seviye"
    );

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        LoadFromSaveAndInit();
        RefreshUI(true);

        if (playerSpeed != null)
            playerSpeed.ApplySpeedFromAttributes();

        UpdateXpMultiplierFromEquipment();
        UpdateRebirthMultiplier();

        if (X2XpShop.Instance != null)
        {
            SetTemporaryXpMultiplier(
                X2XpShop.Instance.IsActive ? 2d : 1d
            );
        }
    }

    public void UpdateXpMultiplierFromEquipment()
    {
        if (equipmentShopUI == null)
        {
            Debug.LogWarning("[XP] EquipmentShopUI not assigned!");
            return;
        }

        double totalMultiplier = equipmentShopUI.GetTotalXpMultiplier();
        double bonusPercent = (totalMultiplier - 1f) * 100f;

        SetEquipmentXpBonus(bonusPercent);

        Debug.Log($"[XP] Equipment multiplier: x{totalMultiplier:F2} → Bonus: {bonusPercent:F0}%");
    }

    public void UpdateRebirthMultiplier()
    {
        if (RebirthManager.instance != null)
        {
            rebirthXpMultiplier = RebirthManager.instance.CurrentMultiplier;
            Debug.Log($"[XP] Rebirth XP multiplier set to: x{rebirthXpMultiplier:F2}");
        }
    }

    public void SetEquipmentXpBonus(double bonusPercent)
    {
        equipmentXpBonusPercent = bonusPercent;
        Debug.Log($"[XP] Equipment XP bonus set to: {equipmentXpBonusPercent}%");
    }

    public void SetInventoryXpBonus(double bonusPercent)
    {
        inventoryXpBonusPercent = bonusPercent;
        Debug.Log($"[XP] Inventory XP bonus set to: {inventoryXpBonusPercent}%");
    }

    public void SetUpgradeXpAmount(int amount)
    {
        upgradeXpAmount = amount;
        Debug.Log("[XP] Set upgrade xp amount: " + amount);
    }

    public void SetTemporaryXpMultiplier(double multiplier)
    {
        temporaryXpMultiplier = Math.Max(1d, multiplier);

        Debug.Log(
            $"[XP] Temporary multiplier set to: x{temporaryXpMultiplier:F2}"
        );
    }

    private void LoadFromSaveAndInit()
    {
        long totalXp = 0;

        if (YG2.saves != null && YG2.saves.playerData != null)
            totalXp = YG2.saves.playerData.totalXp;

        if (totalXp == 0 && YG2.saves != null && YG2.saves.playerData != null)
        {
            int savedSpeedLvl = YG2.saves.playerData.speedLvl;
            if (savedSpeedLvl > 1)
            {
                long calcTotal = 0;
                for (int l = 1; l < savedSpeedLvl; l++)
                    calcTotal += CalculateRequiredXP(l);

                totalXp = calcTotal;
                YG2.saves.playerData.totalXp = totalXp;
                YG2.SaveProgress();
            }
        }

        _currentLevel = 1;
        RecalculateRequiredXP();

        while (totalXp >= _requiredXP)
        {
            totalXp -= _requiredXP;
            _currentLevel++;
            RecalculateRequiredXP();
        }

        _currentXP = totalXp;

        if (playerSpeed != null)
            playerSpeed.SetSpeedFromLevel();

        // Синк уровня в сейв
        if (YG2.saves != null && YG2.saves.playerData != null)
            YG2.saves.playerData.speedLvl = _currentLevel;
    }

    public void SetPlayerIsMoving(bool isMoving)
    {
        if (_onTreadmill)
        {
            if (_xpRoutine != null)
            {
                StopCoroutine(_xpRoutine);
                _xpRoutine = null;
            }
            return;
        }

        if (_isMoving == isMoving)
            return;

        _isMoving = isMoving;

        if (_xpRoutine != null)
        {
            StopCoroutine(_xpRoutine);
            _xpRoutine = null;
        }

        if (_isMoving)
            _xpRoutine = StartCoroutine(GainXPWhileMoving());
    }

    private bool _onTreadmill = false;
    private Coroutine _treadmillXpRoutine;

    public void EnterTreadmill()
    {
        _onTreadmill = true;

        if (_xpRoutine != null)
        {
            StopCoroutine(_xpRoutine);
            _xpRoutine = null;
        }

        if (_treadmillXpRoutine != null)
            StopCoroutine(_treadmillXpRoutine);

        _treadmillXpRoutine = StartCoroutine(GainXPOnTreadmill());

        Debug.Log("[XP] Entered treadmill - stopped normal XP gain");
    }

    public void ExitTreadmill()
    {
        _onTreadmill = false;

        if (_treadmillXpRoutine != null)
        {
            StopCoroutine(_treadmillXpRoutine);
            _treadmillXpRoutine = null;
        }

        Debug.Log("[XP] Exited treadmill");
    }

    private IEnumerator GainXPOnTreadmill()
    {
        AddXP(upgradeXpAmount);

        while (_onTreadmill)
        {
            yield return new WaitForSeconds(xpAddInterval);

            if (!_onTreadmill)
                yield break;

            AddXP(upgradeXpAmount);
        }
    }

    private IEnumerator GainXPWhileMoving()
    {
        AddXP(upgradeXpAmount);

        while (_isMoving)
        {
            yield return new WaitForSeconds(xpAddInterval);

            if (!_isMoving)
                yield break;

            AddXP(upgradeXpAmount);
        }
    }

    public double treadmillXpMultiplier = 1.0;

    public void SetTreadmillXpMultiplier(double multiplier)
    {
        treadmillXpMultiplier = multiplier;
        Debug.Log($"[XP] Treadmill XP multiplier set to: x{treadmillXpMultiplier:F2}");
    }

    private void AddXP(long amount)
    {
        long finalAmount = CalculateXpWithBonus(amount);

        if (YG2.saves != null && YG2.saves.playerData != null)
        {
            YG2.saves.playerData.totalXp += finalAmount;
            _xpSinceLastSave++;

            if (_xpSinceLastSave >= SaveEveryXpGainCount)
            {
                _xpSinceLastSave = 0;
                YG2.SaveProgress();
            }
        }

        _currentXP += finalAmount;

        if (XPPopupPool.Instance != null)
            XPPopupPool.Instance.Spawn(finalAmount);

        while (_currentXP >= _requiredXP)
        {
            _currentXP -= _requiredXP;
            LevelUp();
        }

        RefreshUI(false);
    }

    private long CalculateXpWithBonus(long baseAmount)
    {
        double totalMultiplier = 1d;

        double equipmentMultiplier =
            equipmentShopUI != null
                ? equipmentShopUI.GetTotalXpMultiplier()
                : 1d;

        totalMultiplier *= equipmentMultiplier;

        if (inventoryXpBonusPercent > 0d)
        {
            totalMultiplier *= 1d + inventoryXpBonusPercent / 100d;
        }

        if (rebirthXpMultiplier > 1d)
        {
            totalMultiplier *= rebirthXpMultiplier;
        }

        if (treadmillXpMultiplier > 1d)
        {
            totalMultiplier *= treadmillXpMultiplier;
        }

        if (temporaryXpMultiplier > 1d)
        {
            totalMultiplier *= temporaryXpMultiplier;
        }

        double multipliedAmount = baseAmount * totalMultiplier;
        long finalAmount = (long)Math.Ceiling(multipliedAmount);

        Debug.Log(
            $"[XP] Base: {baseAmount}, " +
            $"Equipment: x{equipmentMultiplier:F2}, " +
            $"Inventory: +{inventoryXpBonusPercent:F0}%, " +
            $"Rebirth: x{rebirthXpMultiplier:F2}, " +
            $"Treadmill: x{treadmillXpMultiplier:F2}, " +
            $"Temporary: x{temporaryXpMultiplier:F2}, " +
            $"Total: x{totalMultiplier:F2}, " +
            $"Final: {finalAmount}"
        );

        return finalAmount;
    }

    private void LevelUp()
    {
        _currentLevel++;

        if (playerSpeed != null)
        {
            playerSpeed.AddSpeed(0.1f, true);
            playerSpeed.SetSpeedFromLevel();
            playerSpeed.ApplySpeedFromAttributes();
        }

        RecalculateRequiredXP();

        if (StepSpeedUpgradeManager.Instance != null)
            StepSpeedUpgradeManager.Instance.AutoSelectBestTier();

        PopupMessageUI.Instance.ShowMessage(
            UTF.ReadLanguageDirectory(levelUpLang),
            UTF.ReadLanguageDirectory(levelLang) + " " + _currentLevel
        );

        UISound.manager.PlayLevelUp(false);

        if (RebirthManager.instance != null)
            RebirthManager.instance.RefreshUI();

        // Синк уровня в сейв
        if (YG2.saves != null && YG2.saves.playerData != null)
            YG2.saves.playerData.speedLvl = _currentLevel;

        YG2.SetLeaderboard("TopLvl", YG2.saves.playerData.speedLvl);
    }

    private void RecalculateRequiredXP()
    {
        _requiredXP = CalculateRequiredXP(_currentLevel);
        if (_requiredXP <= 0)
            _requiredXP = 1;
    }

    private long CalculateRequiredXP(int level)
    {
        long[] xpTable = new long[]
        {
            100, 110, 125, 145, 175,
            201, 230, 265, 305, 352,
            410, 480, 560, 650, 708,
            860, 1020, 1200, 1350, 1420,
            1600, 1820, 2060, 2320, 2600,
            2900, 3220, 3570, 3950, 5760,
            6620
        };

        if (level <= 0) return 1;
        if (level <= xpTable.Length) return xpTable[level - 1];

        float growth = 1.15f;
        long last = xpTable[xpTable.Length - 1];
        for (int i = xpTable.Length + 1; i <= level; i++)
            last = (long)Math.Floor(last * growth);

        return last;
    }

    private void RefreshUI(bool instant)
    {
        if (levelText != null)
            levelText.text = UTF.ReadLanguageDirectory(LangList.instance.level) + " " + _currentLevel;

        if (expText != null)
            expText.text = UTF.AbbreviateNumber(_currentXP) + " / " + UTF.AbbreviateNumber(_requiredXP);

        if (totalExpText != null)
            totalExpText.text = UTF.AbbreviateNumber(YG2.saves.playerData.totalXp) + " " + UTF.ReadLanguageDirectory(LangList.instance.speed);

        if (expSlider != null)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = _requiredXP;

            if (_sliderRoutine != null)
                StopCoroutine(_sliderRoutine);

            if (instant)
                expSlider.value = _currentXP;
            else
                _sliderRoutine = StartCoroutine(SmoothSliderTo(_currentXP));
        }
    }

    public void ResetLevels()
    {
        _currentLevel = 1;
        _currentXP = 0;

        if (YG2.saves != null && YG2.saves.playerData != null)
        {
            YG2.saves.playerData.totalXp = 0;
        }

        RecalculateRequiredXP();
        RefreshUI(true);

        if (playerSpeed != null)
        {
            if (YG2.saves.playerData.attributes == null)
                YG2.saves.playerData.attributes = new AdditionalAttributes();

            YG2.saves.playerData.attributes.speed = 0f;
            YG2.saves.playerData.totalXp = 0;

            playerSpeed.additionalAttributes = YG2.saves.playerData.attributes;
            playerSpeed.SetMaxSpeedAndUnlock();
            playerSpeed.ApplySpeedFromAttributes();
        }

        if (YG2.saves != null && YG2.saves.playerData != null)
            YG2.saves.playerData.speedLvl = _currentLevel;

        Debug.Log("[XP] Levels and speed reset for rebirth!");

        YG2.SaveProgress();
    }

    private IEnumerator SmoothSliderTo(float target)
    {
        while (Mathf.Abs(expSlider.value - target) > 0.01f)
        {
            expSlider.value = Mathf.Lerp(expSlider.value, target, Time.deltaTime * sliderSmoothSpeed);
            yield return null;
        }

        expSlider.value = target;
    }

    public void GiveAdminXP(long xp)
    {
        if (xp <= 0)
        {
            Debug.LogWarning("[XP Admin] XP amount must be positive.");
            return;
        }

        if (YG2.saves == null || YG2.saves.playerData == null)
        {
            Debug.LogError("[XP Admin] Saves not initialized.");
            return;
        }

        YG2.saves.playerData.totalXp += xp;

        RecalculateLevelFromTotalXP();

        // После пересчёта — явно обновляем скорость
        if (playerSpeed != null)
        {
            playerSpeed.SetSpeedFromLevel();
            playerSpeed.ApplySpeedFromAttributes();
        }

        RefreshUI(true);

        YG2.SaveProgress();

        Debug.Log(
            $"[XP Admin] Gave {xp} XP (with bonuses: {xp}). " +
            $"Total: {YG2.saves.playerData.totalXp}, Level: {_currentLevel}"
        );
    }

    private void RecalculateLevelFromTotalXP()
    {
        if (YG2.saves == null || YG2.saves.playerData == null)
            return;

        long totalXp = YG2.saves.playerData.totalXp;

        _currentLevel = 1;
        _currentXP = 0;
        _requiredXP = CalculateRequiredXP(_currentLevel);

        // Проходим по каждому уровню и вызываем LevelUp()
        while (totalXp >= _requiredXP)
        {
            totalXp -= _requiredXP;
            _currentLevel++;
            _requiredXP = CalculateRequiredXP(_currentLevel);

            // Применяем логику уровня (скорость, звуки, UI и т.д.)
            if (playerSpeed != null)
            {
                playerSpeed.AddSpeed(0.1f, true);
                playerSpeed.SetSpeedFromLevel();
                playerSpeed.ApplySpeedFromAttributes();
            }

            if (StepSpeedUpgradeManager.Instance != null)
                StepSpeedUpgradeManager.Instance.AutoSelectBestTier();

            if (RebirthManager.instance != null)
                RebirthManager.instance.RefreshUI();
        }

        _currentXP = totalXp;

        // Финальный синк уровня в сейв
        if (YG2.saves != null && YG2.saves.playerData != null)
            YG2.saves.playerData.speedLvl = _currentLevel;
    }

    public int CurrentLevel => _currentLevel;
    public long CurrentXP => _currentXP;
    public long RequiredXP => _requiredXP;
}