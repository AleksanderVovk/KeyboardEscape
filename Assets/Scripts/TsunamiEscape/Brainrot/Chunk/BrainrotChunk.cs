using System.Collections.Generic;
using UnityEngine;

public class BrainrotChunk : MonoBehaviour
{
    public List<BrainrotInWorld> brainrotInChunk = new List<BrainrotInWorld>();

    [SerializeField] private BoxCollider _spawnZone;

    [Header("Rarity Settings")]
    public BrainrotRare rarityA = BrainrotRare.Common;
    public BrainrotRare rarityB = BrainrotRare.Uncommon;

    [Header("Lucky Blocks")]
    public LuckyBrainrot[] luckyBlocks; // ← Твой массив для ручного выбора Lucky блоков
    [Range(0f, 1f)]
    public float luckyBlockChance = 0.15f; // шанс спавна Lucky блока вместо обычного

    [Header("Spawn Settings")]
    public int countToSpawn = 5;
    public LayerMask groundLayer;
    public float raycastDownDistance = 20f;

    [Header("Chunk Activity")]
    [SerializeField] private bool _alwaysActive = false;
    private int _playersInside = 0;

    public bool IsActive => _alwaysActive || _playersInside > 0;

    private int _nextSpawnId = 0;

    private void Awake()
    {
        if (_spawnZone == null)
            _spawnZone = GetComponent<BoxCollider>();
    }

    public void EnsureSpawned()
    {
        CleanupNulls();

        if (!IsActive) return;

        if (_spawnZone == null)
        {
            Debug.LogWarning("[BrainrotChunk] _spawnZone is null", this);
            return;
        }

        int need = countToSpawn - brainrotInChunk.Count;
        if (need <= 0) return;

        SpawnAmount(need);
    }

    // Истёк таймер у InWorld => удаляем из списка и (если чанк активен) восполняем
    public void NotifyBrainrotTimedOut(BrainrotInWorld obj)
    {
        if (obj != null)
            brainrotInChunk.Remove(obj);

        CleanupNulls();

        // Респавн только если чанк активен
        EnsureSpawned();
    }

    private void SpawnAmount(int amount)
    {
        int spawned = 0;
        int safety = 0;
        int maxAttempts = amount * 6;

        while (spawned < amount && safety < maxAttempts)
        {
            safety++;

            // Шанс спавна Lucky блока из твоего массива
            if (Random.value < luckyBlockChance && luckyBlocks != null && luckyBlocks.Length > 0)
            {
                LuckyBrainrot selectedLucky = luckyBlocks[Random.Range(0, luckyBlocks.Length)];
                if (SpawnLuckyBlock(selectedLucky, out BrainrotInWorld spawnedLucky))
                {
                    brainrotInChunk.Add(spawnedLucky);
                    spawned++;
                    continue;
                }
            }

            // Обычный спавн
            BrainrotRare selectedRare = (Random.value < 0.5f) ? rarityA : rarityB;

            Brainrot brainrot = BrainrotManager.GetRandomBrainrotFromRarity(selectedRare);
            if (brainrot == null || brainrot.brainrotPrefab == null)
                continue;

            Vector3 spawnPos = GetRandomPointInBox(_spawnZone);
            if (!TryProjectToGround(spawnPos, out Vector3 groundedPos))
                continue;

            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject go = Instantiate(brainrot.brainrotPrefab, groundedPos, rot);

            if (go.TryGetComponent(out BrainrotInWorld inWorld))
            {
                int id = _nextSpawnId++;
                inWorld.SetSpawnId(id);
                inWorld.SetChunk(this);

                // 1) Генерим data
                BrainrotData data = BrainrotManager.GenerateBrainrotData(brainrot);

                // 2) Роллим мутации и пишем в data
                RollMutationsIntoData(data);

                // 3) Передаём в инстанс (внутри он только визуал применит)
                inWorld.SetBrainrot(brainrot, data);

                brainrotInChunk.Add(inWorld);
                spawned++;
            }
            else
            {
                Destroy(go);
            }
        }

        if (spawned < amount)
            Debug.LogWarning($"[BrainrotChunk] Spawned only {spawned} of {amount}", this);
    }

    private bool SpawnLuckyBlock(LuckyBrainrot luckyBrainrot, out BrainrotInWorld inWorld)
    {
        inWorld = null;

        if (luckyBrainrot == null || luckyBrainrot.brainrotPrefab == null)
            return false;

        Vector3 spawnPos = GetRandomPointInBox(_spawnZone);
        if (!TryProjectToGround(spawnPos, out Vector3 groundedPos))
            return false;

        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        GameObject go = Instantiate(luckyBrainrot.brainrotPrefab, groundedPos, rot);

        if (go.TryGetComponent(out inWorld))
        {
            int id = _nextSpawnId++;
            inWorld.SetSpawnId(id);
            inWorld.SetChunk(this);

            // 1) выбираем награду
            Brainrot reward = luckyBrainrot.GetRandomBrainrot();

            // 2) дата: id = ЛАКИ, rewardId = награда
            BrainrotData data = new BrainrotData();
            data.id = luckyBrainrot.id;

            RollMutationsIntoData(data);

            // 3) В инстанс отдаём ЛАКИ как brainrotBase + data лаки-блока
            inWorld.SetBrainrot(luckyBrainrot, data);

            return true;
        }

        Destroy(go);
        return false;
    }


    [SerializeField] private int mutationRollsPerSpawn = 2; // сколько "попыток" получить мутацию (может дать несколько)
    private void RollMutationsIntoData(BrainrotData data)
    {
        if (data == null) return;
        if (MutationManager.instance == null) return;

        if (data.mutations == null)
            data.mutations = new List<string>();

        for (int i = 0; i < mutationRollsPerSpawn; i++)
        {
            Mutation m = MutationManager.instance.RollNonEventMutation();
            if (m == null || string.IsNullOrEmpty(m.id)) continue;

            if (!data.mutations.Contains(m.id)) // уникально [web:24]
                data.mutations.Add(m.id);
        }
    }

    private void CleanupNulls()
    {
        brainrotInChunk.RemoveAll(x => x == null);
    }

    private Vector3 GetRandomPointInBox(BoxCollider box)
    {
        Bounds bounds = box.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = bounds.max.y + 1f;
        float z = Random.Range(bounds.min.z, bounds.max.z);
        return new Vector3(x, y, z);
    }

    private bool TryProjectToGround(Vector3 startPos, out Vector3 hitPos)
    {
        Ray ray = new Ray(startPos, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDownDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            hitPos = hit.point;
            return true;
        }

        hitPos = startPos;
        return false;
    }

    public void GenerateBrainrot()
    {
        EnsureSpawned();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playersInside++;
        EnsureSpawned();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playersInside = Mathf.Max(0, _playersInside - 1);
    }
}
