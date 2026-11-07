using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveDatabase", menuName = "GameData/Plays/TsunamiEscape/Wave Database")]
public class WaveDatabaseSO : ScriptableObject
{
    [SerializeField] private bool autoLoadFromResources = true;

    [SerializeField] private string resourcesFolder = "GameData/Waves";

    public List<WaveDefinitionSO> waves = new();

    public void EnsureLoaded()
    {
        if (!autoLoadFromResources) return;
        if (waves != null && waves.Count > 0) return;

        var loaded = Resources.LoadAll<WaveDefinitionSO>(resourcesFolder);
        waves = loaded != null ? new List<WaveDefinitionSO>(loaded) : new List<WaveDefinitionSO>();
    }
}
