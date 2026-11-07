using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ReplayLibrary
{
    public static readonly string RecordsFolder = Path.Combine(Application.persistentDataPath, "Replays");

    private static readonly HashSet<string> BusyRecordings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    static ReplayLibrary()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (!Directory.Exists(RecordsFolder))
            Directory.CreateDirectory(RecordsFolder);
#endif
    }

    public static string GetUniqueRecordingName(string baseName)
    {
        if (string.IsNullOrEmpty(baseName)) baseName = "recording";
        baseName = Path.GetFileName(baseName);

        string name = baseName;
        int i = 1;

        while (File.Exists(Path.Combine(RecordsFolder, name + ".bin")))
        {
            name = $"{baseName}_{i}";
            i++;
        }

        return name + ".bin";
    }

    public static List<string> GetAllRecordings()
    {
        var list = new List<string>();

#if !UNITY_WEBGL || UNITY_EDITOR
        if (Directory.Exists(RecordsFolder))
        {
            string[] files = Directory.GetFiles(RecordsFolder, "*.bin");
            for (int i = 0; i < files.Length; i++)
            {
                string file = Path.GetFileName(files[i]);
                list.Add(file);
            }
        }
#endif

        // Добавляем записи из StreamingAssets
        string[] embedded = ReplayResourceManager.GetAllRecordings();
        foreach (string file in embedded)
        {
            if (!list.Contains(file))
                list.Add(file);
        }

        list.Sort();
        return list;
    }

    public static long GetRecordingSize(string fileName)
    {
        string path = Path.Combine(RecordsFolder, fileName);
        if (!File.Exists(path)) return 0;
        return new FileInfo(path).Length;
    }

    public static string PickRandomRecording(string exclude = null, bool avoidSame = true)
    {
        var all = GetAllRecordings();
        if (all.Count == 0) return null;

        var free = new List<string>();
        for (int i = 0; i < all.Count; i++)
        {
            string f = all[i];
            if (BusyRecordings.Contains(f)) continue;
            if (!string.IsNullOrEmpty(exclude) && avoidSame && f.Equals(exclude, StringComparison.OrdinalIgnoreCase)) continue;
            free.Add(f);
        }

        if (free.Count == 0)
        {
            free = all;
            if (!string.IsNullOrEmpty(exclude) && avoidSame)
            {
                free.RemoveAll(x => x.Equals(exclude, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (free.Count == 0) return all[UnityEngine.Random.Range(0, all.Count)];

        return free[UnityEngine.Random.Range(0, free.Count)];
    }

    public static string PickRandomRecording()
    {
        return PickRandomRecording(null, true);
    }

    public static string GetNextAfter(string current)
    {
        var all = GetAllRecordings();
        if (all.Count == 0) return null;

        int idx = all.IndexOf(current);
        if (idx < 0) return all[0];

        int next = idx + 1;
        if (next >= all.Count) next = 0;

        return all[next];
    }

    public static string GetFullPath(string fileName)
    {
        return Path.Combine(RecordsFolder, fileName);
    }

    public static void MarkRecordingBusy(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return;
        BusyRecordings.Add(fileName);
    }

    public static void MarkRecordingFree(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return;
        BusyRecordings.Remove(fileName);
    }

    public static bool IsRecordingBusy(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return false;
        return BusyRecordings.Contains(fileName);
    }

    public static bool DeleteRecording(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return false;

        string path = GetFullPath(fileName);
        if (!File.Exists(path)) return false;

        try
        {
            File.Delete(path);
            MarkRecordingFree(fileName);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ReplayLibrary] Failed to delete {fileName}: {e.Message}");
            return false;
        }
    }

    public static void RefreshBusySet(HashSet<string> busySet)
    {
        if (busySet == null) return;
        busySet.Clear();
        foreach (var busy in BusyRecordings)
            busySet.Add(busy);
    }
}