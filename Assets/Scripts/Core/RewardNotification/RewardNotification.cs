using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class RewardNotification : MonoBehaviour
{
    public static RewardNotification instance;

    [Header("Refs")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text rewardName;
    [SerializeField] private TMP_Text rewardDescription;

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// Показать уведомление с иконкой и текстом.
    /// </summary>
    public static void Show(Sprite icon, string _rewardname, string _rewardDescription, Color nameColor, Color descriptionColor) => instance._Show(icon, _rewardname, _rewardDescription, nameColor, descriptionColor);
    private void _Show(Sprite icon, string _rewardName, string _rewardDescription, Color nameColor, Color descriptionColor)
    {
        // обновляем контент
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (rewardName != null)
        {
            rewardName.text = _rewardName;
            rewardName.color = nameColor;
        }

        if (rewardDescription != null)
        {
            rewardDescription.text = _rewardDescription;
            rewardDescription.color = descriptionColor;
        }

        PageManager.OpenPageByName("RewardNotification");
    }
}
