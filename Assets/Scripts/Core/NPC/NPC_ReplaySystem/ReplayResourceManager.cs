using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Networking;

public static class ReplayResourceManager
{
    // Путь для встроенных записей (внутри проекта)
    public static readonly string EmbeddedPath = Path.Combine(Application.streamingAssetsPath, "Recordings");

    // Путь для внешних записей (persistentDataPath - для пользовательских)
    public static readonly string ExternalPath = Path.Combine(Application.persistentDataPath, "Replays");

    // КЭШ СПИСКА ЗАПИСЕЙ ДЛЯ WEBGL
    private static string[] _cachedRecordings = null;
    private static bool _isManifestLoaded = false;

    static ReplayResourceManager()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (!Directory.Exists(EmbeddedPath))
            Directory.CreateDirectory(EmbeddedPath);

        if (!Directory.Exists(ExternalPath))
            Directory.CreateDirectory(ExternalPath);
#endif
    }

    // ========================================================================
    // ПОЛУЧЕНИЕ СПИСКА ЗАПИСЕЙ
    // ========================================================================

    public static string[] GetAllRecordings()
    {
        // Если уже загружены - возвращаем кэш
        if (_cachedRecordings != null && _cachedRecordings.Length > 0)
            return _cachedRecordings;

        var list = new List<string>();

#if UNITY_WEBGL && !UNITY_EDITOR
        // В WebGL используем манифест
        if (!_isManifestLoaded)
        {
            LoadManifestSync();
            _isManifestLoaded = true;
        }

        if (_cachedRecordings != null)
            return _cachedRecordings;
#else
        // В редакторе используем Directory
        if (Directory.Exists(EmbeddedPath))
        {
            foreach (var file in Directory.GetFiles(EmbeddedPath, "*.bin"))
                list.Add(Path.GetFileName(file));
        }

        if (Directory.Exists(ExternalPath))
        {
            foreach (var file in Directory.GetFiles(ExternalPath, "*.bin"))
                if (!list.Contains(Path.GetFileName(file)))
                    list.Add(Path.GetFileName(file));
        }
#endif

        _cachedRecordings = list.ToArray();
        return _cachedRecordings;
    }

    // ========================================================================
    // ЗАГРУЗКА МАНИФЕСТА (СИНХРОННО - ДЛЯ СТАРТА В WEBGL)
    // ========================================================================

    private static void LoadManifestSync()
    {
        try
        {
            string manifestPath = Path.Combine(EmbeddedPath, "manifest.txt");

            using (UnityWebRequest request = UnityWebRequest.Get(manifestPath))
            {
                request.SendWebRequest();

                // Ждем загрузки (максимум 5 секунд)
                float timeout = 5f;
                float startTime = Time.realtimeSinceStartup;

                while (!request.isDone && Time.realtimeSinceStartup - startTime < timeout)
                {
                    // Ждем
                }

                if (request.isDone && request.result == UnityWebRequest.Result.Success)
                {
                    string content = request.downloadHandler.text;
                    string[] lines = content.Split('\n', '\r');
                    var list = new List<string>();

                    foreach (string line in lines)
                    {
                        string file = line.Trim();
                        if (!string.IsNullOrEmpty(file) && file.EndsWith(".bin"))
                        {
                            list.Add(file);
                        }
                    }

                    _cachedRecordings = list.ToArray();
                    Debug.Log($"[ReplayResourceManager] Loaded manifest: {_cachedRecordings.Length} recordings");
                }
                else
                {
                    Debug.LogWarning($"[ReplayResourceManager] Failed to load manifest: {request.error}");
                    _cachedRecordings = new string[0];
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ReplayResourceManager] Manifest error: {e.Message}");
            _cachedRecordings = new string[0];
        }
    }

    // ========================================================================
    // ЗАГРУЗКА МАНИФЕСТА (АСИНХРОННО)
    // ========================================================================

    public static System.Collections.IEnumerator LoadManifestAsync(System.Action<string[]> callback)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    // В WebGL загружаем манифест
    string manifestPath = Path.Combine(EmbeddedPath, "manifest.txt");
    Debug.Log($"[ReplayResourceManager] Loading manifest from: {manifestPath}");

    using (UnityWebRequest request = UnityWebRequest.Get(manifestPath))
    {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string content = request.downloadHandler.text;
            string[] lines = content.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            var list = new List<string>();

            foreach (string line in lines)
            {
                string file = line.Trim();
                if (!string.IsNullOrEmpty(file) && file.EndsWith(".bin"))
                {
                    list.Add(file);
                    Debug.Log($"[ReplayResourceManager] Found in manifest: {file}");
                }
            }

            _cachedRecordings = list.ToArray();
            Debug.Log($"[ReplayResourceManager] Loaded manifest: {_cachedRecordings.Length} recordings");
            callback?.Invoke(_cachedRecordings);
        }
        else
        {
            Debug.LogError($"[ReplayResourceManager] Failed to load manifest: {request.error}");
            _cachedRecordings = new string[0];
            callback?.Invoke(_cachedRecordings);
        }
    }
