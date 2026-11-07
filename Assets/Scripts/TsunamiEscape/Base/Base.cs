using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using YG;

[Serializable]
public class BaseLevelConfig
{
    public int level;                         // 1,2,3,...
    public long price;
    public GameObject[] objectsToEnable;      // что включается на этом уровне (декор, BrainrotPoint и т.п.)
}

public class Base : MonoBehaviour
{
    public Player owner;

    [Header("Brainrot Points")]
    public BrainrotPoint[] brainrotPoints;
    public Transform spawnPoint;

    [Header("Base Levels")]
    [Tooltip("Конфиг уровней базы по порядку: 1,2,3...")]
    public BaseLevelConfig[] baseLevels;

    [Header("Auto-save")]
    [SerializeField] private float autoSaveInterval = 10f;
    private Coroutine _autoSaveLoop;

    public Collider primaryBounds;
    public Collider secondaryBounds;

    [SerializeField] private TMP_Text _priceUpgrade;
    [SerializeField] private TMP_Text _currentLevel;

    [Header("NPC brainrots spawn")]
    [SerializeField] private bool spawnBrainrotsForNPC = true;
    [SerializeField] private int npcSpawnMin = 3;
    [SerializeField] private int npcSpawnMax = 7; // включительно

    [SerializeField] private GameObject _pointBaseInfo;
    [SerializeField] private TMP_Text _rebirthMultiplierInfo;
    [SerializeField] private TMP_Text _earchPerSecond;

    [SerializeField] private GameObject _avatar;

    private bool _botFullBaseHandled = false;

    private int randomLevelNPC;

    // ---------------- ИНИЦИАЛИЗАЦИЯ ----------------

    public void Initialization(Player owner)
    {
        this.owner = owner;

        int maxLevel = GetMaxConfigLevel();

        int currentLevel;
        if (owner.thisRealPlayer)
            currentLevel = Mathf.Max(0, YG2.saves.playerData.playerBase.baseLevel);
        else
        {
            randomLevelNPC = UnityEngine.Random.Range(0, maxLevel + 1);
            currentLevel = randomLevelNPC;
        }

        // Фаза 1: база (уровень/объекты)
        ApplyBaseLevel(currentLevel);

        // Фаза 2: поинты + брейнроты (только после ApplyBaseLevel)
        InitializationBrainrotPointsOpenedOnly();

        if (!owner.thisRealPlayer && spawnBrainrotsForNPC)
            SpawnRandomBrainrotsForNPC(); // тоже будет учитывать "открытость"
        else if (owner.thisRealPlayer)
            StartAutoSave();

        _avatar.SetActive(owner.thisRealPlayer);

        RefreshInfoDisplay();
    }

    public void RefreshInfoDisplay()
    {
        if (owner.thisRealPlayer)
        {
            _pointBaseInfo.SetActive(true);

            _rebirthMultiplierInfo.text = UTF.ReadLanguageDirectory(LangList.instance.rebirths) + " " + YG2.saves.playerData.rebirthLevel + " (" + RebirthManager.instance.CurrentMultiplier + "x)";

            long allCashPerSecond = GetAllCashPerSecond();

            _earchPerSecond.text = "$" + UTF.AbbreviateNumber(allCashPerSecond) + "/" + UTF.ReadLanguageDirectory(LangList.instance.second);
        }
    }

    private long GetAllCashPerSecond()
    {
        long allCash = 0;

        for (int i = 0; i < brainrotPoints.Length; i++)
        {
            allCash += brainrotPoints[i].GetEarchPerSecond();
        }

        return allCash;
    }

    public bool HasFreeBrainrotPoint()
    {
        if (brainrotPoints == null) return false;

        foreach (BrainrotPoint point in brainrotPoints)
        {
            if (point == null) continue;
            if (!point.gameObject.activeInHierarchy) continue;
            if (point.brainrotInWorld == null) return true;
        }

        return false;
    }

