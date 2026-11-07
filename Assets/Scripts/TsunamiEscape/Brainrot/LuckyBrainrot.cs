using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LuckyBrainrot", menuName = "GameData/Plays/TsunamiEscape/LuckyBrainrot", order = 2)]
public class LuckyBrainrot : BrainrotBase
{
    [System.Serializable]
    public class Entry
    {
        public Brainrot brainrot;   // обычный брейнрот, который может выпасть
        public float weight = 1f;   // шанс
    }

    public List<Entry> entries = new List<Entry>();

    public override bool IsLucky => true;

    public Brainrot GetRandomBrainrot()
    {
        if (entries == null || entries.Count == 0)
            return null;

        float total = 0f;
        for (int i = 0; i < entries.Count; i++)
            total += Mathf.Max(0f, entries[i].weight);

        float r = Random.value * total;
        float sum = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            float w = Mathf.Max(0f, entries[i].weight);
            if (w <= 0f) continue;

            sum += w;
            if (r <= sum)
                return entries[i].brainrot;
        }

        return entries[entries.Count - 1].brainrot;
    }
}
