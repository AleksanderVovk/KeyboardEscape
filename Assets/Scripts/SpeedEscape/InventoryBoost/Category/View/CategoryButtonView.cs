using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CategoryButtonView : MonoBehaviour
{
    public event Action<CategoryButtonView> OnCategorySelected;

    [Header("Объект страницы (Panel/Popup)")]
    public GameObject pageObject;

    [Header("Кнопка UI")]
    public Button uiButton;

    [Header("Спрайты")]
    public Sprite defaultSprite;
    public Sprite selectedSprite;

    [Header("Image кнопки (опционально)")]
    public Image buttonImage;

    private bool isSelected;

    public bool IsSelected => isSelected;


    [SerializeField] private ScrollRect scrollView;

    public void ScrollToTop()
    {
        Canvas.ForceUpdateCanvases();

        if (scrollView != null)
            scrollView.verticalNormalizedPosition = 1f;
    }

    private void Awake()
    {
        if (uiButton == null)
            uiButton = GetComponent<Button>();

        if (buttonImage == null && uiButton != null)
            buttonImage = uiButton.GetComponent<Image>();

        if (uiButton != null)
        {
            uiButton.onClick.AddListener(OnButtonClick);
        }

        // Применяем стандартный спрайт при старте
        ApplySprite(defaultSprite);
        isSelected = false;
    }

    private void OnDestroy()
    {
        if (uiButton != null)
        {
            uiButton.onClick.RemoveListener(OnButtonClick);
        }
    }

    private void OnButtonClick()
    {
        OnCategorySelected?.Invoke(this);
        StopAllCoroutines();
        StartCoroutine(TopScrollView());
    }

    public void Select()
    {
        OnCategorySelected?.Invoke(this);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplySprite(selected ? selectedSprite : defaultSprite);
    }

    private IEnumerator TopScrollView()
    {
        yield return new WaitForSeconds(0.25f);
        ScrollToTop();
    }

    private void ApplySprite(Sprite sprite)
    {
        if (buttonImage != null && sprite != null)
        {
            buttonImage.sprite = sprite;
        }
    }
}