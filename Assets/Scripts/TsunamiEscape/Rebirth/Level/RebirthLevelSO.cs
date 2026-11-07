using UnityEngine;

[CreateAssetMenu(fileName = "RebirthLevel", menuName = "GameData/Rebirth/New Level", order = 1)]
public class RebirthLevelSO : ScriptableObject
{
    public int level = 1;

    [Header("Requirement")]
    public int requiredSpeedLevel = 1;

    [Header("Reward")]
    public double rewardMultiplier = 1f; // множитель, который будет установлен/выдан на этом уровне
}