    private void InitializationBrainrotPointsOpenedOnly()
    {
        if (brainrotPoints == null) return;

        for (int i = 0; i < brainrotPoints.Length; i++)
        {
            var p = brainrotPoints[i];
            if (p == null) continue;

            // 1) Всегда прокидываем owner (даже если point выключен)
            p.Initialization(owner);

            // 2) Но загрузку сейва делаем только для открытых поинтов
            if (!p.gameObject.activeInHierarchy) // [web:289]
                continue;

            if (owner.thisRealPlayer)
            {
                p.InitializationSave(YG2.saves.playerData.playerBase.brainrotPoints[i]);
            }
        }
    }

    private void InitializationBrainrotPoints()
    {
        for (int i = 0; i < brainrotPoints.Length; i++)
        {
            brainrotPoints[i].Initialization(owner);

            if (owner.thisRealPlayer)
            {
                brainrotPoints[i].InitializationSave(
                    YG2.saves.playerData.playerBase.brainrotPoints[i]
                );
            }
        }
    }

    private void SpawnRandomBrainrotsForNPC()
    {
        if (brainrotPoints == null || brainrotPoints.Length == 0) return;

        int freeCount = 0;
        for (int i = 0; i < brainrotPoints.Length; i++)
            if (brainrotPoints[i] != null && brainrotPoints[i].brainrotInWorld == null)
                freeCount++;

        if (freeCount == 0) return;

        int want = UnityEngine.Random.Range(npcSpawnMin, npcSpawnMax + 1); // 3..7 [web:263]
        int toSpawn = Mathf.Min(want, freeCount);

        for (int i = 0; i < toSpawn; i++)
        {
            BrainrotPoint point = FindFreePoint();
            if (point == null) break;

            BrainrotRare rare = GetRandomRare_RareToCosmic();
            Brainrot brainrot = BrainrotManager.GetRandomBrainrotFromRarity(rare);
            if (brainrot == null)
            {
                // если для этой редкости нет ассетов — просто пропускаем итерацию
                continue;
            }

            BrainrotData data = BrainrotManager.GenerateBrainrotData(brainrot);
            if (data == null) continue;

            point.SetBrainrot(data, true);
        }
    }

    private BrainrotRare GetRandomRare_RareToCosmic()
    {
        int min = (int)BrainrotRare.Rare;
        int maxExclusive = (int)BrainrotRare.Cosmic + 1;
        int v = UnityEngine.Random.Range(min, maxExclusive); // верх не включается [web:263]
        return (BrainrotRare)v;
    }


    // ---------------- УРОВНИ БАЗЫ ----------------

    /// <summary>
    /// Применить визуал/поинты для указанного уровня:
    /// включает все BaseLevelConfig с level <= currentLevel.
    /// </summary>
    private void ApplyBaseLevel(int currentLevel)
    {
        // Сначала выключим всё, потом включим нужное
        if (baseLevels != null)
        {
            foreach (var cfg in baseLevels)
            {
                if (cfg == null || cfg.objectsToEnable == null) continue;

                foreach (var go in cfg.objectsToEnable)
                {
                    if (go != null)
                        go.SetActive(false);
                }
            }

            foreach (var cfg in baseLevels)
            {
                if (cfg == null || cfg.objectsToEnable == null) continue;
                if (cfg.level > currentLevel) continue;

                foreach (var go in cfg.objectsToEnable)
                {
                    if (go != null)
                        go.SetActive(true);
                }
            }
        }

        // Сохраняем уровень в сейв (на случай, если мы меняем в редакторе и хотим зафиксировать)
        if (owner != null && owner.thisRealPlayer)
            YG2.saves.playerData.playerBase.baseLevel = currentLevel;

        UpdateUpgradeDisplay();
    }

    /// <summary>
    /// Публичный метод «улучшить базу». Можно вызывать из магазина / кнопки.
    /// </summary>
    public void UpgradeBase()
    {
        if (!owner.thisRealPlayer) return;

        int currentLevel = Mathf.Max(0, YG2.saves.playerData.playerBase.baseLevel);
        int maxLevel = GetMaxConfigLevel();

        if (currentLevel >= maxLevel)
        {
            Debug.Log("[Base] UpgradeBase: already at max level");
            return;
        }

        int nextLevel = currentLevel + 1;

        if (ValueSystem.SubstractionBalance(GetPriceForLevel(nextLevel), ValueType.Coin))
        {
            Debug.Log($"[Base] UpgradeBase: new level = {nextLevel}");

            ApplyBaseLevel(nextLevel);
            SaveAllPoints();

            UpdateUpgradeDisplay();
        }
    }

