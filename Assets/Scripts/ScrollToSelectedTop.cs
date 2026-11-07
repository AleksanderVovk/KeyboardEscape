using UnityEngine;
using UnityEngine.UI;

public class ScrollToSelectedTop : MonoBehaviour
{
    [SerializeField] private Button targetButton;
    [SerializeField] private ScrollRect scrollView;

    private void Awake()
    {
        if (targetButton != null)
            targetButton.onClick.AddListener(ScrollToTop);
    }

    public void ScrollToTop()
    {
        if (scrollView == null)
        {
            Debug.LogWarning("[Scroll] ScrollView не назначен.");
            return;
        }

        Canvas.ForceUpdateCanvases();

        scrollView.verticalNormalizedPosition = 1f;
    }

    private void OnDestroy()
    {
        if (targetButton != null)
            targetButton.onClick.RemoveListener(ScrollToTop);
    }
}