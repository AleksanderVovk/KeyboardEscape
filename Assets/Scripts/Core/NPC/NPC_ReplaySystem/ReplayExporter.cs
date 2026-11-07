using UnityEngine;
using System.IO;
using UnityEditor;

#if UNITY_EDITOR
public class ReplayExporter : MonoBehaviour
{
    [Header("Export Settings")]
    [SerializeField] private string recordingName = "my_recording.bin";
    [SerializeField] private bool exportOnStop = true;

    private PlayerInputRecorder recorder;
    private string exportPath;

    private void Start()
    {
        recorder = GetComponent<PlayerInputRecorder>();

        // ✅ ИНИЦИАЛИЗИРУЕМ ПУТЬ ЗДЕСЬ
        exportPath = Path.Combine(Application.dataPath, "StreamingAssets/Recordings");

        // Создаём папку если её нет
        if (!Directory.Exists(exportPath))
        {
            Directory.CreateDirectory(exportPath);
            Debug.Log($"[ReplayExporter] Created directory: {exportPath}");
        }
    }

    // Экспорт всех записей из папки persistentDataPath
    [ContextMenu("Export All Recordings")]
    public void ExportAllRecordings()
    {
        // ✅ Убеждаемся что путь инициализирован
        if (string.IsNullOrEmpty(exportPath))
        {
            exportPath = Path.Combine(Application.dataPath, "StreamingAssets/Recordings");
            if (!Directory.Exists(exportPath))
                Directory.CreateDirectory(exportPath);
        }

        string sourcePath = ReplayLibrary.RecordsFolder;
        if (!Directory.Exists(sourcePath))
        {
            Debug.LogWarning($"[ReplayExporter] No recordings found in: {sourcePath}");
            return;
        }

        string[] files = Directory.GetFiles(sourcePath, "*.bin");
        if (files.Length == 0)
        {
            Debug.LogWarning($"[ReplayExporter] No .bin files found in: {sourcePath}");
            return;
        }

        int exportedCount = 0;
        int failedCount = 0;

        foreach (string file in files)
        {
            try
            {
                string fileName = Path.GetFileName(file);
                string destPath = Path.Combine(exportPath, fileName);

                // Проверяем, существует ли уже файл
                if (File.Exists(destPath))
                {
                    // Спрашиваем пользователя
                    if (!EditorUtility.DisplayDialog(
                        "File Already Exists",
                        $"File '{fileName}' already exists in StreamingAssets.\nOverwrite?",
                        "Yes, Overwrite",
                        "Skip"))
                    {
                        Debug.Log($"[ReplayExporter] Skipped: {fileName}");
                        continue;
                    }
                }

                // Копируем файл
                File.Copy(file, destPath, true);

                // Получаем информацию о файле
                var fileInfo = new FileInfo(file);
                Debug.Log($"[ReplayExporter] ✅ Exported: {fileName} ({FormatBytes(fileInfo.Length)})");
                exportedCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ReplayExporter] ❌ Failed to export {Path.GetFileName(file)}: {e.Message}");
                failedCount++;
            }
        }

        // Обновляем Unity
        AssetDatabase.Refresh();

        // Показываем итоговое сообщение
        string summary = $"📦 Export Complete!\n" +
                        $"✅ Successfully exported: {exportedCount} files\n" +
                        $"❌ Failed: {failedCount} files\n" +
                        $"📁 Target folder: {exportPath}";

        Debug.Log($"[ReplayExporter] {summary}");
        EditorUtility.DisplayDialog("Export Complete", summary, "OK");
    }

    [ContextMenu("Open Recordings Folder")]
    public void OpenRecordingsFolder()
    {
        // ✅ Убеждаемся что путь инициализирован
        if (string.IsNullOrEmpty(exportPath))
        {
            exportPath = Path.Combine(Application.dataPath, "StreamingAssets/Recordings");
            if (!Directory.Exists(exportPath))
                Directory.CreateDirectory(exportPath);
        }

        if (Directory.Exists(exportPath))
        {
            EditorUtility.RevealInFinder(exportPath);
        }
        else
        {
            Debug.LogError($"[ReplayExporter] Folder not found: {exportPath}");
        }
    }

    [ContextMenu("Open PersistentDataPath")]
    public void OpenPersistentDataPath()
    {
        string path = Application.persistentDataPath;
        if (Directory.Exists(path))
        {
            EditorUtility.RevealInFinder(path);
            Debug.Log($"[ReplayExporter] Opening: {path}");
        }
        else
        {
            Debug.LogError($"[ReplayExporter] Folder not found: {path}");
        }
    }

    [ContextMenu("Clear StreamingAssets Recordings")]
    public void ClearStreamingAssetsRecordings()
    {
        // ✅ Убеждаемся что путь инициализирован
        if (string.IsNullOrEmpty(exportPath))
        {
            exportPath = Path.Combine(Application.dataPath, "StreamingAssets/Recordings");
            if (!Directory.Exists(exportPath))
                Directory.CreateDirectory(exportPath);
        }

        if (!Directory.Exists(exportPath))
        {
            Debug.LogWarning("[ReplayExporter] No recordings folder found.");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Clear All Recordings?",
            $"This will delete ALL files in:\n{exportPath}\n\nAre you sure?",
            "Yes, Delete All",
            "Cancel"))
        {
            return;
        }

        string[] files = Directory.GetFiles(exportPath, "*.bin");
        int deletedCount = 0;

        foreach (string file in files)
        {
            try
            {
                File.Delete(file);
                Debug.Log($"[ReplayExporter] Deleted: {Path.GetFileName(file)}");
                deletedCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ReplayExporter] Failed to delete {Path.GetFileName(file)}: {e.Message}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[ReplayExporter] Deleted {deletedCount} files from StreamingAssets");
        EditorUtility.DisplayDialog("Clear Complete", $"Deleted {deletedCount} files.", "OK");
    }

    // Вспомогательный метод для форматирования размера
    private string FormatBytes(long bytes)
    {
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1024 * 1024) return (bytes / 1024f).ToString("0.0") + " KB";
        if (bytes < 1024 * 1024 * 1024) return (bytes / 1024f / 1024f).ToString("0.0") + " MB";
        return (bytes / 1024f / 1024f / 1024f).ToString("0.00") + " GB";
    }
}
#endif