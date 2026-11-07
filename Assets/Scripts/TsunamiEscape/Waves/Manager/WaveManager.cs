using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private WaveDatabaseSO database;

    [Header("Spawn points")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform endPoint;

    [Header("Spawn timing")]
    [SerializeField] private Vector2 spawnIntervalRange = new Vector2(2f, 8f); // короче интервал
    [SerializeField] private bool useUnscaledTime = false;

    [Header("Multi-wave spawn")]
    [Tooltip("Шанс заспавнить несколько волн сразу (0-1)")]
    [SerializeField][Range(0f, 1f)] private float multiWaveChance = 0.3f;

    [Tooltip("Сколько волн максимум за раз (если выпал шанс)")]
    [SerializeField][Range(1, 5)] private int maxMultiWaves = 3;

    [Tooltip("Задержка между волнами в мульти-спавне (сек)")]
    [SerializeField] private float delayBetweenMultiWaves = 0.5f;

    [Header("Lifetime")]
    [SerializeField] private float waveLifeTime = 90f;

    [Header("Filters")]
    [SerializeField] private bool spawnOnlyNormally = true;

    private Coroutine _loop;

    private void OnEnable()
    {
        StartLoop();
    }

    private void OnDisable()
    {
        StopLoop();
    }

    private void Awake()
    {
        if (database != null)
            database.EnsureLoaded();
    }

    public void StartLoop()
    {
        if (_loop != null) return;
        _loop = StartCoroutine(SpawnLoop());
    }

    public void StopLoop()
    {
        if (_loop == null) return;
        StopCoroutine(_loop);
        _loop = null;
    }

    public void SpawnEventWave(WaveDefinitionSO wave)
    {
        if (wave == null) return;
        SpawnWave(wave);
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float wait = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
            if (useUnscaledTime)
            {
                float t = 0f;
                while (t < wait) { t += Time.unscaledDeltaTime; yield return null; }
            }
            else
            {
                yield return new WaitForSeconds(wait);
            }

            // Решаем, спавним одну волну или несколько
            int wavesToSpawn = 1;
            if (Random.value < multiWaveChance)
            {
                wavesToSpawn = Random.Range(2, maxMultiWaves + 1);
            }

            for (int i = 0; i < wavesToSpawn; i++)
            {
                var wave = PickRandomWave();
                if (wave != null)
                {
                    SpawnWave(wave);

                    // Пауза между волнами в мульти-спавне
                    if (i < wavesToSpawn - 1 && delayBetweenMultiWaves > 0f)
                    {
                        if (useUnscaledTime)
                        {
                            float t = 0f;
                            while (t < delayBetweenMultiWaves) { t += Time.unscaledDeltaTime; yield return null; }
                        }
                        else
                        {
                            yield return new WaitForSeconds(delayBetweenMultiWaves);
                        }
                    }

                    // Доп. кулдаун для конкретной волны (если есть)
                    if (wave.extraCooldown > 0f)
                    {
                        if (useUnscaledTime)
                        {
                            float t = 0f;
                            while (t < wave.extraCooldown) { t += Time.unscaledDeltaTime; yield return null; }
                        }
                        else
                        {
                            yield return new WaitForSeconds(wave.extraCooldown);
                        }
                    }
                }
            }
        }
    }

    private WaveDefinitionSO PickRandomWave()
    {
        if (database == null || database.waves == null || database.waves.Count == 0)
            return null;

        List<WaveDefinitionSO> candidates = null;
        float total = 0f;

        for (int i = 0; i < database.waves.Count; i++)
        {
            var w = database.waves[i];
            if (w == null || w.wavePrefab == null) continue;
            if (spawnOnlyNormally && !w.spawnNormally) continue;
            if (w.weight <= 0f) continue;

            candidates ??= new List<WaveDefinitionSO>(16);
            candidates.Add(w);
            total += w.weight;
        }

        if (candidates == null || candidates.Count == 0 || total <= 0f)
            return null;

        float r = Random.Range(0f, total);
        float acc = 0f;

        for (int i = 0; i < candidates.Count; i++)
        {
            acc += candidates[i].weight;
            if (r <= acc) return candidates[i];
        }

        return candidates[candidates.Count - 1];
    }

    private void SpawnWave(WaveDefinitionSO wave)
    {
        if (spawnPoint == null || endPoint == null) return;
        if (wave == null || wave.wavePrefab == null) return;

        var go = Instantiate(wave.wavePrefab, spawnPoint.position, spawnPoint.rotation);

        var dir = (endPoint.position - spawnPoint.position);
        if (dir.sqrMagnitude < 0.0001f) dir = spawnPoint.forward;

        var instance = go.GetComponent<WaveInstance>();
        if (instance == null) instance = go.AddComponent<WaveInstance>();

        instance.Init(wave, wave.moveSpeed, dir, endPoint.position, waveLifeTime);
    }
}
