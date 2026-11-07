using System.Collections.Generic;
using UnityEngine;

public class MutationManager : MonoBehaviour
{
    public static MutationManager instance;

    [SerializeField] private string resourcesPath = "GameData/Mutations";

    [Header("Global spawn chance")]
    [Range(0f, 1f)]
    [SerializeField] private float globalMutationChance = 0.10f; // например 10%

    private readonly List<Mutation> _all = new();
    private readonly List<Mutation> _nonEventPool = new();
    private readonly Dictionary<string, Mutation> _byId = new();

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        LoadAllFromResources();
    }

    private void LoadAllFromResources()
    {
        _all.Clear();
        _nonEventPool.Clear();
        _byId.Clear();

        var loaded = Resources.LoadAll<Mutation>(resourcesPath); // [web:12]
        foreach (var m in loaded)
        {
            if (m == null) continue;

            _all.Add(m);
            if (!string.IsNullOrEmpty(m.id))
                _byId[m.id] = m;

            if (!m.isEvent)
                _nonEventPool.Add(m);
        }
    }

    public Mutation GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _byId.TryGetValue(id, out var m) ? m : null;
    }

    public Mutation RollNonEventMutation()
    {
        if (_nonEventPool.Count == 0) return null;

        // 1) Глобальный шанс: чаще всего вернём null
        if (Random.value > globalMutationChance) // Random.value в диапазоне [0..1] [web:112]
            return null;

        // 2) Взвешенный выбор среди мутаций (chance = вес)
        float total = 0f;
        for (int i = 0; i < _nonEventPool.Count; i++)
            total += Mathf.Max(0f, _nonEventPool[i].chance);

        if (total <= 0f) return null;

        float r = Random.value * total;
        float acc = 0f;

        for (int i = 0; i < _nonEventPool.Count; i++)
        {
            var m = _nonEventPool[i];
            acc += Mathf.Max(0f, m.chance);
            if (r <= acc) return m;
        }

        return null;
    }

    public List<Mutation> GetAll() => _all;
}