#else
        // В редакторе используем файловую систему
        string[] recordings = GetAllRecordings();
        callback?.Invoke(recordings);
        yield return null;
#endif
    }

    // ========================================================================
    // ЗАГРУЗКА ЗАПИСИ (СИНХРОННО - ТОЛЬКО ДЛЯ РЕДАКТОРА)
    // ========================================================================

    public static PlayerReplayData LoadRecording(string recordingName)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        // 1. Пробуем загрузить из встроенных ресурсов
        string embeddedPath = Path.Combine(EmbeddedPath, recordingName);
        if (File.Exists(embeddedPath))
        {
            Debug.Log($"[ReplayResourceManager] Loading embedded: {recordingName}");
            return PlayerReplayIO.LoadFromPath(embeddedPath);
        }

        // 2. Пробуем загрузить из внешних (пользовательских) записей
        string externalPath = Path.Combine(ExternalPath, recordingName);
        if (File.Exists(externalPath))
        {
            Debug.Log($"[ReplayResourceManager] Loading external: {recordingName}");
            return PlayerReplayIO.LoadFromPath(externalPath);
        }
#endif

        Debug.LogWarning($"[ReplayResourceManager] Recording not found: {recordingName}");
        return null;
    }

    // ========================================================================
    // ЗАГРУЗКА ЗАПИСИ (АСИНХРОННО - ДЛЯ WEBGL)
    // ========================================================================

    public static System.Collections.IEnumerator LoadRecordingAsync(string recordingName, System.Action<PlayerReplayData> callback)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string path = Path.Combine(EmbeddedPath, recordingName);

        using (UnityWebRequest request = UnityWebRequest.Get(path))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                byte[] data = request.downloadHandler.data;
                PlayerReplayData replayData = PlayerReplayIO.LoadFromBytes(data);

                if (replayData != null && replayData.IsValid)
                {
                    Debug.Log($"[ReplayResourceManager] ✅ Loaded from WebGL: {recordingName}, frames: {replayData.frames.Count}");
                    callback?.Invoke(replayData);
                }
                else
                {
                    Debug.LogWarning($"[ReplayResourceManager] ⚠️ Invalid data: {recordingName}");
                    callback?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"[ReplayResourceManager] ❌ Failed to load {recordingName}: {request.error}");
                callback?.Invoke(null);
            }
        }
#else
        // В редакторе используем синхронную загрузку
        PlayerReplayData data = LoadRecording(recordingName);
        callback?.Invoke(data);
        yield return null;
