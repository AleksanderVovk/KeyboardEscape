using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum PlaytimeGiftRewardType
{
    Money,
    Diamonds,
    Brainrot,
    SpeedBonus,
    Speed,
    Wins
}

[Serializable]
public class PlaytimeGiftData
{
    public int id;
    public string specialId;
    public PlaytimeGiftRewardType rewardType;
    public int amount;
    public Sprite icon;
    public LanguageDirectory title;
    public float requiredSeconds;
    public bool isExtraGift = false;

    [Header("Gradient Colors for Title")]
    public Color gradientColor1 = Color.white;
    public Color gradientColor2 = Color.white;
}

public class PlaytimeGiftWindow : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private List<PlaytimeGiftData> gifts = new();

    [Header("UI")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private Transform rootExtraGiftView;
    [SerializeField] private PlaytimeGiftView giftViewPrefab;
    [SerializeField] private PlaytimeGiftView extraGiftViewPrefab;
    [SerializeField] private PlaytimeGiftRewardReceiver rewardReceiver;

    // Новое поле для текста с таймером/наградой
    [SerializeField] private TMP_Text nextRewardTimerText;

    private readonly List<PlaytimeGiftView> spawnedViews = new();
    private readonly List<PlaytimeGiftView> spawnedExtraViews = new();
    private float sessionPlaytime;
    private float lastUpdateTime;

    private void OnEnable()
    {
        BuildViews();
        lastUpdateTime = Time.time;
        UpdateNextRewardTimer();
    }

    private void Update()
    {
        float delta = Time.time - lastUpdateTime;
        lastUpdateTime = Time.time;
        sessionPlaytime += delta;

        for (int i = 0; i < spawnedViews.Count; i++)
            spawnedViews[i].Refresh(sessionPlaytime);

        for (int i = 0; i < spawnedExtraViews.Count; i++)
            spawnedExtraViews[i].Refresh(sessionPlaytime);

        UpdateNextRewardTimer();
    }

    // Обновляет текст таймера до следующей не забранной награды
    private void UpdateNextRewardTimer()
    {
        if (nextRewardTimerText == null)
            return;

        PlaytimeGiftData nextUnclaimedGift = null;

        for (int i = 0; i < gifts.Count; i++)
        {
            var gift = gifts[i];
            if (gift.isExtraGift)
                continue;

            bool isClaimed = false;
            for (int j = 0; j < spawnedViews.Count; j++)
            {
                if (spawnedViews[j].giftData.id == gift.id && spawnedViews[j].isClaimed)
                {
                    isClaimed = true;
                    break;
                }
            }

            if (!isClaimed)
            {
                nextUnclaimedGift = gift;
                break;
            }
        }

        if (nextUnclaimedGift == null)
        {
            nextRewardTimerText.text = "";
            return;
        }

        float remaining = Mathf.Max(0f, nextUnclaimedGift.requiredSeconds - sessionPlaytime);

        if (remaining <= 0f)
        {
            LanguageDirectory claimTextLang = new LanguageDirectory("Забрать награду", "Claim Reward", "Rewardi Al");
            string claimText = UTF.ReadLanguageDirectory(claimTextLang);
            nextRewardTimerText.text = $"<color=#00FF00>{claimText}</color>";
        }
        else
        {
            string timerText = FormatTime(remaining);
            nextRewardTimerText.text = $"<color=#FF0000>{timerText}</color>";
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

    private void BuildViews()
    {
        for (int i = 0; i < spawnedViews.Count; i++)
            Destroy(spawnedViews[i].gameObject);
        spawnedViews.Clear();

        for (int i = 0; i < spawnedExtraViews.Count; i++)
            Destroy(spawnedExtraViews[i].gameObject);
        spawnedExtraViews.Clear();

        for (int i = 0; i < gifts.Count; i++)
        {
            var gift = gifts[i];

            if (gift.isExtraGift)
            {
                if (extraGiftViewPrefab == null)
                {
                    Debug.LogWarning("extraGiftViewPrefab не назначен! Используем обычный префаб.");
                    var view = Instantiate(giftViewPrefab, contentRoot);
                    view.Initialize(gift, sessionPlaytime, rewardReceiver, OnGiftClaimed);
                    spawnedViews.Add(view);
                }
                else
                {
                    var view = Instantiate(extraGiftViewPrefab, rootExtraGiftView);
                    view.Initialize(gift, sessionPlaytime, rewardReceiver, OnGiftClaimed);
                    spawnedExtraViews.Add(view);
                }
            }
            else
            {
                var view = Instantiate(giftViewPrefab, contentRoot);
                view.Initialize(gift, sessionPlaytime, rewardReceiver, OnGiftClaimed);
                spawnedViews.Add(view);
            }
        }
    }

    private void OnGiftClaimed(PlaytimeGiftView view)
    {
        // После получения награды обновим таймер до следующей
        UpdateNextRewardTimer();

        RewardNotification.Show(view.giftData.icon, UTF.ReadLanguageDirectory(view.giftData.title), "", view.giftData.gradientColor1, view.giftData.gradientColor2);
        UISound.manager.PlayWin(false);
    }
}