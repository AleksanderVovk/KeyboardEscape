using System;
using UnityEngine;
using YG;

public class PlayerAttendance : MonoBehaviour
{
    private void Awake()
    {
        UpdateAttendance();
    }

    public static void UpdateAttendance()
    {
        long lastLoginTime = YG2.saves.LastLoginTime;
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Первый запуск – просто записываем now и открываем первый день
        if (lastLoginTime == 0)
        {
            YG2.saves.LastLoginTime = now;
            if (YG2.saves.DaysUnlocked <= 0)
                YG2.saves.DaysUnlocked = 1;   // день 0 открыт
            YG2.SaveProgress();
            Debug.Log("[ATTENDANCE] First login, DaysUnlocked=1");
            return;
        }

        DateTime lastDate = DateTimeOffset.FromUnixTimeSeconds(lastLoginTime).Date;
        DateTime todayDate = DateTimeOffset.FromUnixTimeSeconds(now).Date;

        Debug.Log($"[ATTENDANCE] LastLogin={lastDate:yyyy-MM-dd}, Today={todayDate:yyyy-MM-dd}, DaysUnlocked={YG2.saves.DaysUnlocked}");

        if (lastDate != todayDate)
        {
            // Новый календарный день – увеличиваем DaysUnlocked на 1 (не больше 7)
            YG2.saves.DaysUnlocked = Mathf.Min(YG2.saves.DaysUnlocked + 1, 7);
            YG2.saves.LastLoginTime = now;
            YG2.SaveProgress();

            Debug.Log($"[ATTENDANCE] New day, DaysUnlocked={YG2.saves.DaysUnlocked}");
        }
        else
        {
            Debug.Log("[ATTENDANCE] Same day – no update");
        }
    }
}
