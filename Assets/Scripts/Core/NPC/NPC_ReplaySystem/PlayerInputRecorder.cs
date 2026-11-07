using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerInputRecorder : MonoBehaviour
{
    [SerializeField] private InputsManager inputsManager;
    [SerializeField] private PlayerState playerState;
    [SerializeField] private PlayerController playerController;

    [Header("Recording")]
    [SerializeField] private string recordingName = "recording.bin";
    [SerializeField] private bool recordOnlyWhenChanged = true;

    [Header("Compression")]
    [SerializeField] private float positionEpsilon = 0.002f;
    [SerializeField] private float rotationEpsilon = 0.5f;
    [SerializeField] private float velocityEpsilon = 0.03f;
    [SerializeField] private float minSaveInterval = 0.02f;

    [Header("Auto Export")]
    [SerializeField] private bool autoExportToStreamingAssets = true;
    [SerializeField] private string streamingAssetsFolder = "Recordings";

    private readonly List<ReplayFrame> _frames = new List<ReplayFrame>(8192);
    private bool _recording;
    private float _startTime;
    private float _lastSaveTime;
    private ReplayFrame _lastFrame;
    private bool _hasLast;
    private PlayerReplayData _cachedData;

    // Публичные свойства
    public bool IsRecording => _recording;
    public float RecordingTime => _recording ? Time.time - _startTime : 0f;
    public int FrameCount => _frames.Count;
    public string RecordingName => recordingName;

    private void Awake()
    {
        // Автоматически находим компоненты если не назначены
        if (inputsManager == null) inputsManager = GetComponent<InputsManager>();
        if (playerState == null) playerState = GetComponent<PlayerState>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
    }

    public void SetRecordingName(string newName)
    {
        if (!string.IsNullOrWhiteSpace(newName))
        {
            // Убеждаемся что имя имеет расширение .bin
            if (!newName.EndsWith(".bin"))
                newName += ".bin";
            recordingName = newName;
        }
    }

    public void StartNewRecording(string newName)
    {
        SetRecordingName(newName);
        StartRecording();
    }

    public void StartRecording()
    {
        if (inputsManager == null)
        {
            Debug.LogError("[PlayerInputRecorder] InputsManager is null!");
            return;
        }
        if (playerController == null)
        {
            Debug.LogError("[PlayerInputRecorder] PlayerController is null!");
            return;
        }

        _frames.Clear();
        _cachedData = null;
        _startTime = Time.time;
        _recording = true;
        _hasLast = false;
        _lastSaveTime = -999f;

        Debug.Log($"[PlayerInputRecorder] Started recording: {recordingName}");
    }

    public void StopRecording()
    {
        if (!_recording) return;

        _recording = false;
        SaveToFile();

        // Автоматический экспорт в StreamingAssets
        if (autoExportToStreamingAssets)
        {
            ExportToStreamingAssets();
        }
    }

    public PlayerReplayData GetRecordedData()
    {
        if (_cachedData != null && _cachedData.IsValid)
            return _cachedData;

        if (_frames.Count == 0)
        {
            Debug.LogWarning("[PlayerInputRecorder] No frames recorded!");
            return null;
        }

        _cachedData = new PlayerReplayData();
        _cachedData.frames.AddRange(_frames);
        return _cachedData;
    }

    private void FixedUpdate()
    {
        if (!_recording || inputsManager == null || playerController == null) return;
        if (Time.time - _lastSaveTime < minSaveInterval) return;

        Transform root = playerController.transform;
        Transform body = playerController.GetModelRoot();

        Vector3 rawPos = root.position;
        Quaternion rawRoot = root.rotation;
        Quaternion rawBodyL = body != null ? body.localRotation : Quaternion.identity;

        Vector3 rawVel = playerController.GetVelocity();

        ReplayFrame frame = new ReplayFrame
        {
            time = Time.time - _startTime,
            position = QuantizePosition(rawPos),
            rootRotation = QuantizeRotation(rawRoot),
            bodyLocalRotation = QuantizeRotation(rawBodyL),
            velocity = QuantizeVelocity(rawVel),
            moveX = FloatToShort(inputsManager.inputDirection.x),
            moveY = FloatToShort(inputsManager.inputDirection.y),
            lookX = FloatToShort(GetLookInput().x),
            lookY = FloatToShort(GetLookInput().y),
            flags = GetFlags()
        };

        if (recordOnlyWhenChanged && _hasLast)
        {
            bool posChanged = (rawPos - _lastFrame.position).sqrMagnitude > positionEpsilon * positionEpsilon;
            bool rootChanged = Quaternion.Angle(rawRoot, _lastFrame.rootRotation) > rotationEpsilon;
            bool bodyChanged = Quaternion.Angle(rawBodyL, _lastFrame.bodyLocalRotation) > rotationEpsilon;
            bool velChanged = (rawVel - _lastFrame.velocity).sqrMagnitude > velocityEpsilon * velocityEpsilon;
            bool inputChanged =
                frame.moveX != _lastFrame.moveX ||
                frame.moveY != _lastFrame.moveY ||
                frame.lookX != _lastFrame.lookX ||
                frame.lookY != _lastFrame.lookY ||
                frame.flags != _lastFrame.flags;

            if (!posChanged && !rootChanged && !bodyChanged && !velChanged && !inputChanged)
                return;
        }

        _frames.Add(frame);
        _lastFrame = frame;
        _hasLast = true;
        _lastSaveTime = Time.time;
    }

    private byte GetFlags()
    {
        byte flags = 0;
        if (inputsManager.run) flags |= ReplayFlags.Run;
        if (inputsManager.jump) flags |= ReplayFlags.Jump;
        if (playerState != null && playerState.onLadder) flags |= ReplayFlags.OnLadder;
        if (playerState != null && playerState.isPushed) flags |= ReplayFlags.IsPushed;
        if (playerState != null && playerState.isGrounded) flags |= ReplayFlags.Grounded;
        if (playerState != null && playerState.isWalking) flags |= ReplayFlags.Walking;
        return flags;
    }

    private Vector2 GetLookInput()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return new Vector2(SimpleInput.GetAxis("LookX"), SimpleInput.GetAxis("LookY"));
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }

    private short FloatToShort(float v)
    {
        v = Mathf.Clamp(v, -1f, 1f);
        return (short)Mathf.RoundToInt(v * 32767f);
    }

    private Vector3 QuantizePosition(Vector3 p)
    {
        return new Vector3(
            Mathf.Round(p.x * 100f) * 0.01f,
            Mathf.Round(p.y * 100f) * 0.01f,
            Mathf.Round(p.z * 100f) * 0.01f
        );
    }

    private Quaternion QuantizeRotation(Quaternion q)
    {
        Vector3 e = q.eulerAngles;
        e.x = Mathf.Round(e.x * 10f) * 0.1f;
        e.y = Mathf.Round(e.y * 10f) * 0.1f;
        e.z = Mathf.Round(e.z * 10f) * 0.1f;
        return Quaternion.Euler(e);
    }

    private Vector3 QuantizeVelocity(Vector3 v)
    {
        return new Vector3(
            Mathf.Round(v.x * 100f) * 0.01f,
            Mathf.Round(v.y * 100f) * 0.01f,
            Mathf.Round(v.z * 100f) * 0.01f
        );
    }

    public long GetSavedFileSize()
    {
        string path = PlayerReplayIO.GetPath(recordingName);
        if (!File.Exists(path)) return 0;
        return new FileInfo(path).Length;
    }

    public long GetCurrentEstimatedSize()
    {
        int bytesPerFrame = 4      // time
                           + 12    // position
                           + 16    // rootRotation
                           + 16    // bodyLocalRotation
                           + 12    // velocity
                           + 2 + 2 + 2 + 2 // move/look
                           + 1;    // flags
        return 8L + (long)_frames.Count * bytesPerFrame;
    }

    private void SaveToFile()
    {
        if (_frames.Count == 0)
        {
            Debug.LogWarning("[PlayerInputRecorder] No frames to save!");
            return;
        }

        try
        {
            // Создаем папку если её нет
#if !UNITY_WEBGL || UNITY_EDITOR
            if (!Directory.Exists(ReplayLibrary.RecordsFolder))
                Directory.CreateDirectory(ReplayLibrary.RecordsFolder);
#endif

            string path = PlayerReplayIO.GetPath(recordingName);

            // Используем универсальный метод сохранения
            PlayerReplayData data = GetRecordedData();
            if (data == null || !data.IsValid)
            {
                Debug.LogError("[PlayerInputRecorder] Failed to create replay data!");
                return;
            }

            PlayerReplayIO.SaveToPath(path, data);

            Debug.Log($"[PlayerInputRecorder] ✅ Saved {_frames.Count} frames to {path}");
            Debug.Log($"   - Duration: {data.frames[data.frames.Count - 1].time:F2} seconds");
            Debug.Log($"   - File size: {FormatBytes(new FileInfo(path).Length)}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerInputRecorder] ❌ Failed to save recording: {e.Message}");
        }
    }

    private void ExportToStreamingAssets()
    {
#if UNITY_EDITOR
        try
        {
            string streamingPath = Path.Combine(Application.dataPath, "StreamingAssets", streamingAssetsFolder);

            // Создаем папку если её нет
            if (!Directory.Exists(streamingPath))
                Directory.CreateDirectory(streamingPath);

            string sourcePath = PlayerReplayIO.GetPath(recordingName);
            string destPath = Path.Combine(streamingPath, recordingName);

            if (!File.Exists(sourcePath))
            {
                Debug.LogWarning($"[PlayerInputRecorder] Source file not found: {sourcePath}");
                return;
            }

            // Копируем файл
            File.Copy(sourcePath, destPath, true);

            // Обновляем Unity Asset Database
            UnityEditor.AssetDatabase.Refresh();

            Debug.Log($"[PlayerInputRecorder] ✅ Auto-exported to StreamingAssets: {destPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerInputRecorder] ❌ Failed to auto-export: {e.Message}");
        }
#endif
    }

    private string FormatBytes(long bytes)
    {
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1024 * 1024) return (bytes / 1024f).ToString("0.0") + " KB";
        if (bytes < 1024 * 1024 * 1024) return (bytes / 1024f / 1024f).ToString("0.0") + " MB";
        return (bytes / 1024f / 1024f / 1024f).ToString("0.00") + " GB";
    }

    // Метод для ручного экспорта из контекстного меню
    [ContextMenu("Export To StreamingAssets")]
    public void ManualExportToStreamingAssets()
    {
#if UNITY_EDITOR
        if (_frames.Count == 0)
        {
            Debug.LogWarning("[PlayerInputRecorder] No frames recorded!");
            return;
        }

        // Сначала сохраняем файл
        SaveToFile();

        // Затем экспортируем
        ExportToStreamingAssets();

        UnityEditor.EditorUtility.DisplayDialog(
            "Export Complete",
            $"Recording '{recordingName}' exported to StreamingAssets/Recordings/",
            "OK"
        );
#else
        Debug.LogWarning("[PlayerInputRecorder] Manual export only available in Unity Editor!");
#endif
    }

    [ContextMenu("Clear Recording")]
    public void ClearRecording()
    {
        if (_recording)
        {
            Debug.LogWarning("[PlayerInputRecorder] Cannot clear while recording!");
            return;
        }

        _frames.Clear();
        _cachedData = null;
        _hasLast = false;

        Debug.Log("[PlayerInputRecorder] Recording cleared");
    }

    [ContextMenu("Show Recording Info")]
    public void ShowRecordingInfo()
    {
        Debug.Log($"[PlayerInputRecorder] Recording Info:");
        Debug.Log($"   - Name: {recordingName}");
        Debug.Log($"   - Frames: {_frames.Count}");
        Debug.Log($"   - Duration: {(_frames.Count > 0 ? _frames[_frames.Count - 1].time : 0):F2}s");
        Debug.Log($"   - Is Recording: {_recording}");
        Debug.Log($"   - Estimated Size: {FormatBytes(GetCurrentEstimatedSize())}");

        if (_frames.Count > 0)
        {
            Debug.Log($"   - First frame time: {_frames[0].time:F3}s");
            Debug.Log($"   - Last frame time: {_frames[_frames.Count - 1].time:F3}s");
        }
    }

    private void OnDestroy()
    {
        // Если запись идёт и включен авто-экспорт - сохраняем
        if (_recording)
        {
            Debug.Log("[PlayerInputRecorder] Stopping recording due to destruction...");
            StopRecording();
        }
    }

    private void OnApplicationQuit()
    {
        // Если запись идёт - останавливаем
        if (_recording)
        {
            Debug.Log("[PlayerInputRecorder] Stopping recording due to application quit...");
            StopRecording();
        }
    }
}