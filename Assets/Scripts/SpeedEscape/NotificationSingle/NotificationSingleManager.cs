using UnityEngine;

public class NotificationSingleManager : MonoBehaviour
{
    public static NotificationSingleManager Instance { get; private set; }

    [Header("Settings")]
    public NotificationSingleView notificationPrefab;  // Префаб уведомления
    public Transform spawnPoint;  // Где спавнить (например, в вертикальном Layout Group)

    [Header("Animation")]
    public float notificationSpacing = 10f;  // Расстояние между уведомлениями
    public float maxNotifications = 5;  // Максимум одновременно видимых

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Показать уведомление
    /// </summary>
    public static void ShowNotification(string text, Color textColor, float duration = 2f)
    {
        if (Instance == null)
        {
            Debug.LogError("[Notification] NotificationManager not found in scene!");
            return;
        }

        if (Instance.notificationPrefab == null)
        {
            Debug.LogError("[Notification] Notification prefab not assigned!");
            return;
        }

        // Спавним уведомление
        var notification = Instantiate(
            Instance.notificationPrefab,
            Instance.spawnPoint
        );

        // Устанавливаем позицию (сдвигаем вниз для каждого нового)
        var rectTransform = notification.GetComponent<RectTransform>();
        float yOffset = -Instance.GetNotificationCount() * Instance.notificationSpacing;
        rectTransform.anchoredPosition = new Vector2(0, yOffset);

        // Инициализируем
        notification.showDuration = duration;
        notification.Initialize(text, textColor);
    }

    /// <summary>
    /// Показать уведомление с цветом по типу
    /// </summary>
    private int GetNotificationCount()
    {
        if (spawnPoint == null)
            return 0;

        int count = 0;
        foreach (Transform child in spawnPoint)
        {
            if (child.GetComponent<NotificationSingleView>() != null)
                count++;
        }

        return count;
    }
}