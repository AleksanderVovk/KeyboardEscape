using UnityEngine;
using UnityEngine.UI;

public class ShopTabsScroll : MonoBehaviour
{
    public static ShopTabsScroll instance;

    [Header("Scroll")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform goldStartMarker;
    [SerializeField] private RectTransform diamondsStartMarker;

    [Header("Buttons")]
    [SerializeField] private Button goldButton;
    [SerializeField] private Button diamondsButton;
    [SerializeField] private Image goldButtonImage;
    [SerializeField] private Image diamondsButtonImage;
    [SerializeField] private Sprite goldNormal;
    [SerializeField] private Sprite goldActive;
    [SerializeField] private Sprite diamondsNormal;
    [SerializeField] private Sprite diamondsActive;

    [Header("Settings")]
    [SerializeField] private float lerpSpeed = 10f;
    [SerializeField] private float snapThreshold = 0.001f;

    // Тут можно выставить в инспекторе конкретные значения
    [Header("Fixed Positions (0=bottom,1=top)")]
    [Range(0f, 1f)]
    [SerializeField] private float goldTargetPos = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float diamondsTargetPos = 0.5f;

    private float targetNormalizedPos = -1f;

    private void Awake()
    {
        instance = this;

        goldButton.onClick.AddListener(ScrollToGold);
        diamondsButton.onClick.AddListener(ScrollToDiamonds);
    }

    private void Update()
    {
        if (targetNormalizedPos >= 0f)
        {
            float current = scrollRect.verticalNormalizedPosition;
            float next = Mathf.Lerp(current, targetNormalizedPos, Time.deltaTime * lerpSpeed);
            scrollRect.verticalNormalizedPosition = next;

            if (Mathf.Abs(next - targetNormalizedPos) < snapThreshold)
            {
                scrollRect.verticalNormalizedPosition = targetNormalizedPos;
                targetNormalizedPos = -1f;
            }
        }

        UpdateTabByCurrentScroll();
    }

    // ---------- ПЕРЕКЛЮЧЕНИЕ ПО КЛИКУ -------------

    public void OpenDonatePage(ValueType valueType)
    {
        PageManager.OpenPageByName("DonateShop");

        switch (valueType)
        {
            case ValueType.Coin:
                ScrollToGold();
                break;
            case ValueType.Diamond:
                ScrollToDiamonds();
                break;
        }
    }

    public void OpenDonatePage_Diamond()
    {
        OpenDonatePage(ValueType.Diamond);
    }

    public void ScrollToTopInstant()
    {
        PageManager.OpenPageByName("DonateShop");

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }


    private void ScrollToGold()
    {
        // Листаем к фиксированному значению (по умолчанию 1 = верх)
        targetNormalizedPos = Mathf.Clamp01(goldTargetPos);
        SetActiveTab(isGold: true);
    }

    private void ScrollToDiamonds()
    {
        // Вариант 1: тоже фиксированное значение из инспектора
        targetNormalizedPos = Mathf.Clamp01(diamondsTargetPos);

        // Вариант 2: если хочешь по маркеру — раскомментируй:
        // targetNormalizedPos = GetNormalizedPositionFor(diamondsStartMarker);

        SetActiveTab(isGold: false);
    }

    // Пересчёт нормализованной позиции (0–1) по маркеру
    private float GetNormalizedPositionFor(RectTransform marker)
    {
        float contentHeight = scrollRect.content.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;
        float maxScroll = contentHeight - viewportHeight;
        if (maxScroll <= 0f) return 1f;

        float markerY = Mathf.Abs(marker.anchoredPosition.y);
        float scrollY = Mathf.Clamp(markerY, 0, maxScroll);
        float normalized = 1f - (scrollY / maxScroll);
        return normalized;
    }

    // ---------- СМЕНА КАРТИНОК ПО СКРОЛЛУ -------------

    private void UpdateTabByCurrentScroll()
    {
        float current = scrollRect.verticalNormalizedPosition;
        float diamondPos = GetNormalizedPositionFor(diamondsStartMarker);

        bool isGoldZone = Mathf.Abs(current - 1f) <= Mathf.Abs(current - diamondPos);
        SetActiveTab(isGoldZone);
    }

    private void SetActiveTab(bool isGold)
    {
        if (isGold)
        {
            goldButtonImage.sprite = goldActive;
            diamondsButtonImage.sprite = diamondsNormal;
        }
        else
        {
            goldButtonImage.sprite = goldNormal;
            diamondsButtonImage.sprite = diamondsActive;
        }
    }
}
