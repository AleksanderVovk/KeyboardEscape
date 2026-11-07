using System.Collections;
using System.Linq;
using UnityEngine;

public class CategorySelector : MonoBehaviour
{
    [Header("Кнопки категорий")]
    public CategoryButtonView[] categoryButtons;

    [Header("Настройки pop-up анимации")]
    [Tooltip("Время от 0 до 1.25")]
    public float popInDuration = 0.15f;
    [Tooltip("Время от 1.25 до 1")]
    public float popSettleDuration = 0.1f;
    [Tooltip("Время исчезновения (scale 1 -> 0)")]
    public float popOutDuration = 0.12f;

    private CategoryButtonView currentActiveButton;

    private void Awake()
    {
        foreach (var button in categoryButtons)
        {
            if (button != null)
            {
                button.OnCategorySelected += OnCategorySelected;
            }
        }

        foreach (var button in categoryButtons)
        {
            if (button != null && button.pageObject != null)
            {
                button.pageObject.SetActive(false);
            }
        }

        UpdateButtonVisuals(null);

        categoryButtons[0].Select();
    }

    private void OnDestroy()
    {
        foreach (var button in categoryButtons)
        {
            if (button != null)
            {
                button.OnCategorySelected -= OnCategorySelected;
            }
        }
    }

    private void OnCategorySelected(CategoryButtonView selectedButton)
    {
        if (selectedButton == null || selectedButton.pageObject == null)
            return;

        // Если уже активна эта же кнопка — закрываем
        if (currentActiveButton == selectedButton)
        {
            CloseCurrentPage();
            return;
        }

        // Скрываем текущую страницу
        if (currentActiveButton != null && currentActiveButton.pageObject != null)
        {
            StartCoroutine(PopOut(currentActiveButton.pageObject));
        }

        // Включаем новую страницу
        selectedButton.pageObject.SetActive(true);
        var rt = selectedButton.pageObject.GetComponent<RectTransform>();
        if (rt == null)
        {
            rt = selectedButton.pageObject.AddComponent<RectTransform>();
        }

        // Начальный масштаб 0
        rt.localScale = Vector3.zero;

        currentActiveButton = selectedButton;

        // Обновляем визуальное состояние кнопок
        UpdateButtonVisuals(selectedButton);

        // Анимация pop-in
        StartCoroutine(PopIn(rt));
    }

    private void CloseCurrentPage()
    {
        if (currentActiveButton == null || currentActiveButton.pageObject == null)
            return;

        StartCoroutine(PopOut(currentActiveButton.pageObject));

        UpdateButtonVisuals(null);

        currentActiveButton = null;
    }

    private void UpdateButtonVisuals(CategoryButtonView activeButton)
    {
        foreach (var button in categoryButtons)
        {
            if (button == null) continue;

            bool selected = (button == activeButton);
            button.SetSelected(selected);
        }
    }

    private IEnumerator PopIn(RectTransform rt)
    {
        // Фаза 1: 0 -> 1.25
        float t = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 overshootScale = Vector3.one * 1.25f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / popInDuration;
            float k = Mathf.Clamp01(t);
            rt.localScale = Vector3.Lerp(startScale, overshootScale, k);
            yield return null;
        }

        rt.localScale = overshootScale;

        // Фаза 2: 1.25 -> 1
        t = 0f;
        Vector3 finalScale = Vector3.one;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / popSettleDuration;
            float k = Mathf.Clamp01(t);
            rt.localScale = Vector3.Lerp(overshootScale, finalScale, k);
            yield return null;
        }

        rt.localScale = finalScale;
    }

    private IEnumerator PopOut(GameObject page)
    {
        var rt = page.GetComponent<RectTransform>();
        if (rt == null)
        {
            page.SetActive(false);
            yield break;
        }

        float t = 0f;
        Vector3 startScale = rt.localScale;
        Vector3 endScale = Vector3.zero;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / popOutDuration;
            float k = Mathf.Clamp01(t);
            rt.localScale = Vector3.Lerp(startScale, endScale, k);
            yield return null;
        }

        rt.localScale = endScale;
        page.SetActive(false);
    }
}