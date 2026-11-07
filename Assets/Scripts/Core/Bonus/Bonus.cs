using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Bonus", menuName = "GameData/Bonuses/New Bonus", order = 4)]
public class Bonus : ScriptableObject
{
    public Sprite icon;

    public BonusType bonusType;
    public float timeAction;

    public LanguageDirectory bonusName;
}
