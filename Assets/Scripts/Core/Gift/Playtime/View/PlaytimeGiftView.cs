using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlaytimeGiftView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Button claimButton;
    [SerializeField] private GameObject claimedRoot;

    public PlaytimeGiftData giftData;
    private PlaytimeGiftRewardReceiver rewardReceiver;
    private Action<PlaytimeGiftView> onClaimed;
    private float availableAtTime;
    public bool isClaimed;

    // Флаги для предотвращения многократного выполнения
    private bool wasClaimedThisSession = false;
    private bool wasAvailableThisSession = false;

    // Public свойства для доступа из других классов
    public PlaytimeGiftData GiftData => giftData;
    public bool IsClaimed => isClaimed;

    private readonly LanguageDirectory takeTextLang = new LanguageDirectory("Забрать", "Claim", "Al");

    public void Initialize(PlaytimeGiftData data, float elapsedPlaytime, PlaytimeGiftRewardReceiver receiver, Action<PlaytimeGiftView> claimedCallback)
    {
        giftData = data;
        rewardReceiver = receiver;
        onClaimed = claimedCallback;

        iconImage.sprite = data.icon;
        ApplyTitleGradient(UTF.ReadLanguageDirectory(data.title), data.gradientColor1, data.gradientColor2);
        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(Claim);

        availableAtTime = data.requiredSeconds;
        isClaimed = elapsedPlaytime >= availableAtTime;

        // Сбрасываем флаги при инициализации
        wasClaimedThisSession = false;
        wasAvailableThisSession = false;

        Refresh(elapsedPlaytime);
    }

    public void Refresh(float elapsedPlaytime)
    {
        if (isClaimed)
        {
            if (!wasClaimedThisSession)
            {
                timerText.text = "";
                claimButton.interactable = false;
                claimedRoot.SetActive(true);
                wasClaimedThisSession = true;
            }
            return;
        }

        float remaining = Mathf.Max(0f, availableAtTime - elapsedPlaytime);

        if (remaining <= 0f)
        {
            timerText.text = UTF.ReadLanguageDirectory(takeTextLang);
            claimButton.interactable = true;
            claimedRoot.SetActive(false);
            wasAvailableThisSession = true;
        }
        else
        {
            timerText.text = FormatTime(remaining);
            claimButton.interactable = false;
            claimedRoot.SetActive(false);
            wasAvailableThisSession = false;
        }
    }


    private void Claim()
    {
        if (isClaimed)
            return;

        if (rewardReceiver.CanReceiveGift(giftData))
        {
            rewardReceiver.ReceiveGift(giftData);
            isClaimed = true;

            // Сбрасываем флаг, чтобы Refresh сразу отобразил состояние "Получено"
            wasClaimedThisSession = false;

            Refresh(availableAtTime);
            onClaimed?.Invoke(this);
        }
    }

    private string FormatTime(float totalSeconds)
    {
        int seconds = Mathf.FloorToInt(totalSeconds) % 60;
        int minutes = Mathf.FloorToInt(totalSeconds / 60f) % 60;
        int hours = Mathf.FloorToInt(totalSeconds / 3600f);

        if (hours > 0)
            return $"{hours:00}:{minutes:00}:{seconds:00}";

        return $"{minutes:00}:{seconds:00}";
    }

    private void ApplyTitleGradient(string title, Color gradientColor1, Color gradientColor2)
    {
        titleText.text = CreateGradientText(title, gradientColor1, gradientColor2);
    }

    private string CreateGradientText(string text, Color color1, Color color2)
    {
        if (text.Length == 0)
            return "";

        string result = "";
        float totalLength = text.Length - 1;

        for (int i = 0; i < text.Length; i++)
        {
            float ratio = totalLength > 0 ? i / totalLength : 0;
            Color mixedColor = new Color(
                color1.r + (color2.r - color1.r) * ratio,
                color1.g + (color2.g - color1.g) * ratio,
                color1.b + (color2.b - color1.b) * ratio,
                1f
            );

            result += $"<color=#{mixedColor.HexValue()}>{text[i]}</color>";
        }

        return result;
    }
}

public static class ColorExtensions
{
    public static string HexValue(this Color color)
    {
        return $"{((byte)(color.r * 255)):X2}{((byte)(color.g * 255)):X2}{((byte)(color.b * 255)):X2}";
    }
}