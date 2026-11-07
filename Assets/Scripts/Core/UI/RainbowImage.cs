using UnityEngine;
using UnityEngine.UI;

public class RainbowImage : MonoBehaviour
{
    [Header("—сылка на UI Image")]
    [SerializeField] private Image targetImage;

    [Header("—корость смены цвета (круг за секунды)")]
    [SerializeField] private float speed = 1f;

    // текущий угол тона [0..1]
    private float hue;

    private void Reset()
    {
        // јвтоматически подхватить Image с этого объекта
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        // стартуем с текущего цвета, чтобы не прыгало
        if (targetImage != null)
            Color.RGBToHSV(targetImage.color, out hue, out _, out _);
    }

    private void Update()
    {
        if (targetImage == null) return;

        // крутим тон по кругу 0..1
        hue += speed * Time.deltaTime;
        if (hue > 1f) hue -= 1f;

        // насыщенность и €ркость = 1 (чиста€ радуга)
        Color rainbow = Color.HSVToRGB(hue, 1f, 1f);

        // сохран€ем альфу исходной картинки
        rainbow.a = targetImage.color.a;

        targetImage.color = rainbow;
    }
}
