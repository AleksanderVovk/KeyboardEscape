using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

public class RebirthManager : MonoBehaviour
{
    public static RebirthManager instance;

    [Header("Config")]
    [SerializeField] private List<RebirthLevelSO> _levels = new();

    [Header("References")]
    [SerializeField] private PlayerSpeed _playerSpeed;
    [SerializeField] private PlayerSpeedLevelManager _speedLevelManager;

    [Header("UI")]
    [SerializeField] private Slider _speedSlider;
    [SerializeField] private TMP_Text _speedText;
    [SerializeField] private Button _rebirthButton;
    [SerializeField] private TMP_Text _requiredLevelText;

    [Header("UI - Multipliers")]
    [SerializeField] private TMP_Text _currentMultiplierText;
    [SerializeField] private TMP_Text _nextMultiplierText;
    [SerializeField] private TMP_Text _rebirthMultiplierText; // ← НОВЫЙ текст: "Множитель x0 (Перерождение)"

    [SerializeField] private ShopUpgradesController _shopUpgrades;

    [Header("Localization")]
    public LanguageDirectory requiredLevelTextDir = new LanguageDirectory(
        "Уровень {0}",
        "Level {0}",
        "Seviye {0}"
    );

    public LanguageDirectory rebirthMultiplierTextDir = new LanguageDirectory(
        "Множитель x{0} (Перерождение)",
        "Multiplier x{0} (Rebirth)",
        "Çarpan x{0} (Yeniden Doğuş)"
    );

    private void Awake()
    {
        instance = this;

        if (_rebirthButton)
        {
            _rebirthButton.onClick.RemoveListener(TryRebirth);
            _rebirthButton.onClick.AddListener(TryRebirth);
        }
    }

    private void Start()
    {
        RefreshUI();
    }

    public int CurrentRebirthLevel => YG2.saves.playerData.rebirthLevel;

    public double CurrentMultiplier
    {
        get
        {
            if (YG2.saves.playerData.rebirthMultiplier <= 0f)
                YG2.saves.playerData.rebirthMultiplier = 1f;

            return YG2.saves.playerData.rebirthMultiplier;
        }
    }

    public RebirthLevelSO GetNextLevel()
    {
        int next = CurrentRebirthLevel + 1;
        return _levels.Find(l => l != null && l.level == next);
    }

    public void RefreshUI()
    {
        var next = GetNextLevel();

        // Всегда показываем текущий множитель
        if (_currentMultiplierText)
            _currentMultiplierText.text = CurrentMultiplier.ToString("F1") + "x " + UTF.ReadLanguageDirectory(LangList.instance.speed);

        // ← НОВЫЙ текст: "Множитель x0 (Перерождение)" / "Multiplier x0 (Rebirth)" / "Çarpan x0 (Yeniden Doğuş)"
        if (_rebirthMultiplierText)
        {
            if (CurrentMultiplier <= 1)
            {
                _rebirthMultiplierText.text = string.Format(UTF.ReadLanguageDirectory(rebirthMultiplierTextDir), 0);
            }
            else
            {
                _rebirthMultiplierText.text = string.Format(UTF.ReadLanguageDirectory(rebirthMultiplierTextDir), CurrentMultiplier.ToString("F1"));
            }
        }

        if (next == null)
        {
            // КОНЕЦ ПЕРЕРОЖДЕНИЙ
            if (_rebirthButton) _rebirthButton.interactable = false;
            if (_requiredLevelText) _requiredLevelText.text = "";
            return;
        }

        int currentLevel = _speedLevelManager != null ? _speedLevelManager.CurrentLevel : 1;
        int reqLevel = next.requiredSpeedLevel;

        // Speed UI
        if (_speedSlider)
        {
            _speedSlider.minValue = 0f;
            _speedSlider.maxValue = Mathf.Max(1, reqLevel);
            _speedSlider.value = Mathf.Min(currentLevel, reqLevel);
        }

        if (_speedText)
            _speedText.text = UTF.ReadLanguageDirectory(LangList.instance.speed) + " " + $"{currentLevel} / {reqLevel}";

        // Новый текст: "Уровень 125" / "Level 125" / "Seviye 125"
        if (_requiredLevelText)
            _requiredLevelText.text = string.Format(UTF.ReadLanguageDirectory(requiredLevelTextDir), reqLevel);

        // Next multiplier UI
        if (_nextMultiplierText)
            _nextMultiplierText.text = next.rewardMultiplier.ToString("F1") + "x " + UTF.ReadLanguageDirectory(LangList.instance.speed);

        // Can rebirth?
        bool can = currentLevel >= reqLevel;
        if (_rebirthButton) _rebirthButton.interactable = can;
    }

    public void TryRebirth()
    {
        var next = GetNextLevel();
        if (next == null) return;

        int currentLevel = _speedLevelManager != null ? _speedLevelManager.CurrentLevel : 1;
        if (currentLevel < next.requiredSpeedLevel)
        {
            RefreshUI();
            return;
        }

        UISound.manager.PlayBuy(true);

        // 2) Сбросить уровни PlayerSpeedLevelManager
        if (_speedLevelManager != null)
        {
            _speedLevelManager.ResetLevels();
        }

        // 3) Сбросить апгрейды и цены магазина
        if (_shopUpgrades != null)
        {
            _shopUpgrades.ResetShopToDefaults();
        }

        // 4) Reward: множитель
        YG2.saves.playerData.rebirthMultiplier = Math.Max(0.01, next.rewardMultiplier);

        // 5) Level up rebirth
        YG2.saves.playerData.rebirthLevel = next.level;

        // ← НОВОЕ: обновить множитель в PlayerSpeedLevelManager
        if (_speedLevelManager != null)
        {
            _speedLevelManager.UpdateRebirthMultiplier();
        }

        // Save
        YG2.SaveProgress();

        RefreshUI();
    }
}