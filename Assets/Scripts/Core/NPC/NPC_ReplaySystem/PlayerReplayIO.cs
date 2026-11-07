using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerReplayData
{
    public List<ReplayFrame> frames = new List<ReplayFrame>(8192);
    public bool IsValid => frames != null && frames.Count > 0;
}

public static class PlayerReplayIO
{
    // Путь к папке с записями
    public static string GetPath(string recordingName)
    {
        return Path.Combine(ReplayLibrary.RecordsFolder, recordingName);
    }

    // Основной метод загрузки по имени (использует GetPath)
    public static PlayerReplayData Load(string recordingName)
    {
        string path = GetPath(recordingName);
        return LoadFromPath(path);
    }

    // Загрузка из пути (синхронно)
    public static PlayerReplayData LoadFromPath(string path)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.LogWarning("[PlayerReplayIO] Synchronous loading not supported in WebGL! Use LoadFromBytes or async loading.");
        return new PlayerReplayData();
#else
        if (!File.Exists(path))
        {
            Debug.LogWarning($"File not found: {path}");
            return new PlayerReplayData();
        }

        try
        {
            using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                return Deserialize(reader);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load recording {path}: {e.Message}");
            return new PlayerReplayData();
        }
#endif
    }

    // Загрузка из байтов (для WebGL)
    public static PlayerReplayData LoadFromBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            Debug.LogWarning("Empty byte array!");
            return new PlayerReplayData();
        }

        try
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                return Deserialize(reader);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load from bytes: {e.Message}");
            return new PlayerReplayData();
        }
    }

    // Десериализация
    private static PlayerReplayData Deserialize(BinaryReader reader)
    {
        PlayerReplayData data = new PlayerReplayData();

        int version = reader.ReadInt32();
        int count = reader.ReadInt32();

        for (int i = 0; i < count; i++)
        {
            ReplayFrame f = new ReplayFrame();
            f.time = reader.ReadSingle();
            f.position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            if (version >= 5)
            {
                f.rootRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                f.bodyLocalRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                f.velocity = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
            else
            {
                f.rootRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                f.velocity = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                f.bodyLocalRotation = Quaternion.identity;
            }

            f.moveX = reader.ReadInt16();
            f.moveY = reader.ReadInt16();
            f.lookX = reader.ReadInt16();
            f.lookY = reader.ReadInt16();
            f.flags = reader.ReadByte();

            data.frames.Add(f);
        }

        return data;
    }

    // Сохранение в путь
    public static void SaveToPath(string path, PlayerReplayData data)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.LogWarning("[PlayerReplayIO] Saving not supported in WebGL! Use SaveToBytes and upload to server.");
#else
        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
            {
                Serialize(writer, data);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save recording {path}: {e.Message}");
        }
#endif
    }

    // Сохранение в байты (для WebGL)
    public static byte[] SaveToBytes(PlayerReplayData data)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            Serialize(writer, data);
            return stream.ToArray();
        }
    }

    // Сериализация
    private static void Serialize(BinaryWriter writer, PlayerReplayData data)
    {
        const int VERSION = 5;

        writer.Write(VERSION);
        writer.Write(data.frames.Count);

        foreach (ReplayFrame f in data.frames)
        {
            writer.Write(f.time);
            writer.Write(f.position.x);
            writer.Write(f.position.y);
            writer.Write(f.position.z);
            writer.Write(f.rootRotation.x);
            writer.Write(f.rootRotation.y);
            writer.Write(f.rootRotation.z);
            writer.Write(f.rootRotation.w);
            writer.Write(f.bodyLocalRotation.x);
            writer.Write(f.bodyLocalRotation.y);
            writer.Write(f.bodyLocalRotation.z);
            writer.Write(f.bodyLocalRotation.w);
            writer.Write(f.velocity.x);
            writer.Write(f.velocity.y);
            writer.Write(f.velocity.z);
            writer.Write(f.moveX);
            writer.Write(f.moveY);
            writer.Write(f.lookX);
            writer.Write(f.lookY);
            writer.Write(f.flags);
        }
    }

    // Проверка существования файла
    public static bool RecordingExists(string recordingName)
    {
        string path = GetPath(recordingName);
        return File.Exists(path);
    }

    // Получение размера файла
    public static long GetRecordingSize(string recordingName)
    {
        string path = GetPath(recordingName);
        if (!File.Exists(path)) return 0;
        return new FileInfo(path).Length;
    }

    // Удаление файла
    public static bool DeleteRecording(string recordingName)
    {
        string path = GetPath(recordingName);
        if (!File.Exists(path)) return false;

        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete recording {path}: {e.Message}");
            return false;
        }
    }

    // Получить все записи в папке
    public static List<string> GetAllRecordings()
    {
        var list = new List<string>();

#if !UNITY_WEBGL || UNITY_EDITOR
        string folder = ReplayLibrary.RecordsFolder;
        if (Directory.Exists(folder))
        {
            string[] files = Directory.GetFiles(folder, "*.bin");
            for (int i = 0; i < files.Length; i++)
            {
                string file = Path.GetFileName(files[i]);
                list.Add(file);
            }
        }
#endif

        return list;
    }

    // Получить полный путь к файлу (алиас для GetPath)
    public static string GetFullPath(string recordingName)
    {
        return GetPath(recordingName);
    }
}