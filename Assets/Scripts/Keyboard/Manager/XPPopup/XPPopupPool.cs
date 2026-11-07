using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class XPPopupPool : MonoBehaviour
{
    public static XPPopupPool Instance;

    [Header("Pool")]
    [SerializeField] private TMP_Text prefab;
    [SerializeField] private RectTransform parent;
    [SerializeField] private int poolSize = 15;

    [Header("Spawn")]
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform boundsArea;

    [Header("Animation")]
    [SerializeField] private float flyDuration = 0.35f;
    [SerializeField] private float waitAfterFly = 0.15f;
    [SerializeField] private float fadeDuration = 0.15f;
    [SerializeField] private float downDistance = 50f;

    private readonly Queue<Popup> pool = new();

    private class Popup
    {
        public TMP_Text text;
        public RectTransform rect;
        public CanvasGroup canvas;

        public GameObject gameObject => text.gameObject;
    }

    private void Awake()
    {
        Instance = this;

        for (int i = 0; i < poolSize; i++)
        {
            TMP_Text txt = Instantiate(prefab, parent);

            CanvasGroup cg = txt.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = txt.gameObject.AddComponent<CanvasGroup>();

            txt.gameObject.SetActive(false);

            pool.Enqueue(new Popup()
            {
                text = txt,
                rect = txt.rectTransform,
                canvas = cg
            });
        }
    }

    public void Spawn(long xp)
    {
        if (pool.Count == 0)
            return;

        Popup popup = pool.Dequeue();

        popup.gameObject.SetActive(true);
        popup.text.text = UTF.manager.TextToSprite("\U0001F603") + " +" + UTF.AbbreviateNumber(xp);

        StartCoroutine(Animate(popup));
    }

    private IEnumerator Animate(Popup popup)
    {
        // Центр Canvas
        Vector2 startPos = Vector2.zero;

        // Случайная точка внутри RectTransform
        Rect rect = boundsArea.rect;

        Vector2 localTarget = new Vector2(
            Random.Range(rect.xMin, rect.xMax),
            Random.Range(rect.yMin, rect.yMax));

        Vector2 targetPos = boundsArea.anchoredPosition + localTarget;

        popup.rect.anchoredPosition = startPos;
        popup.rect.localScale = Vector3.zero;
        popup.canvas.alpha = 1f;

        float timer = 0f;

        while (timer < flyDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / flyDuration);

            popup.rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            popup.rect.localScale = Vector3.LerpUnclamped(
                Vector3.zero,
                Vector3.one,
                Mathf.SmoothStep(0, 1, t));

            yield return null;
        }

        yield return new WaitForSeconds(waitAfterFly);

        Vector2 fadeStart = popup.rect.anchoredPosition;
        Vector2 fadeEnd = fadeStart + Vector2.down * downDistance;

        timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / fadeDuration);

            popup.rect.anchoredPosition = Vector2.Lerp(fadeStart, fadeEnd, t);
            popup.canvas.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        popup.gameObject.SetActive(false);
        pool.Enqueue(popup);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (boundsArea == null)
            return;

        Vector3[] corners = new Vector3[4];
        boundsArea.GetWorldCorners(corners);

        Gizmos.color = Color.green;

        for (int i = 0; i < 4; i++)
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
    }
#endif
}