    private void UpdateUpgradeDisplay()
    {
        if (_currentLevel == null || _priceUpgrade == null) return;

        int maxLevel = GetMaxConfigLevel();

        int currentLevel = owner != null && owner.thisRealPlayer
            ? YG2.saves.playerData.playerBase.baseLevel
            : randomLevelNPC;

        // Если уже максимум — выключаем оба текста
        if (currentLevel >= maxLevel)
        {
            _currentLevel.gameObject.SetActive(false);   // [web:249]
            _priceUpgrade.gameObject.SetActive(false);   // [web:249]
            return;
        }

        // Иначе включаем и обновляем
        _currentLevel.gameObject.SetActive(true);
        _priceUpgrade.gameObject.SetActive(true);

        // Уровень (тут можно оставить baseLevels.Length, но правильнее maxLevel)
        _currentLevel.text = currentLevel + "/" + maxLevel;

        // Цена следующего уровня
        int nextLevel = currentLevel + 1;
        long nextPrice = GetPriceForLevel(nextLevel);
        _priceUpgrade.text = "$" + UTF.AbbreviateNumber(nextPrice); // если хочешь формат (1.2K/1.2M) — скажи
    }

    private long GetPriceForLevel(int level)
    {
        if (baseLevels == null) return 0;

        for (int i = 0; i < baseLevels.Length; i++)
        {
            var cfg = baseLevels[i];
            if (cfg != null && cfg.level == level)
                return cfg.price;
        }
        return 0;
    }


    private int GetMaxConfigLevel()
    {
        int max = 0;
        if (baseLevels != null)
        {
            foreach (var cfg in baseLevels)
            {
                if (cfg != null && cfg.level > max)
                    max = cfg.level;
            }
        }
        return max;
    }

    // ---------------- ЛАКИ-БЛОК ----------------

    public void SpawnLuckyBlockToFreePoint(LuckyBrainrot luckyBrainrot)
    {
        BrainrotPoint freePoint = FindFreePoint();
        if (freePoint == null)
        {
            Debug.Log("[Base] SpawnLuckyBlockToFreePoint: no free point");
            return;
        }

        if (luckyBrainrot == null)
        {
            Debug.LogError("[Base] SpawnLuckyBlockToFreePoint: defaultLuckyBrainrot is NULL");
            return;
        }

        Debug.Log($"[Base] SpawnLuckyBlockToFreePoint: freePoint = {freePoint.name}, lucky id = {luckyBrainrot.id}");

        BrainrotData data = new BrainrotData();
        data.id = luckyBrainrot.id;
        data.mutations = new List<string>();

        freePoint.SetBrainrot(data, true);
    }

    // ---------------- ТРИГГЕР / ПОМЕЩЕНИЕ БРЕЙНРОТОВ ----------------

    /*
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponent<Player>();
        if (player != owner)
            return;

        if (player.tsunamiReference == null || player.tsunamiReference.playerCarry == null)
            return;

        var carry = player.tsunamiReference.playerCarry;
        List<BrainrotInWorld> brainrotInHands = carry.brainrotsInHand;

        if (brainrotInHands == null || brainrotInHands.Count == 0)
            return;

        // важный момент: работаем по копии списка, потому что внутри PlaceBrainrotInPoint
        // ты удаляешь элементы из carry.brainrotsInHand
        var copy = new List<BrainrotInWorld>(brainrotInHands);

        for (int i = 0; i < copy.Count; i++)
        {
            BrainrotPoint brainrotPoint = FindFreePoint();
            if (brainrotPoint == null)
            {
                // свободных поинтов больше нет — прерываемся
                break;
            }

            var brainrot = copy[i];
            if (brainrot == null)
                continue;

            PlaceBrainrotInPoint(brainrotPoint, brainrot);
        }

        // после того как всё разложили, можно обновить доход/сейв
        RefreshInfoDisplay();
        SaveAllPoints();
    }
    */

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();