#endif
    }

    // ========================================================================
    // ЗАГРУЗКА ВСЕХ ЗАПИСЕЙ (АСИНХРОННО)
    // ========================================================================

    public static System.Collections.IEnumerator LoadAllRecordingsAsync(System.Action<Dictionary<string, PlayerReplayData>> callback)
    {
        var result = new Dictionary<string, PlayerReplayData>();

        // Сначала загружаем манифест
        string[] recordings = null;
        bool manifestLoaded = false;

        yield return LoadManifestAsync((manifest) =>
        {
            recordings = manifest;
            manifestLoaded = true;
        });

        while (!manifestLoaded)
            yield return null;

        if (recordings == null || recordings.Length == 0)
        {
            Debug.LogWarning("[ReplayResourceManager] No recordings found!");
            callback?.Invoke(result);
            yield break;
        }

        Debug.Log($"[ReplayResourceManager] Loading {recordings.Length} recordings...");

        int loadedCount = 0;
        int totalCount = recordings.Length;

        foreach (string recordingName in recordings)
        {
            yield return LoadRecordingAsync(recordingName, (data) =>
            {
                if (data != null && data.IsValid)
                {
                    result[recordingName] = data;
                    Debug.Log($"[ReplayResourceManager] Loaded: {recordingName} ({data.frames.Count} frames)");
                }
                else
                {
                    Debug.LogWarning($"[ReplayResourceManager] Failed to load: {recordingName}");
                }
                loadedCount++;
            });
        }

        // Ждем пока все загрузятся
        while (loadedCount < totalCount)
            yield return null;

        Debug.Log($"[ReplayResourceManager] Loaded {result.Count}/{totalCount} recordings");
        callback?.Invoke(result);
    }

    // ========================================================================
    // ПРОВЕРКА СУЩЕСТВОВАНИЯ ЗАПИСИ
    // ========================================================================

    public static bool RecordingExists(string recordingName)
    {
        // Проверяем в кэше
        if (_cachedRecordings != null)
        {
            foreach (string file in _cachedRecordings)
            {
                if (file.Equals(recordingName, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        string embeddedPath = Path.Combine(EmbeddedPath, recordingName);
        if (File.Exists(embeddedPath))
            return true;

        string externalPath = Path.Combine(ExternalPath, recordingName);
        if (File.Exists(externalPath))
            return true;
#endif

        return false;
    }

    // ========================================================================
    // ОЧИСТКА КЭША
    // ========================================================================

    public static void ClearCache()
    {
        _cachedRecordings = null;
        _isManifestLoaded = false;
        Debug.Log("[ReplayResourceManager] Cache cleared");
    }

    // ========================================================================
    // ОБНОВЛЕНИЕ СПИСКА ЗАПИСЕЙ (ПРИНУДИТЕЛЬНО)
    // ========================================================================

    public static void RefreshRecordingList()
    {
        ClearCache();
        GetAllRecordings();
        Debug.Log("[ReplayResourceManager] Recording list refreshed");
    }

    // ========================================================================
    // ПОЛУЧЕНИЕ ИНФОРМАЦИИ О ЗАПИСЯХ
    // ========================================================================

    public static int GetRecordingsCount()
    {
        string[] recordings = GetAllRecordings();
        return recordings != null ? recordings.Length : 0;
    }

    public static bool HasRecordings()
    {
        return GetRecordingsCount() > 0;
    }

    // ========================================================================
    // ПОИСК ЗАПИСЕЙ ПО ШАБЛОНУ
    // ========================================================================

    public static string[] FindRecordings(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return GetAllRecordings();

        var result = new List<string>();
        string[] all = GetAllRecordings();

        foreach (string file in all)
        {
            if (file.IndexOf(pattern, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.Add(file);
            }
        }

        return result.ToArray();
    }

    // ========================================================================
    // ПОЛУЧЕНИЕ ПУТИ К ЗАПИСИ
    // ========================================================================

    public static string GetRecordingPath(string recordingName)
    {
        return Path.Combine(EmbeddedPath, recordingName);
    }

    public static string GetExternalPath(string recordingName)
    {
        return Path.Combine(ExternalPath, recordingName);
    }
}