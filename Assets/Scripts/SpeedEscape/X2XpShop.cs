    using System;
    using TMPro;
    using UnityEngine;
    using YG;

    public class X2XpShop : MonoBehaviour
    {
        public static X2XpShop Instance;

        [Header("Timer Object")]
        [Tooltip("Отдельный объект с таймером. Не должен быть объектом, на котором висит X2XpShop.")]
        [SerializeField] private GameObject timerObject;

        [Header("UI")]
        [SerializeField] private TMP_Text timerText;

        [Header("Localization")]
        [SerializeField]
        private LanguageDirectory activeText = new LanguageDirectory(
            "x2 Опыт активно",
            "x2 XP Active",
            "x2 XP Aktif"
        );

        private double _saveTimer;
        private const double SaveInterval = 15d;

        private bool _lastActiveState;

        public double RemainingSeconds
        {
            get
            {
                EnsureSaveData();

                if (YG2.saves == null || YG2.saves.playerData == null)
                    return 0d;

                return Math.Max(
                    0d,
                    YG2.saves.playerData.x2XpTimeRemaining
                );
            }
        }

        public bool IsActive => RemainingSeconds > 0d;

        private void Awake()
        {
            Instance = this;

            EnsureSaveData();

            if (timerObject == gameObject)
            {
                Debug.LogError(
                    "[X2 XP] Timer Object cannot be the same object as X2XpShop!"
                );
            }
        }

        private void Start()
        {
            EnsureSaveData();

            // При старте объект сразу включится или выключится
            // в зависимости от сохранённого времени.
            RefreshBoostState(true);
            RefreshUI();
        }

        private void Update()
        {
            if (!IsActive)
            {
                RefreshBoostState();
                RefreshUI();
                return;
            }

            // Игровое время. При Time.timeScale = 0 таймер не уменьшается.
            double deltaTime = Time.deltaTime;

            YG2.saves.playerData.x2XpTimeRemaining -= deltaTime;
            _saveTimer += deltaTime;

            if (YG2.saves.playerData.x2XpTimeRemaining <= 0d)
            {
                YG2.saves.playerData.x2XpTimeRemaining = 0d;
                _saveTimer = 0d;

                SaveTimer();
                RefreshBoostState(true);
                RefreshUI();

                return;
            }

            // Сохраняем остаток каждые 15 секунд игрового времени.
            if (_saveTimer >= SaveInterval)
            {
                _saveTimer = 0d;
                SaveTimer();
            }

            RefreshBoostState();
            RefreshUI();
        }

        public void GetX2SpeedBonus(float seconds)
        {
            EnsureSaveData();

            if (YG2.saves == null || YG2.saves.playerData == null)
                return;

            // Время стакается.
            YG2.saves.playerData.x2XpTimeRemaining += Math.Max(
                0d,
                seconds
            );

            SaveTimer();

            RefreshBoostState(true);
            RefreshUI();

            YG2.saves.playerData.donateCount++;
            YG2.SaveProgress();

            NotificationManager.ShowNotification(
                UTF.ReadLanguageDirectory(
                    new LanguageDirectory(
                        "x2 опыта активирован",
                        "x2 XP activated",
                        "x2 XP etkinlestirildi"
                    )
                ),
                NotificationType.Success
            );
        }

    private void RefreshBoostState(bool force = false)
    {
        bool active = IsActive;

        if (timerObject != null &&
            (force || _lastActiveState != active))
        {
            timerObject.SetActive(active);
        }

        if (PlayerSpeedLevelManager.instance != null)
        {
            PlayerSpeedLevelManager.instance.SetTemporaryXpMultiplier(
                active ? 2d : 1d
            );
        }

        _lastActiveState = active;
    }

    private void OnDestroy()
    {
        if (PlayerSpeedLevelManager.instance != null)
        {
            PlayerSpeedLevelManager.instance.SetTemporaryXpMultiplier(1d);
        }
    }

    private void RefreshUI()
    {
        if (timerText == null)
            return;

        bool active = IsActive;

        timerText.enabled = active;

        if (!active)
        {
            timerText.text = string.Empty;
            return;
        }

        timerText.text =
            UTF.ReadLanguageDirectory(activeText) +
            " (" +
            FormatTime(RemainingSeconds) +
            ")";
    }

    private void SaveTimer()
        {
            EnsureSaveData();

            if (YG2.saves == null || YG2.saves.playerData == null)
                return;

            YG2.saves.playerData.x2XpTimeRemaining = Math.Max(
                0d,
                YG2.saves.playerData.x2XpTimeRemaining
            );

            YG2.SaveProgress();

            Debug.Log(
                $"[X2 XP] Timer saved: " +
                $"{YG2.saves.playerData.x2XpTimeRemaining:F1} sec"
            );
        }

        private string FormatTime(double seconds)
        {
            seconds = Math.Max(0d, seconds);

            TimeSpan time = TimeSpan.FromSeconds(seconds);

            if (time.TotalHours >= 1d)
            {
                return $"{(int)time.TotalHours:00}:" +
                       $"{time.Minutes:00}:" +
                       $"{time.Seconds:00}";
            }

            return $"{time.Minutes:00}:{time.Seconds:00}";
        }

        private void EnsureSaveData()
        {
            if (YG2.saves == null)
                return;

            if (YG2.saves.playerData == null)
                YG2.saves.playerData = new PlayerData();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveTimer();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                SaveTimer();
        }

        private void OnApplicationQuit()
        {
            SaveTimer();
        }
    }