using UnityEngine;
using System.Collections;

public class WebGLStartup : MonoBehaviour
{
    [Header("Startup Settings")]
    [SerializeField] private bool assignBotsAfterLoad = true;
    [SerializeField] private float delayBeforeAssign = 0.5f;

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("[WebGLStartup] Starting WebGL initialization...");
        StartCoroutine(InitializeWebGL());
#else
        Debug.Log("[WebGLStartup] Running in Editor, skipping WebGL init");
#endif
    }

    private IEnumerator InitializeWebGL()
    {
        // Ждем пока загрузится менеджер
        yield return new WaitForSeconds(0.5f);

        // Создаем WebGLReplayLoader если его нет
        if (WebGLReplayLoader.Instance == null)
        {
            Debug.Log("[WebGLStartup] Creating WebGLReplayLoader...");
            GameObject loader = new GameObject("WebGLReplayLoader");
            WebGLReplayLoader loaderComponent = loader.AddComponent<WebGLReplayLoader>();
            // Загружаем вручную
            loaderComponent.LoadAllRecordings();
        }

        // ✅ ЖДЕМ ПОЛНОЙ ЗАГРУЗКИ
        float timeout = 30f;
        float startTime = Time.realtimeSinceStartup;

        while (WebGLReplayLoader.Instance.IsLoading)
        {
            if (Time.realtimeSinceStartup - startTime > timeout)
            {
                Debug.LogWarning("[WebGLStartup] Load timeout!");
                break;
            }

            float progress = WebGLReplayLoader.Instance.GetLoadProgress();
            Debug.Log($"[WebGLStartup] Loading progress: {progress:P0}");
            yield return new WaitForSeconds(0.5f);
        }

        // ✅ ДОПОЛНИТЕЛЬНАЯ ЗАДЕРЖКА
        yield return new WaitForSeconds(delayBeforeAssign);

        int count = WebGLReplayLoader.Instance.GetAllRecordings().Count;
        Debug.Log($"[WebGLStartup] Loaded {count} recordings");

        if (assignBotsAfterLoad && count > 0)
        {
            if (BotReplayManager.Instance != null)
            {
                Debug.Log("[WebGLStartup] BotReplayManager will assign recordings...");
                // Не вызываем AssignRandomToAllBots(), так как BotReplayManager сам инициализируется
            }
            else
            {
                Debug.LogWarning("[WebGLStartup] BotReplayManager not found!");
            }
        }

        Debug.Log("[WebGLStartup] Initialization complete!");
    }
}