using TMPro;
using UnityEngine;
using YG;

public class StepSpeedTrigger : MonoBehaviour
{
    [Header("Render")]
    [SerializeField] private DynamicTilingProperty _dynamicTiling;

    [Header("Upgrade")]
    [SerializeField] private int requiredCups = 0;
    [SerializeField] private float speedPerStep = 1f;

    [Header("Optional Color Tint")]
    [SerializeField] private Color defaultTint = Color.white;
    [SerializeField] private Color availableTint = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color activeTint = Color.yellow;

    private bool _available;
    private bool _active;

    [SerializeField] private TMP_Text _displaySpeedAmount;
    [SerializeField] private TMP_Text _winPrice;

    public int RequiredCups => requiredCups;
    public float SpeedPerStep => speedPerStep;

    private void Start()
    {
        RefreshState();
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        _displaySpeedAmount.text = UTF.manager.TextToSprite("\U0001F603") + " +" + speedPerStep + "/" + UTF.ReadLanguageDirectory(LangList.instance.step);
        _winPrice.text = UTF.ReadLanguageDirectory(LangList.instance.required) + " " + UTF.AbbreviateNumber(requiredCups) + " " + UTF.ReadLanguageDirectory(LangList.instance.wins);
    }

    public void RefreshState()
    {
        long cups = GetCups();
        _available = cups >= requiredCups;

        bool isActive = false;

        if (StepSpeedUpgradeManager.Instance != null)
            isActive = StepSpeedUpgradeManager.Instance.IsTriggerActive(this);

        _active = _available && isActive;

        SetVisualState(_available, _active);
    }

    public void SetVisualState(bool available, bool active)
    {
        _available = available;
        _active = active;

        if (_dynamicTiling == null)
        {
            Debug.LogWarning($"StepSpeedTrigger '{name}' has no DynamicTilingProperty assigned.");
            return;
        }

        if (_active)
        {
            ApplyTint(activeTint);
            return;
        }

        if (_available)
        {
            ApplyTint(availableTint);
            return;
        }

        ApplyTint(defaultTint);
    }

    private void ApplyTint(Color color)
    {
        if (_dynamicTiling == null)
            return;

        _dynamicTiling.SetColor(color);
    }

    private long GetCups()
    {
        if (YG2.saves == null || YG2.saves.playerData == null)
            return 0;

        return YG2.saves.playerData.wallet.wins;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponent<Player>();

        if (player.thisRealPlayer)
        {
            Debug.Log("Player in trigger: " + gameObject.name);

            if (StepSpeedUpgradeManager.Instance != null)
            {
                StepSpeedUpgradeManager.Instance.EquipTier(this);
                return;
            }

            long cups = GetCups();
            if (cups < requiredCups)
            {
                ShowNeedCupsNotification();
                return;
            }

            if (_active)
            {
                ShowEquippedNotification();
                return;
            }

            ShowEquippedNotification();
        }
    }

    public void ShowNeedCupsNotification()
    {
        NotificationManager.ShowNotification(GetNeedCupsText(requiredCups), NotificationType.Error);
    }

    public void ShowEquippedNotification()
    {
        NotificationManager.ShowNotification(GetEquippedText(), NotificationType.Success);
    }

    private string GetNeedCupsText(int need)
    {
        switch (YG2.envir.language)
        {
            case "ru":
                return $"Нужно {need} кубков";
            case "en":
                return $"Need {need} cups";
            case "tr":
                return $"{need} kupa gerekli";
        }

        return $"Need {need} cups";
    }

    private string GetEquippedText()
    {
        switch (YG2.envir.language)
        {
            case "ru":
                return "Экипировано";
            case "en":
                return "Equipped";
            case "tr":
                return "Takildi";
        }

        return "Equipped";
    }
}