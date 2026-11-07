using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class UiWorldTextToggles : MonoBehaviour
{
    [Header("Hotkeys")]
    [SerializeField] private KeyCode toggleAllTextKey = KeyCode.T;
    [SerializeField] private KeyCode toggleCanvasKey = KeyCode.C;

    [Header("Canvas (optional)")]
    [SerializeField] private Canvas targetCanvas;

    [Header("Auto disable world TMP")]
    [SerializeField] private float rescanInterval = 0.5f;

    private bool textsVisible = true;
    private bool canvasVisible = true;

    // Чтобы не делать лишнюю работу: помним, кого уже обработали
    private readonly HashSet<int> processedInstanceIds = new();

    private void Awake()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponentInChildren<Canvas>(true);
    }

    private void OnEnable()
    {
        InvokeRepeating(nameof(RescanAndApplyWorldTextState), 0f, rescanInterval);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(RescanAndApplyWorldTextState));
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleAllTextKey))
            ToggleAllText();

        if (Input.GetKeyDown(toggleCanvasKey))
            ToggleCanvas();
    }

    private void ToggleAllText()
    {
        textsVisible = !textsVisible;

        // Переключаем ВСЕ TMP (и UI, и world) одним проходом
        var all = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in all)
            ApplyTextState(t, textsVisible);

        // Сброс кеша, чтобы новый режим корректно применился к миру при следующих сканах
        processedInstanceIds.Clear();
    }

    private void ToggleCanvas()
    {
        if (!targetCanvas) return;
        canvasVisible = !canvasVisible;
        targetCanvas.enabled = canvasVisible;
    }

    // Каждые 0.5 сек ищем именно WORLD/3D тексты и гасим их, если сейчас тексты выключены
    private void RescanAndApplyWorldTextState()
    {
        if (textsVisible) return; // если тексты включены — ничего не делаем

        var all = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in all)
        {
            if (!t) continue;

            // Отсекаем UI-тексты: у них обычно есть CanvasRenderer
            if (t.GetComponent<CanvasRenderer>() != null)
                continue;

            int id = t.GetInstanceID();
            if (processedInstanceIds.Contains(id))
                continue;

            ApplyTextState(t, false);
            processedInstanceIds.Add(id);
        }
    }

    private static void ApplyTextState(TMP_Text t, bool visible)
    {
        if (!t) return;

        // TMP компонент
        t.enabled = visible;

        // Для 3D TextMeshPro дополнительно выключаем Renderer (MeshRenderer)
        var r = t.GetComponent<Renderer>();
        if (r) r.enabled = visible;
    }
}
