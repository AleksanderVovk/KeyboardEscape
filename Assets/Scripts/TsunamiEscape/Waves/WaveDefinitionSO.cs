using UnityEngine;

[CreateAssetMenu(fileName = "WaveDefinition", menuName = "GameData/Plays/TsunamiEscape/Wave Definition")]
public class WaveDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public string waveId = "SuperSlow";

    public LanguageDirectory waveName;
    public Sprite waveIcon;

    [Header("Prefab")]
    public GameObject wavePrefab;

    [Header("Movement")]
    [Min(0f)] public float moveSpeed = 85f;

    [Header("Spawning")]
    [Min(0f)] public float weight = 1f;          // шанс в выборе (вес)
    [Min(0f)] public float extraCooldown = 0f;    // доп. пауза после этой волны (если нужно)
    public bool spawnNormally = true;             // обычная/ивентовая (для фильтра)
}