            if (player == owner && player.thisRealPlayer)
            {

                if (player.tsunamiReference == null || player.tsunamiReference.playerCarry == null)
                    return;

                var carry = player.tsunamiReference.playerCarry;
                List<BrainrotInWorld> brainrotInHands = carry.brainrotsInHand;

                if (brainrotInHands == null || brainrotInHands.Count == 0)
                    return;

                var copy = new List<BrainrotInWorld>(brainrotInHands);

                for (int i = 0; i < copy.Count; i++)
                {
                    BrainrotPoint brainrotPoint = FindFreePoint();
                    if (brainrotPoint == null)
                    {
                        // свободных поинтов больше нет — прерываемся
                        break;
                    }

                    var brainrot = copy[i];
                    if (brainrot == null)
                        continue;

                    PlaceBrainrotInPoint(brainrotPoint, brainrot);
                }

                // после того как всё разложили, можно обновить доход/сейв
                RefreshInfoDisplay();
                SaveAllPoints();
            }
            
            if (player == owner && !player.thisRealPlayer)
            {
                Debug.Log("NPC Carrier Set #1");
                NPCBrainrotCarrier npcCarrier = other.GetComponent<NPCBrainrotCarrier>();
                if (npcCarrier != null && npcCarrier.brainrotInHand != null)
                {
                    Debug.Log("NPC Carrier Set #2");
                    if (TryPlaceBrainrotFromNPC(npcCarrier))
                    {
                        Debug.Log("NPC Carrier Set #3");
                        CheckBotBaseFullAndHandle();
                    }
                }
            }
        }

    }

    private void CheckBotBaseFullAndHandle()
    {
        if (owner == null) return;
        if (owner.thisRealPlayer) return;
        if (_botFullBaseHandled) return;
        if (brainrotPoints == null || brainrotPoints.Length == 0) return;

        foreach (var p in brainrotPoints)
        {
            if (p == null) continue;
            if (!p.gameObject.activeInHierarchy) continue;

            if (p.brainrotInWorld == null)
            {
                return;
            }
        }

        _botFullBaseHandled = true;
        StartCoroutine(BotTeleportAndClearRoutine());
    }

    private IEnumerator BotTeleportAndClearRoutine()
    {
        yield return new WaitForSeconds(30f);

        if (owner == null) yield break;

        if (spawnPoint != null)
        {
            owner.TeleportToSpawn();
        }

        ClearAllBrainrotsForBot();
        _botFullBaseHandled = false;
    }

    private void ClearAllBrainrotsForBot()
    {
        if (brainrotPoints == null) return;

        for (int i = 0; i < brainrotPoints.Length; i++)
        {
            var point = brainrotPoints[i];
            if (point == null) continue;

            if (point.brainrotInWorld != null)
            {
                Destroy(point.brainrotInWorld.gameObject);
                point.brainrotInWorld = null;
            }

            if (point.brainrotPointData != null)
            {
                point.brainrotPointData.brainrotData = null;
            }
        }
    }

    public bool TryPlaceBrainrotFromNPC(NPCBrainrotCarrier carrier)
    {
        if (carrier == null || carrier.brainrotInHand == null) return false;

        BrainrotPoint freePoint = FindFreePoint();
        if (freePoint == null) return false;

        freePoint.SetBrainrot(carrier.brainrotInHand.brainrotData, true);

        GameObject.Destroy(carrier.brainrotInHand.gameObject);
        carrier.brainrotInHand = null;

        if (carrier.npcAsPlayer != null && carrier.npcAsPlayer.animator != null)
            carrier.npcAsPlayer.animator.SetBool("isCarry", false);

        return true;
    }

    public BrainrotPoint FindFreePoint()
    {
        if (brainrotPoints == null) return null;

        foreach (BrainrotPoint point in brainrotPoints)
        {
            if (point == null) continue;

            // закрыт/не открыт уровнем базы => не считаем свободным
            if (!point.gameObject.activeInHierarchy) // [web:289]
                continue;

            if (point.brainrotInWorld == null)
                return point;
        }

        if (owner.thisRealPlayer)
        {
            NotificationManager.ShowNotification(UTF.ReadLanguageDirectory(LangList.instance.noFreeBrainrotPoint), NotificationType.Error);
        }

        return null;
    }


    public void PlaceBrainrotInPoint(BrainrotPoint brainrotPoint, BrainrotInWorld brainrotInWorld)
    {
        if (brainrotPoint == null || brainrotInWorld == null)
            return;

        if (owner.thisRealPlayer)
        {
            IndexManager.instance.RegisterDiscovered(
                brainrotInWorld.brainrot.id,
                brainrotInWorld.brainrotData.mutations
            );

            // вызывать финиш туториала только один раз, например,
            // когда туториал ещё активен
            if (TutorialManager.instance != null && TutorialManager.instance.IsActive)
                TutorialManager.instance.FinishTutorialFromBase();
        }

        brainrotPoint.SetBrainrot(brainrotInWorld.brainrotData, true);
        owner.tsunamiReference.playerCarry.DestroyBrainrotInHand(brainrotInWorld);
    }


    public bool PlaceBrainrot(BrainrotBase brainrot)
    {
        if (brainrot == null)
        {
            Debug.LogWarning("[Base] PlaceBrainrot: brainrot is NULL");
            return false;
        }

        BrainrotPoint freePoint = FindFreePoint();
        if (freePoint == null)
        {
            Debug.Log("[Base] PlaceBrainrot: no free point");
            return false;
        }

        BrainrotData data = CreateBrainrotDataFromAsset(brainrot);
        if (data == null)
        {
            Debug.LogWarning("[Base] PlaceBrainrot: failed to create BrainrotData");
            return false;
        }

        // Ставим в поинт (анимации/спавн — внутри SetBrainrot)
        freePoint.SetBrainrot(data, true);

        // Если это наш игрок — фиксируем сейв поинтов
        if (owner != null && owner.thisRealPlayer)
            SaveAllPoints();

        RefreshInfoDisplay();

        return true;
    }

    private BrainrotData CreateBrainrotDataFromAsset(BrainrotBase brainrot)
    {
        // Если у тебя есть генерация мутаций/статов — добавишь сюда.
        return new BrainrotData
        {
            id = brainrot.id,
            mutations = new List<string>()
        };
    }

    // ---------------- АВТО-СЕЙВ ----------------

    private void StartAutoSave()
    {
        if (_autoSaveLoop != null) return;
        _autoSaveLoop = StartCoroutine(AutoSaveLoop());
    }

    private void StopAutoSave()
    {
        if (_autoSaveLoop == null) return;
        StopCoroutine(_autoSaveLoop);
        _autoSaveLoop = null;
    }

    private IEnumerator AutoSaveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveInterval);
            SaveAllPoints();
        }
    }

    private void SaveAllPoints()
    {
        if (!owner.thisRealPlayer) return;

        // сохраняем поинты
        for (int i = 0; i < brainrotPoints.Length; i++)
        {
            YG2.saves.playerData.playerBase.brainrotPoints[i] = brainrotPoints[i].brainrotPointData;
        }

        // уровень базы уже сидит в YG2.saves.playerData.playerBase.baseLevel,
        // он сохранится вместе с остальными данными
        YG2.SaveProgress();
    }

    // ---------------- ПРОВЕРКИ НАЛИЧИЯ БРЕЙНРОТОВ ----------------

    public bool HasBrainrotOnBase(string brainrotId)
    {
        if (string.IsNullOrEmpty(brainrotId)) return false;
        if (brainrotPoints == null) return false;

        foreach (var p in brainrotPoints)
        {
            if (p == null) continue;
            if (p.brainrotPointData?.brainrotData == null) continue;

            if (p.brainrotPointData.brainrotData.id == brainrotId)
                return true;
        }

        return false;
    }

    public bool HasAllBrainrotsOnBase(IEnumerable<string> brainrotIds)
    {
        foreach (var id in brainrotIds)
            if (!HasBrainrotOnBase(id))
                return false;

        return true;
    }

    private void OnDisable()
    {
        StopAutoSave();

        if (owner != null && owner.thisRealPlayer)
        {
            SaveAllPoints();
        }
    }
}
