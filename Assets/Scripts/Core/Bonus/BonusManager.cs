using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum BonusType
{
    DoubleJump,
    SpeedX2,
    MoneyX2
}

public class BonusManager : MonoBehaviour
{
    public static BonusManager instance;

    public List<BonusView> existingBonuses = new List<BonusView>();

    public Player realPlayer;

    [SerializeField] private Transform _storage;
    [SerializeField] private BonusView _bonusViewPrefab;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        StartCoroutine(RepeatEveryNSeconds(1));
    }

    public void AddBouns(Bonus bonus)
    {
        foreach (BonusView existingBonus in existingBonuses)
        {
            if (bonus == existingBonus.bonus)
            {
                existingBonus.remainingTime = bonus.timeAction;
                return;
            }
        }

        BonusView newBonusView = Instantiate(_bonusViewPrefab, _storage);
        newBonusView.SetData(bonus);

        // Добавление нового бонуса
        existingBonuses.Add(newBonusView);
        BonusHolder(bonus, true);

        LanguageDirectory messageNotification = new LanguageDirectory
              (
              "Вы получили " + UTF.ReadLanguageDirectory(bonus.bonusName),
              "You received " + UTF.ReadLanguageDirectory(bonus.bonusName),
              "Sen aldin " + UTF.ReadLanguageDirectory(bonus.bonusName)
              );

        NotificationManager.ShowNotification(bonus.icon, UTF.ReadLanguageDirectory(messageNotification), NotificationType.Success);
    }

    public void BonusHolder(Bonus bonus, bool active)
    {
        switch (bonus.bonusType)
        {
            case BonusType.DoubleJump:

                //if (active)
                    //realPlayer.playerController.SetJumpCount(2);
                //else
                    //realPlayer.playerController.SetJumpCount(1);

                break;

            case BonusType.SpeedX2:

                //realPlayer.thisX2Speed = active;

                //realPlayer.UpdateDisplayAttribute(false);

                break;

            case BonusType.MoneyX2:

                ValueSystem.instance.thisX2Money = active;

                break;
        }

        if (!active)
        {
            LanguageDirectory messageNotification = new LanguageDirectory
                (
                UTF.ReadLanguageDirectory(bonus.bonusName) + " закончился",
                UTF.ReadLanguageDirectory(bonus.bonusName) + " ended",
                UTF.ReadLanguageDirectory(bonus.bonusName) + " bitiyordu"
                );

            NotificationManager.ShowNotification(bonus.icon, UTF.ReadLanguageDirectory(messageNotification), NotificationType.Error);
        }
    }

    private IEnumerator RepeatEveryNSeconds(float n)
    {
        while (true) // Бесконечный цикл
        {
            if (existingBonuses.Count > 0)
            {
                for (int i = existingBonuses.Count - 1; i >= 0; i--)
                {
                    BonusView existingBonus = existingBonuses[i];
                    existingBonus.remainingTime -= 1;

                    existingBonus.UpdateView();

                    if (existingBonus.remainingTime <= 0)
                    {
                        BonusHolder(existingBonus.bonus, false);

                        existingBonuses.RemoveAt(i); // Удаляем элемент по индексу

                        if (existingBonus != null)
                            Destroy(existingBonus.gameObject);
                    }
                }
            }

            yield return new WaitForSeconds(n); // Ждем N секунд перед следующей итерацией
        }
    }
}
