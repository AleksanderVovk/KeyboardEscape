using UnityEngine;
using TMPro;

public class ToggleUIText : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private KeyCode toggleKey = KeyCode.T;
    [SerializeField] private bool includeInactive = true; // Искать даже на неактивных объектах

    private Canvas[] allCanvases;
    private TMP_Text[] allTexts;
    private bool isVisible = true;

    void Start()
    {
        // Находим все Canvas и TMP_Text в сцене при старте
        FindAllUIElements();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleVisibility();
        }
    }

    void FindAllUIElements()
    {
        // Находим все Canvas (включая неактивные)
        if (includeInactive)
        {
            allCanvases = FindObjectsOfType<Canvas>(true);
            allTexts = FindObjectsOfType<TMP_Text>(true);
        }
        else
        {
            allCanvases = FindObjectsOfType<Canvas>();
            allTexts = FindObjectsOfType<TMP_Text>();
        }
    }

    void ToggleVisibility()
    {
        isVisible = !isVisible;

        // Отключаем/включаем все Canvas
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas != null)
            {
                canvas.enabled = isVisible;
            }
        }

        // Отключаем/включаем все TMP_Text (включая тексты в мире)
        foreach (TMP_Text text in allTexts)
        {
            if (text != null)
            {
                text.enabled = isVisible;
            }
        }

        Debug.Log($"UI и тексты {(isVisible ? "включены" : "отключены")}");
    }

    // Метод для обновления списка элементов вручную (можно вызвать из инспектора)
    [ContextMenu("Обновить список UI элементов")]
    public void RefreshUIList()
    {
        FindAllUIElements();
        Debug.Log($"Найдено Canvas: {allCanvases.Length}, TMP_Text: {allTexts.Length}");
    }
}