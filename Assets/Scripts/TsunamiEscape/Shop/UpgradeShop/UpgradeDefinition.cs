using System;
using UnityEngine;
using YG;

public enum UpgradeEffectType { AddSpeed, AddCarrySlot }

[Serializable]
public class UpgradeDefinition
{
    public UpgradeId id;
    public Sprite icon;

    public UpgradeEffectType effectType;

    [Header("Fake UI value")]
    public int displayAmount = 1;   // что показываем в тексте: 1 / 5 / 10

    [Header("Real effect value")]
    public float realAmount = 0.1f; // что реально прибавл€ем: 0.1 / 0.5 / 1.0

    public ValueType currency;
    public long startPrice;
    public float priceMultiplier = 1.35f;
    public int maxLevel = 999999;
    public long startPriceHard = 10;
}
