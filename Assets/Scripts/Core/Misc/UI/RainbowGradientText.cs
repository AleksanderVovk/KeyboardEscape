using TMPro;
using UnityEngine;

public class RainbowGradientText : MonoBehaviour
{
    public float cycleSpeed = 1f;

    private TMP_Text text;
    private float hue = 0f;

    private void Start()
    {
        text = GetComponent<TMP_Text>();

        if (text == null)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        hue += Time.deltaTime * cycleSpeed;

        if (hue > 1f)
        {
            hue -= 1f;
        }

        Color topColor = Color.HSVToRGB(hue, 0.6f, 1f);
        Color downColor = Color.HSVToRGB(hue, 1f, 1f);

        if (text != null)
        {
            TMP_ColorGradient gradient = ScriptableObject.CreateInstance<TMP_ColorGradient>();
            gradient.topLeft = topColor;
            gradient.topRight = topColor;
            gradient.bottomLeft = downColor;
            gradient.bottomRight = downColor;

            text.colorGradientPreset = gradient;
        }
    }
}
