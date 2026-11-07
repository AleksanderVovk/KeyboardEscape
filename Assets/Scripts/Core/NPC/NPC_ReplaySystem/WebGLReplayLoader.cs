using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class WebGLReplayLoader : MonoBehaviour
{
    public static WebGLReplayLoader Instance;

    [Header("Settings")]
    [SerializeField] private bool autoLoadOnStart = true;

    private Dictionary<string, PlayerReplayData> loadedRecordings = new Dictionary<string, PlayerReplayData>();
    private bool isLoading = false;
    private bool isInitialized = false;

    public event System.Action OnAllRecordingsLoaded;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (autoLoadOnStart)
        {
            LoadAllRecordings();
        }
    }

    public void LoadAllRecordings()
    {
        if (isLoading)
        {
            Debug.LogWarning("[WebGLReplayLoader] Already loading!");
            return;
        }

        isLoading = true;
        loadedRecordings.Clear();

        StartCoroutine(LoadRecordingsCoroutine());
    }

    public IEnumerator LoadRecordingsCoroutine()
    {
        // Получаем список через ReplayResourceManager
        string[] recordings = null;
        bool manifestLoaded = false;

        yield return ReplayResourceManager.LoadManifestAsync((manifest) =>
        {
            recordings = manifest;
            manifestLoaded = true;
        });

        while (!manifestLoaded)
            yield return null;

        if (recordings == null || recordings.Length == 0)
        {
            Debug.LogWarning("[WebGLReplayLoader] No recordings found!");
            isLoading = false;
            isInitialized = true;
            OnAllRecordingsLoaded?.Invoke();
            yield break;
        }

        Debug.Log($"[WebGLReplayLoader] Found {recordings.Length} recordings");

        int loadedCount = 0;
        int totalCount = recordings.Length;

        foreach (string recordingName in recordings)
        {
            // Пропускаем пустые имена
            if (string.IsNullOrEmpty(recordingName)) continue;

            yield return ReplayResourceManager.LoadRecordingAsync(recordingName, (data) =>
            {
                if (data != null && data.IsValid)
                {
                    loadedRecordings[recordingName] = data;
                    Debug.Log($"[WebGLReplayLoader] ✅ Loaded: {recordingName} ({data.frames.Count} frames)");
                }
                else
                {
                    Debug.LogWarning($"[WebGLReplayLoader] ❌ Failed to load: {recordingName}");
                }
                loadedCount++;
            });
        }

        // Ждем завершения
        while (loadedCount < totalCount)
            yield return null;

        isLoading = false;
        isInitialized = true;

        Debug.Log($"[WebGLReplayLoader] Loaded {loadedRecordings.Count}/{totalCount} recordings");
        OnAllRecordingsLoaded?.Invoke();
    }

    // ✅ ПОЛУЧЕНИЕ СПИСКА ФАЙЛОВ В WEBGL
    private string[] GetAvailableRecordings()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // В WebGL используем предварительно сохраненный список
        // или загружаем через UnityWebRequest
        return ReplayResourceManager.GetAllRecordings();
#else
        return ReplayResourceManager.GetAllRecordings();
#endif
    }

    public void LoadRecording(string recordingName, System.Action<PlayerReplayData> callback)
    {
        if (loadedRecordings.ContainsKey(recordingName))
        {
            callback?.Invoke(loadedRecordings[recordingName]);
            return;
        }

        StartCoroutine(ReplayResourceManager.LoadRecordingAsync(recordingName, (data) =>
        {
            if (data != null && data.IsValid)
            {
                loadedRecordings[recordingName] = data;
            }
            callback?.Invoke(data);
        }));
    }

    public PlayerReplayData GetRecording(string recordingName)
    {
        if (loadedRecordings.TryGetValue(recordingName, out PlayerReplayData data))
            return data;
        return null;
    }

    public Dictionary<string, PlayerReplayData> GetAllRecordings()
    {
        return loadedRecordings;
    }

    public string GetRandomRecording()
    {
        if (loadedRecordings.Count == 0) return null;
        var keys = new List<string>(loadedRecordings.Keys);
        return keys[Random.Range(0, keys.Count)];
    }

    public bool IsRecordingLoaded(string recordingName)
    {
        return loadedRecordings.ContainsKey(recordingName);
    }

    public void ClearCache()
    {
        loadedRecordings.Clear();
    }

    public float GetLoadProgress()
    {
        if (!isLoading) return 1f;
        return (float)loadedRecordings.Count / Mathf.Max(1, GetAvailableRecordings().Length);
    }

    public bool IsLoading => isLoading;
    public bool IsInitialized => isInitialized;
}