using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotReplayManager : MonoBehaviour
{
    public static BotReplayManager Instance;

    [Header("Settings")]
    [SerializeField] private bool autoAssignOnStart = true;
    [SerializeField] private float checkInterval = 0.5f;

    private readonly List<NPCReplayController> _bots = new List<NPCReplayController>();
    private readonly HashSet<string> _busyRecordings = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    private float _nextCheckTime;
    private bool _isInitialized = false;
    private bool _isAssigning = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        RefreshBots();

        if (autoAssignOnStart)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(InitializeWebGL());
#else
            AssignRandomToAllBots();
            _isInitialized = true;
#endif
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private void CheckWebGLStatus()
    {
        if (WebGLReplayLoader.Instance == null)
        {
            Debug.LogError("WebGLReplayLoader.Instance is NULL!");
            return;
        }
        
        Debug.Log($"WebGLReplayLoader initialized: {WebGLReplayLoader.Instance.IsInitialized}");
        Debug.Log($"WebGLReplayLoader loading: {WebGLReplayLoader.Instance.IsLoading}");
        Debug.Log($"Recordings count: {WebGLReplayLoader.Instance.GetAllRecordings().Count}");
        
        var recordings = WebGLReplayLoader.Instance.GetAllRecordings();
        foreach (var kvp in recordings)
        {
            Debug.Log($"- {kvp.Key}: {kvp.Value.frames.Count} frames");
        }
    }
#endif

    private IEnumerator InitializeWebGL()
    {
        Debug.Log("[BotReplayManager] Initializing WebGL...");

        // Ждем загрузчик
        yield return new WaitUntil(() => WebGLReplayLoader.Instance != null);

        // Ждем полной загрузки
        int waitCount = 0;
        while (WebGLReplayLoader.Instance.IsLoading)
        {
            waitCount++;
            if (waitCount % 10 == 0)
            {
                Debug.Log($"[BotReplayManager] Waiting for load... ({waitCount * 0.5f}s)");
            }
            yield return new WaitForSeconds(0.5f);
        }

        // Дополнительная задержка для гарантии
        yield return new WaitForSeconds(0.5f);

        // Проверяем загруженные записи
        var loadedRecordings = WebGLReplayLoader.Instance.GetAllRecordings();

        if (loadedRecordings.Count == 0)
        {
            Debug.LogWarning("[BotReplayManager] No recordings loaded!");
            _isInitialized = true;
            yield break;
        }

        Debug.Log($"[BotReplayManager] Found {loadedRecordings.Count} recordings, assigning to bots...");

        // Назначаем ботов
        _isAssigning = true;
        _busyRecordings.Clear();

        var keys = new List<string>(loadedRecordings.Keys);

        for (int i = 0; i < _bots.Count; i++)
        {
            if (_bots[i] == null) continue;

            string recording = keys[Random.Range(0, keys.Count)];
            _busyRecordings.Add(recording);

            PlayerReplayData data = WebGLReplayLoader.Instance.GetRecording(recording);
            if (data != null && data.IsValid)
            {
                _bots[i].LoadFromData(data, recording);
                Debug.Log($"[BotReplayManager] Assigned {recording} to {_bots[i].name}");
                yield return null; // Даем время на инициализацию
            }
        }

        _isAssigning = false;
        _isInitialized = true;
        Debug.Log("[BotReplayManager] WebGL initialization complete!");
    }

    private void Update()
    {
        if (!_isInitialized || _isAssigning) return;

        if (Time.time >= _nextCheckTime)
        {
            _nextCheckTime = Time.time + checkInterval;
            CheckBots();
        }
    }

    public void RefreshBots()
    {
        _bots.Clear();

#if UNITY_2023_1_OR_NEWER
        NPCReplayController[] found = Object.FindObjectsByType<NPCReplayController>(FindObjectsSortMode.None);
#else
        NPCReplayController[] found = Object.FindObjectsOfType<NPCReplayController>();
#endif

        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null)
                _bots.Add(found[i]);
        }

        Debug.Log($"[BotReplayManager] Found {_bots.Count} bots.");
    }

    public void AssignRandomToAllBots()
    {
        _busyRecordings.Clear();

        for (int i = 0; i < _bots.Count; i++)
        {
            if (_bots[i] == null) continue;
            AssignRandomRecording(_bots[i]);
        }
    }

    public void AssignRandomRecording(NPCReplayController bot)
    {
        if (bot == null) return;

        string current = bot.GetCurrentRecording();
        if (!string.IsNullOrEmpty(current))
            _busyRecordings.Remove(current);

        string chosenRecording = null;

#if UNITY_WEBGL && !UNITY_EDITOR
        // В WebGL используем WebGLReplayLoader
        if (WebGLReplayLoader.Instance == null || !WebGLReplayLoader.Instance.IsInitialized)
        {
            Debug.LogWarning("[BotReplayManager] WebGLReplayLoader not ready!");
            return;
        }

        var webGLRecordings = WebGLReplayLoader.Instance.GetAllRecordings();
        if (webGLRecordings.Count == 0)
        {
            Debug.LogWarning("[BotReplayManager] No recordings available in WebGL!");
            return;
        }

        var keys = new List<string>(webGLRecordings.Keys);
        
        // Выбираем случайную свободную запись
        List<string> available = new List<string>();
        foreach (var key in keys)
        {
            if (!_busyRecordings.Contains(key) && !key.Equals(current, System.StringComparison.OrdinalIgnoreCase))
                available.Add(key);
        }

        if (available.Count == 0)
            available = keys;

        chosenRecording = available[Random.Range(0, available.Count)];
        _busyRecordings.Add(chosenRecording);

        // Берем данные из кэша
        PlayerReplayData data = WebGLReplayLoader.Instance.GetRecording(chosenRecording);
        if (data != null && data.IsValid)
        {
            bot.LoadFromData(data, chosenRecording);
            Debug.Log($"[BotReplayManager] Assigned {chosenRecording} to {bot.name} (from cache)");
        }
        else
        {
            // Если нет в кэше - загружаем асинхронно
            WebGLReplayLoader.Instance.LoadRecording(chosenRecording, (loadedData) =>
            {
                if (loadedData != null && loadedData.IsValid)
                {
                    bot.LoadFromData(loadedData, chosenRecording);
                    Debug.Log($"[BotReplayManager] Assigned {chosenRecording} to {bot.name} (async)");
                }
            });
        }
        return;
#else
        // В редакторе используем синхронную загрузку
        string[] availableRecordings = ReplayResourceManager.GetAllRecordings();
        if (availableRecordings.Length == 0)
        {
            Debug.LogWarning("[BotReplayManager] No recordings available!");
            return;
        }

        chosenRecording = PickRandomRecording(availableRecordings, current);

        if (string.IsNullOrEmpty(chosenRecording))
        {
            Debug.LogWarning("[BotReplayManager] No recording found for bot: " + bot.name);
            return;
        }

        _busyRecordings.Add(chosenRecording);
        bot.LoadAndPlay(chosenRecording);
        Debug.Log($"[BotReplayManager] Assigned {chosenRecording} to {bot.name}");
#endif
    }

    private string PickRandomRecording(string[] recordings, string exclude)
    {
        List<string> available = new List<string>();

        for (int i = 0; i < recordings.Length; i++)
        {
            if (_busyRecordings.Contains(recordings[i])) continue;
            if (!string.IsNullOrEmpty(exclude) && recordings[i].Equals(exclude, System.StringComparison.OrdinalIgnoreCase))
                continue;
            available.Add(recordings[i]);
        }

        if (available.Count == 0)
        {
            for (int i = 0; i < recordings.Length; i++)
            {
                if (!string.IsNullOrEmpty(exclude) && recordings[i].Equals(exclude, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                available.Add(recordings[i]);
            }
        }

        if (available.Count == 0)
            return recordings[Random.Range(0, recordings.Length)];

        return available[Random.Range(0, available.Count)];
    }

    private void CheckBots()
    {
        for (int i = 0; i < _bots.Count; i++)
        {
            if (_bots[i] == null) continue;

            if (!_bots[i].IsReplaying && !_bots[i].IsLoading)
            {
                Debug.Log($"[BotReplayManager] Bot {_bots[i].name} is not replaying, assigning new.");
                AssignRandomRecording(_bots[i]);
            }
        }
    }

    public void OnBotFinished(NPCReplayController bot)
    {
        if (bot == null) return;
        Debug.Log($"[BotReplayManager] OnBotFinished: {bot.name}");
        AssignRandomRecording(bot);
    }

    public void ReleaseRecording(string recordingName)
    {
        if (string.IsNullOrEmpty(recordingName)) return;
        _busyRecordings.Remove(recordingName);
    }

    public bool IsRecordingBusy(string recordingName)
    {
        if (string.IsNullOrEmpty(recordingName)) return false;
        return _busyRecordings.Contains(recordingName);
    }
}