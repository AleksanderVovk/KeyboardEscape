using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ReplayDebugGUI : MonoBehaviour
{
    [Header("Recording")]
    [SerializeField] private PlayerInputRecorder playerRecorder;
    [SerializeField] private string recordingName = "recording.bin";

    [Header("NPC")]
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private float refreshInterval = 2f;

    private readonly List<NPCReplayController> _npcs = new List<NPCReplayController>();
    private string[] _npcNames = new string[0];
    private int _selectedNpcIndex;
    private Vector2 _recordScroll;
    private Vector2 _npcScroll;
    private Vector2 _fileScroll;
    private string _newRecordingName = "recording";
    private bool _showWindow = true;
    private Rect _windowRect = new Rect(20, 20, 620, 560);
    private float _nextRefreshTime;
    private float _recordStartTime;
    private bool _wasRecording;

    private void Start()
    {
        RefreshLists();
    }

    private void Update()
    {
        if (autoRefresh && Time.time >= _nextRefreshTime)
        {
            _nextRefreshTime = Time.time + refreshInterval;
            RefreshLists();
        }

        if (playerRecorder != null)
        {
            if (playerRecorder.IsRecording && !_wasRecording)
                _recordStartTime = Time.time;
            _wasRecording = playerRecorder.IsRecording;
        }
    }

    private void OnGUI()
    {
        if (!_showWindow) return;
        _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "Replay Debug");
    }

    private void DrawWindow(int id)
    {
        GUILayout.BeginVertical();

        GUILayout.Label("Recording name");
        _newRecordingName = GUILayout.TextField(_newRecordingName, GUILayout.Height(24));

        if (GUILayout.Button("Create / Use Recording Name", GUILayout.Height(26)))
        {
            if (playerRecorder != null)
            {
                string safe = ReplayLibrary.GetUniqueRecordingName(_newRecordingName);
                playerRecorder.SetRecordingName(safe);
                recordingName = safe;
                _newRecordingName = Path.GetFileNameWithoutExtension(safe);
            }
        }

        GUILayout.Space(6);
        DrawRecordingStatus();

        GUILayout.Space(8);
        GUILayout.BeginHorizontal();
        GUI.enabled = playerRecorder != null && !playerRecorder.IsRecording;
        if (GUILayout.Button("Start / Overwrite", GUILayout.Height(34)))
        {
            if (playerRecorder != null)
            {
                playerRecorder.StartRecording();
                _recordStartTime = Time.time;
            }
        }

        GUI.enabled = playerRecorder != null && playerRecorder.IsRecording;
        if (GUILayout.Button("Stop Recording", GUILayout.Height(34)))
        {
            if (playerRecorder != null)
                playerRecorder.StopRecording();
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("Saved recordings");
        DrawRecordingsList();

        GUILayout.Space(8);
        GUILayout.Label("NPC list");
        if (GUILayout.Button("Refresh NPCs", GUILayout.Height(24)))
            RefreshNPCs();

        DrawNPCList();

        GUILayout.Space(8);
        DrawNPCControls();

        GUILayout.Space(8);
        _showWindow = GUILayout.Toggle(_showWindow, "Show Window");

        GUILayout.EndVertical();
        GUI.DragWindow();
    }

    private void DrawRecordingStatus()
    {
        if (playerRecorder == null)
        {
            GUILayout.Label("Status: no recorder");
            return;
        }

        GUILayout.Label("Current file: " + playerRecorder.RecordingName);

        if (playerRecorder.IsRecording)
        {
            float elapsed = Time.time - _recordStartTime;
            GUILayout.Label("Status: RECORDING");
            GUILayout.Label("Time: " + FormatTime(elapsed));
            GUILayout.Label("Frames: " + playerRecorder.FrameCount);
            GUILayout.Label("Estimated size: " + FormatBytes(playerRecorder.GetCurrentEstimatedSize()));
        }
        else
        {
            GUILayout.Label("Status: idle");
            GUILayout.Label("Saved size: " + FormatBytes(playerRecorder.GetSavedFileSize()));
        }
    }

    private void DrawRecordingsList()
    {
        List<string> files = ReplayLibrary.GetAllRecordings();
        if (files.Count == 0)
        {
            GUILayout.Label("No recordings found.");
            return;
        }

        _fileScroll = GUILayout.BeginScrollView(_fileScroll, GUILayout.Height(170));
        for (int i = 0; i < files.Count; i++)
        {
            string file = files[i];
            long size = ReplayLibrary.GetRecordingSize(file);
            bool isBusy = ReplayLibrary.IsRecordingBusy(file);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(file, GUILayout.Height(22)))
            {
                recordingName = file;
                _newRecordingName = Path.GetFileNameWithoutExtension(file);
                if (playerRecorder != null)
                    playerRecorder.SetRecordingName(file);
            }

            GUILayout.Label(FormatBytes(size), GUILayout.Width(80));

            // ← НОВОЕ: кнопка Play
            GUI.enabled = !isBusy;
            if (GUILayout.Button("Play", GUILayout.Width(44), GUILayout.Height(22)))
                PlayRecordingOnSelectedNPC(file);
            GUI.enabled = true;

            // ← НОВОЕ: кнопка Delete
            if (GUILayout.Button("Delete", GUILayout.Width(54), GUILayout.Height(22)))
            {
                if (ReplayLibrary.DeleteRecording(file))
                {
                    Debug.Log($"[ReplayDebug] Deleted recording: {file}");
                    RefreshLists();
                }
                else
                {
                    Debug.LogWarning($"[ReplayDebug] Failed to delete recording: {file}");
                }
            }

            GUILayout.EndHorizontal();

            if (isBusy)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("▶ BUSY (playing by bot)", GUILayout.Height(18));
                GUILayout.EndHorizontal();
            }
        }
        GUILayout.EndScrollView();
    }

    private void DrawNPCList()
    {
        if (_npcs.Count == 0)
        {
            GUILayout.Label("No NPCReplayController found.");
            return;
        }

        _npcScroll = GUILayout.BeginScrollView(_npcScroll, GUILayout.Height(140));
        for (int i = 0; i < _npcs.Count; i++)
        {
            if (_npcs[i] == null) continue;
            string label = _npcs[i].name;
            if (i == _selectedNpcIndex) label = "> " + label;

            if (GUILayout.Button(label, GUILayout.Height(22)))
                _selectedNpcIndex = i;
        }
        GUILayout.EndScrollView();
    }

    private void DrawNPCControls()
    {
        if (_selectedNpcIndex < 0 || _selectedNpcIndex >= _npcs.Count || _npcs[_selectedNpcIndex] == null)
            return;

        NPCReplayController npc = _npcs[_selectedNpcIndex];
        GUILayout.Label("Selected: " + npc.name);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Play Selected", GUILayout.Height(30)))
            npc.LoadAndPlay(recordingName);

        if (GUILayout.Button("Stop Selected", GUILayout.Height(30)))
            npc.StopReplay();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Play Random", GUILayout.Height(30)))
        {
            string rec = ReplayLibrary.PickRandomRecording();
            if (!string.IsNullOrEmpty(rec))
                npc.LoadAndPlay(rec);
        }

        if (GUILayout.Button("Next Random On Finish", GUILayout.Height(30)))
        {
            string next = ReplayLibrary.GetNextAfter(recordingName);
            if (!string.IsNullOrEmpty(next))
                npc.LoadAndPlay(next);
        }
        GUILayout.EndHorizontal();
    }

    private void PlayRecordingOnSelectedNPC(string file)
    {
        if (_selectedNpcIndex < 0 || _selectedNpcIndex >= _npcs.Count) return;
        if (_npcs[_selectedNpcIndex] == null) return;
        _npcs[_selectedNpcIndex].LoadAndPlay(file);
    }

    private void RefreshLists()
    {
        RefreshNPCs();
    }

    private void RefreshNPCs()
    {
        _npcs.Clear();

#if UNITY_2023_1_OR_NEWER
        NPCReplayController[] found = Object.FindObjectsByType<NPCReplayController>(FindObjectsSortMode.None);
#else
        NPCReplayController[] found = Object.FindObjectsOfType<NPCReplayController>();
#endif

        if (found != null)
        {
            for (int i = 0; i < found.Length; i++)
                if (found[i] != null)
                    _npcs.Add(found[i]);
        }

        _npcNames = new string[_npcs.Count];
        for (int i = 0; i < _npcs.Count; i++)
            _npcNames[i] = _npcs[i].name;

        if (_selectedNpcIndex >= _npcs.Count)
            _selectedNpcIndex = 0;
    }

    private string FormatTime(float t)
    {
        int total = Mathf.FloorToInt(t);
        return string.Format("{0:00}:{1:00}", total / 60, total % 60);
    }

    private string FormatBytes(long bytes)
    {
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1024 * 1024) return (bytes / 1024f).ToString("0.0") + " KB";
        return (bytes / 1024f / 1024f).ToString("0.00") + " MB";
    }
}