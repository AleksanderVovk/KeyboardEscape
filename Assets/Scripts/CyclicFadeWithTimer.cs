using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

public class CyclicFadeWithTimer : MonoBehaviour
{
    [Header("Настройки объектов")]
    public GameObject[] targetObjects; // Кнопки и объекты для fade
    public Renderer urpMaterialRenderer; // Объект с URP материалом

    [Header("Коллайдеры")]
    public Collider[] collidersToDisable; // Коллайдеры для отключения

    [Header("Настройки таймера")]
    public float countdownDuration = 3f; // Время обратного отсчёта
    public float fadeDuration = 1.25f; // Время исчезновения
    public float waitDuration = 1.25f; // Время ожидания перед появлением

    [Header("UI")]
    public TMP_Text timerText; // Текст для отображения таймера

    [Header("Локализация")]
    public LanguageDirectory waitText; // Текст "Подождите..."

    [Header("Цвета текста")]
    public Color normalTextColor = Color.white;
    public Color warningTextColor = Color.red;

    [Header("Настройки цикла")]
    public bool enableCyclic = true; // Циклическое повторение
    public float cycleDelay = 1f; // Задержка перед новым циклом

    private bool isFading = false;
    private Coroutine currentCoroutine;

    // Храним оригинальные материалы
    private Material[] originalUrpMaterials;
    private Material[] fadeUrpMaterials;

    // Материалы для кнопок (как в оригинальном скрипте)
    private List<Material[]> fadeMaterials = new List<Material[]>();

    void Start()
    {
        InitializeMaterials();
        StartCycle();
    }

    void InitializeMaterials()
    {
        // Инициализация URP материала
        if (urpMaterialRenderer != null)
        {
            Material[] mats = urpMaterialRenderer.materials;
            originalUrpMaterials = mats;

            fadeUrpMaterials = new Material[mats.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                fadeUrpMaterials[i] = new Material(mats[i]);
                if (fadeUrpMaterials[i].HasProperty("_BaseColor") ||
                    fadeUrpMaterials[i].HasProperty("_Color"))
                {
                    Color color = fadeUrpMaterials[i].HasProperty("_BaseColor")
                        ? fadeUrpMaterials[i].GetColor("_BaseColor")
                        : fadeUrpMaterials[i].GetColor("_Color");
                    color.a = 1f;

                    if (fadeUrpMaterials[i].HasProperty("_BaseColor"))
                        fadeUrpMaterials[i].SetColor("_BaseColor", color);
                    else
                        fadeUrpMaterials[i].SetColor("_Color", color);
                }
            }

            urpMaterialRenderer.materials = fadeUrpMaterials;
        }

        // Инициализация материалов для кнопок
        fadeMaterials.Clear();

        foreach (GameObject obj in targetObjects)
        {
            if (obj != null)
            {
                Debug.Log($"[CyclicFade] Инициализация объекта: {obj.name}");

                // Renderer в детях
                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

                foreach (Renderer renderer in renderers)
                {
                    if (renderer != null)
                    {
                        Material[] mats = renderer.materials;
                        Material[] fadeMats = new Material[mats.Length];

                        for (int i = 0; i < mats.Length; i++)
                        {
                            fadeMats[i] = new Material(mats[i]);

                            // Устанавливаем _Alpha = 1
                            if (fadeMats[i].HasProperty("_Alpha"))
                            {
                                fadeMats[i].SetFloat("_Alpha", 1f);
                                Debug.Log($"[CyclicFade] Найден материал с _Alpha: {fadeMats[i].name}");
                            }
                        }

                        renderer.materials = fadeMats;
                        fadeMaterials.Add(fadeMats);
                        Debug.Log($"[CyclicFade] Добавлено материалов: {fadeMats.Length}");
                    }
                }
            }
        }

        Debug.Log($"[CyclicFade] Инициализация завершена:fadeMaterials={fadeMaterials.Count}");
    }

    public void StartCycle()
    {
        if (!isFading)
        {
            if (currentCoroutine != null)
                StopCoroutine(currentCoroutine);

            currentCoroutine = StartCoroutine(FadeSequence());
        }
    }

    IEnumerator FadeSequence()
    {
        isFading = true;

        // 1. Обратный отсчёт (3 секунды)
        yield return StartCoroutine(CountdownPhase());

        // 2. Исчезновение (1.25 секунды)
        yield return StartCoroutine(FadeAlpha(1f, 0f, fadeDuration));

        // 3. Отключаем коллайдеры
        SetCollidersState(false);

        // 4. Ожидание (1.25 секунды)
        yield return new WaitForSeconds(waitDuration);

        // 5. Включаем коллайдеры СРАЗУ перед появлением
        SetCollidersState(true);

        // 6. Появление (1.25 секунды)
        yield return StartCoroutine(FadeAlpha(0f, 1f, 0.15f));

        isFading = false;

        // 7. Если циклично - повторяем
        if (enableCyclic)
        {
            yield return new WaitForSeconds(cycleDelay);
            StartCycle();
        }
    }

    IEnumerator CountdownPhase()
    {
        float elapsedTime = 0f;

        while (elapsedTime < countdownDuration)
        {
            elapsedTime += Time.deltaTime;
            float remainingTime = countdownDuration - elapsedTime;

            // Обновляем текст таймера
            if (timerText != null)
            {
                // Формат: секунды.миллисекунды (0.00)
                timerText.text = remainingTime.ToString("F2");

                // Меняем цвет текста
                if (remainingTime <= 1f)
                {
                    timerText.color = warningTextColor;
                }
                else
                {
                    timerText.color = normalTextColor;
                }
            }

            yield return null;
        }

        // После 3 секунд меняем текст на "Подождите..."
        if (timerText != null)
        {
            timerText.text = UTF.ReadLanguageDirectory(waitText);
        }
    }

    IEnumerator FadeAlpha(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);

            SetAlphaForAllMaterials(currentAlpha);

            yield return null;
        }

        SetAlphaForAllMaterials(endAlpha);
    }

    void SetAlphaForAllMaterials(float alpha)
    {
        // Кнопки - через _Alpha свойство
        int materialsWithAlpha = 0;

        for (int i = 0; i < fadeMaterials.Count; i++)
        {
            if (fadeMaterials[i] != null)
            {
                foreach (Material mat in fadeMaterials[i])
                {
                    if (mat != null && mat.HasProperty("_Alpha"))
                    {
                        mat.SetFloat("_Alpha", alpha);
                        materialsWithAlpha++;
                    }
                }
            }
        }

        // Изменяем прозрачность URP материала
        if (urpMaterialRenderer != null && fadeUrpMaterials != null)
        {
            foreach (Material mat in fadeUrpMaterials)
            {
                if (mat != null)
                {
                    if (mat.HasProperty("_BaseColor"))
                    {
                        Color color = mat.GetColor("_BaseColor");
                        color.a = alpha;
                        mat.SetColor("_BaseColor", color);
                    }
                    else if (mat.HasProperty("_Color"))
                    {
                        Color color = mat.GetColor("_Color");
                        color.a = alpha;
                        mat.SetColor("_Color", color);
                    }
                }
            }
        }
    }

    void SetCollidersState(bool state)
    {
        foreach (Collider collider in collidersToDisable)
        {
            if (collider != null)
            {
                collider.enabled = state;
            }
        }
    }

    void OnDisable()
    {
        // Очищаем корутину при отключении
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        // Восстанавливаем коллайдеры
        SetCollidersState(true);
    }
}