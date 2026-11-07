using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;

public class IndexManager : MonoBehaviour
{
    public static IndexManager instance;

    [Header("Load paths (Resources)")]
    [SerializeField] private string brainrotResourcesPath = "GameData/Brainrots";

    [Header("UI")]
    [SerializeField] private Transform itemsParent;        // куда спавнить иконки
    [SerializeField] private IndexItemView itemPrefab;

    private readonly List<Brainrot> _allBrainrots = new();
    private readonly List<Mutation> _allMutations = new();

    private HashSet<string> _discovered; // кеш

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private void Start()
    {
        LoadData();
        BuildDiscoveredCache();
    }

    private void LoadData()
    {
        _allBrainrots.Clear();
        _allMutations.Clear();

        _allBrainrots.AddRange(Resources.LoadAll<Brainrot>(brainrotResourcesPath));
        _allMutations.AddRange(MutationManager.instance != null
            ? MutationManager.instance.GetAll()
            : Resources.LoadAll<Mutation>("GameData/Mutations"));
    }

    private void BuildDiscoveredCache()
    {
        _discovered = new HashSet<string>(YG2.saves.playerData.discoveredIndexEntries);
    }

    public void RegisterDiscovered(string brainrotId, List<string> mutationIds)
    {
        if (mutationIds == null || mutationIds.Count == 0)
            mutationIds = new List<string> { "none" };

        bool changed = false;

        for (int i = 0; i < mutationIds.Count; i++)
        {
            string key = MakeKey(brainrotId, mutationIds[i]);
            if (_discovered.Add(key))
            {
                YG2.saves.playerData.discoveredIndexEntries.Add(key);
                changed = true;
            }
        }

        if (changed)
            YG2.SaveProgress();
    }

    public static string MakeKey(string brainrotId, string mutationId) => $"{brainrotId}|{mutationId}";

    public void ShowCategoryNone()
    {
        // показываем все брейнроты, отсортированные по редкости
        var list = _allBrainrots.Where(b => b != null).OrderBy(b => b.brainrotRare).ToList();
        BuildItems(list, mutationId: "none");
    }

    public void ShowCategoryMutation(string mutationId)
    {
        var list = _allBrainrots.Where(b => b != null).OrderBy(b => b.brainrotRare).ToList();
        BuildItems(list, mutationId);
    }

    private void BuildItems(List<Brainrot> list, string mutationId)
    {
        for (int i = itemsParent.childCount - 1; i >= 0; i--)
            Destroy(itemsParent.GetChild(i).gameObject);

        Mutation mut = (mutationId == "none") ? null : MutationManager.instance?.GetById(mutationId);

        for (int i = 0; i < list.Count; i++)
        {
            var b = list[i];
            var view = Instantiate(itemPrefab, itemsParent);

            bool discovered = _discovered.Contains(MakeKey(b.id, mutationId));
            view.Bind(b, mut, discovered);
        }
    }
}
