using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum BrainrotRare
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Mythical,
    Cosmic,
    Secret,
    Heavenly,
    Divine
}

[System.Serializable]
public enum BrainrotState
{
    InWorld,
    InHand,
    InBase
}

[System.Serializable]
public class BrainrotData
{
    public string id;
    public List<string> mutations = new List<string>();
}

[System.Serializable]
public class BrainrotPointData
{
    public BrainrotData brainrotData;
    public long balance;
}

public class BrainrotManager : MonoBehaviour
{
    public static BrainrotManager instance;

    // ВСЕ брейнроты (и обычные, и лаки) через базовый тип
    public BrainrotBase[] allBrainrots;

    [SerializeField] private LanguageDirectory[] _brainrotRaresName;
    [SerializeField] private Color[] _brainrotRaresColors;

    [SerializeField] private BrainrotChunk[] _defaultChunks;

    // кеш для быстрого поиска по id
    private Dictionary<string, BrainrotBase> _brainrotById;

    // кеш Lucky блоков по редкости
    private Dictionary<BrainrotRare, List<LuckyBrainrot>> _luckyByRarity;

    private void Awake()
    {
        instance = this;
    }

    public void Initialization()
    {
        return;

        Debug.Log("[BrainrotManager] Initialization");

        // Грузим ВСЕ ScriptableObject наследники BrainrotBase из папки Resources/GameData/Brainrots
        allBrainrots = Resources.LoadAll<BrainrotBase>("GameData/Brainrots"); // путь относителен Resources[web:55]

        if (allBrainrots == null || allBrainrots.Length == 0)
        {
            Debug.LogWarning("[BrainrotManager] No BrainrotBase assets found in Resources/GameData/Brainrots");
        }
        else
        {
            Debug.Log($"[BrainrotManager] Loaded {allBrainrots.Length} BrainrotBase assets");
        }

        BuildIdCache();
        BuildLuckyCache();

        InitializationPreloadChunk();
    }

    private void BuildIdCache()
    {
        _brainrotById = new Dictionary<string, BrainrotBase>(allBrainrots != null ? allBrainrots.Length : 0);

        if (allBrainrots == null) return;

        for (int i = 0; i < allBrainrots.Length; i++)
        {
            BrainrotBase b = allBrainrots[i];
            if (b == null) continue;

            if (string.IsNullOrWhiteSpace(b.id))
            {
                Debug.LogWarning($"[BrainrotManager] Asset '{b.name}' has empty id");
                continue;
            }

            if (_brainrotById.ContainsKey(b.id))
            {
                Debug.LogWarning($"[BrainrotManager] Duplicate brainrot id: '{b.id}' (asset: {b.name})");
                continue;
            }

            _brainrotById.Add(b.id, b);
        }

        Debug.Log($"[BrainrotManager] Id cache built, count = {_brainrotById.Count}");
    }

    private void BuildLuckyCache()
    {
        _luckyByRarity = new Dictionary<BrainrotRare, List<LuckyBrainrot>>();

        if (allBrainrots == null) return;

        foreach (BrainrotRare rare in System.Enum.GetValues(typeof(BrainrotRare)))
        {
            _luckyByRarity[rare] = new List<LuckyBrainrot>();
        }

        for (int i = 0; i < allBrainrots.Length; i++)
        {
            if (allBrainrots[i] is LuckyBrainrot lucky)
            {
                // LuckyBrainrot имеет свою редкость (берем из базового класса)
                BrainrotRare luckyRare = lucky.brainrotRare;
                if (_luckyByRarity.ContainsKey(luckyRare))
                {
                    _luckyByRarity[luckyRare].Add(lucky);
                }
            }
        }

        Debug.Log("[BrainrotManager] Lucky blocks cache built");
    }

    // Публичный поиск по id (BrainrotBase для всех типов, включая лаки-блоки)
    public static BrainrotBase GetBrainrotById(string id) => instance._GetBrainrotById(id);

    private BrainrotBase _GetBrainrotById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        if (_brainrotById == null || _brainrotById.Count == 0)
            BuildIdCache();

        if (_brainrotById.TryGetValue(id, out var b))
            return b;

        Debug.LogWarning($"[BrainrotManager] BrainrotBase with id '{id}' not found");
        return null;
    }

    private void InitializationPreloadChunk()
    {
        for (int i = 0; i < _defaultChunks.Length; i++)
            _defaultChunks[i].GenerateBrainrot();
    }

    // --------- Рандом по редкости (только обычные Brainrot) ---------
    public static Brainrot GetRandomBrainrotFromRarity(BrainrotRare rare) =>
        instance._GetRandomBrainrotFromRarity(rare);

    private Brainrot _GetRandomBrainrotFromRarity(BrainrotRare rare)
    {
        if (allBrainrots == null || allBrainrots.Length == 0)
            return null;

        List<Brainrot> filtered = new List<Brainrot>();

        for (int i = 0; i < allBrainrots.Length; i++)
        {
            // Берём только обычные Brainrot (не LuckyBrainrot)
            if (allBrainrots[i] is Brainrot b)
            {
                if (b.brainrotRare == rare && b.brainrotPrefab != null)
                    filtered.Add(b);
            }
        }

        if (filtered.Count == 0)
            return null;

        int index = Random.Range(0, filtered.Count);
        return filtered[index];
    }

    // --------- НОВЫЙ: Рандом Lucky блоков по редкости ---------
    public static BrainrotBase GetRandomLuckyBrainrotFromRarity(BrainrotRare rare) =>
        instance._GetRandomLuckyBrainrotFromRarity(rare);

    private BrainrotBase _GetRandomLuckyBrainrotFromRarity(BrainrotRare rare)
    {
        if (_luckyByRarity == null)
            BuildLuckyCache();

        if (!_luckyByRarity.ContainsKey(rare) || _luckyByRarity[rare].Count == 0)
            return null;

        List<LuckyBrainrot> luckyList = _luckyByRarity[rare];
        int index = Random.Range(0, luckyList.Count);
        return luckyList[index];
    }

    // --------- Генерация BrainrotData для обычных брейнротов ---------
    public static BrainrotData GenerateBrainrotData(Brainrot brainrot) =>
        instance._GenerateBrainrotData(brainrot);

    private BrainrotData _GenerateBrainrotData(Brainrot brainrot)
    {
        if (brainrot == null)
            return null;

        BrainrotData brainrotData = new BrainrotData
        {
            id = brainrot.id
            // mutations можно заполнить позже
        };

        return brainrotData;
    }

    // --------- UI: название/цвет редкости ---------
    public static string GetBrainrotRareName(BrainrotRare brainrotRare) =>
        instance._GetBrainrotRareName(brainrotRare);

    private string _GetBrainrotRareName(BrainrotRare brainrotRare)
    {
        return UTF.ReadLanguageDirectory(_brainrotRaresName[(int)brainrotRare]);
    }

    public static Color GetBrainrotRareColor(BrainrotRare brainrotRare) =>
        instance._GetBrainrotRareColor(brainrotRare);

    private Color _GetBrainrotRareColor(BrainrotRare brainrotRare)
    {
        return _brainrotRaresColors[(int)brainrotRare];
    }
}
