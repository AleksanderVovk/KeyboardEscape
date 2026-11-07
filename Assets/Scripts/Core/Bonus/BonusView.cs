using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BonusView : MonoBehaviour
{
    public Bonus bonus;
    public float remainingTime;

    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _fillImage;

    public void SetData(Bonus bonus)
    {
        this.bonus = bonus;

        remainingTime = bonus.timeAction;

        _iconImage.sprite = bonus.icon;
    }

    public void UpdateView()
    {
        float fillAmount = 1 - (remainingTime / bonus.timeAction);
        _fillImage.fillAmount = fillAmount;
    }
}
