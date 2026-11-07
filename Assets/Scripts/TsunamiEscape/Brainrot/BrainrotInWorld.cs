using System.Text;
using TMPro;
using UnityEngine;
using YG;
using DG.Tweening;
using Unity.VisualScripting;

public class BrainrotInWorld : Interaction
{
    public BrainrotBase brainrot;
    public BrainrotData brainrotData;

    public BrainrotState state;

    private HeadInfo _headInfo;

    [Header("Throw")]
    [SerializeField] private float throwForceForward = 6f;
    [SerializeField] private float throwForceUp = 3f;
    [SerializeField] private float stopVelocitySqr = 0.05f;

    [Header("Despawn timer (random step 5s: 30..60)")]
    [SerializeField] private int minDespawn = 30;
    [SerializeField] private int maxDespawn = 60;
    [SerializeField] private int step = 5;

    [Header("Mutation")]
    public float mutationMultiplier = 1f;

    [SerializeField] private Transform effectPivot;
    [SerializeField] private string autoPivotName = "EffectPivot";

    private GameObject _mutationFxInstance;

    private float _timer;
    private int _lastShownSeconds = -1;

    private BrainrotChunk _chunk;
    private int _spawnId = -1;

    private Rigidbody _rb;
    private bool _isFlying;
    private bool _isDespawning;

    [Header("Lucky block settings")]
    [SerializeField] private Transform spinVisualParent; // где «крутятся» модели
    [SerializeField] private float rollDuration = 2.0f;

    [Header("Lucky block visuals")]
    [SerializeField] private Renderer[] luckyBlockRenderers;

    [Tooltip("Минимальное время между сменами брейнрота в начале")]
    [SerializeField] private float minSwitchInterval = 0.05f;

    [Tooltip("Максимальное время между сменами брейнрота в конце")]
    [SerializeField] private float maxSwitchInterval = 1.2f;

    [Tooltip("Кривая замедления: X = нормализованное время [0..1], Y = коэффициент [0..1]")]
    [SerializeField] private AnimationCurve slowdownCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool _isRolling;

    private BrainrotPoint _ownerPoint;

    public bool IsLuckyBlock => brainrot != null && brainrot.IsLucky;

    [SerializeField] private GameObject _luckyBlockWin;

    private void Awake()
    {
        minDespawn = 30;
        maxDespawn = 60;
        step = 5;

        _headInfo = GetComponentInChildren<HeadInfo>();

        if (effectPivot == null && !string.IsNullOrEmpty(autoPivotName))
            effectPivot = FindDeepChild(transform, autoPivotName);

        ResetTimer(true);
        gameObject.AddComponent<HeadInfoProxy>();
    }

    public void SetChunk(BrainrotChunk chunk) => _chunk = chunk;
    public void SetSpawnId(int id) => _spawnId = id;
    public void SetOwnerPoint(BrainrotPoint point) => _ownerPoint = point;

    public void SetBrainrot(BrainrotBase brainrot, BrainrotData brainrotData)
    {
        this.brainrot = brainrot;
        this.brainrotData = brainrotData;

        ChangeBrainrotState(BrainrotState.InWorld);

        if (_headInfo != null)
            _headInfo.SetData(brainrot);

        RecalculateMutationMultiplier();
        ApplyMutationVisualsFromData();
        UpdateMutationTextFromData();

        ResetTimer(true);
    }

    private void ApplyMutationVisualsFromData()
    {
        ClearMutationFx();

        if (brainrotData == null || brainrotData.mutations == null || brainrotData.mutations.Count == 0)
            return;

        string activeId = brainrotData.mutations[brainrotData.mutations.Count - 1];
        Mutation active = MutationManager.instance != null ? MutationManager.instance.GetById(activeId) : null;
        if (active == null) return;

        if (active.overrideMaterial != null)
            ApplyMaterialToAllMeshes(active.overrideMaterial);

        if (active.centerEffectPrefab != null)
            SpawnEffectAtPivot(active.centerEffectPrefab);
    }

    private void SpawnEffectAtPivot(GameObject fxPrefab)
    {
        if (fxPrefab == null) return;

        Transform pivot = effectPivot != null ? effectPivot : transform;
        _mutationFxInstance = Instantiate(fxPrefab, pivot.position, pivot.rotation, pivot);
    }

    private void ClearMutationFx()
    {
        if (_mutationFxInstance != null)
        {
            Destroy(_mutationFxInstance);
            _mutationFxInstance = null;
        }
    }

    private void UpdateMutationTextFromData()
    {
        if (_headInfo == null || _headInfo.mutationDisplay == null) return;

        if (brainrotData == null || brainrotData.mutations == null || brainrotData.mutations.Count == 0)
        {
            _headInfo.mutationDisplay.text = "";
            return;
        }

        var sb = new StringBuilder(128);
        int shown = 0;

        for (int i = 0; i < brainrotData.mutations.Count; i++)
        {
            Mutation m = MutationManager.instance != null ? MutationManager.instance.GetById(brainrotData.mutations[i]) : null;
            if (m == null) continue;

            string name = UTF.ReadLanguageDirectory(m.mutationName);
            if (string.IsNullOrEmpty(name)) continue;

            if (shown > 0) sb.Append(" + ");

            if (m.useGradient && m.gradientPreset != null)
            {
                string presetName = m.gradientPreset.name;
                sb.Append("<color=#FFFFFFFF><gradient=\"");
                sb.Append(presetName);
                sb.Append("\">");
                sb.Append(name);
                sb.Append("</gradient></color>");
            }
            else
            {
                string hex = UnityEngine.ColorUtility.ToHtmlStringRGBA(m.color);
                sb.Append("<color=#");
                sb.Append(hex);
                sb.Append(">");
                sb.Append(name);
                sb.Append("</color>");
            }

            shown++;
        }

        _headInfo.mutationDisplay.text = sb.ToString();
    }

    private void ApplyMaterialToAllMeshes(Material mat)
    {
        if (mat == null) return;

        // Обычные меши
        var meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            var mr = meshRenderers[i];
            if (mr == null) continue;

            var mats = mr.sharedMaterials;
            for (int k = 0; k < mats.Length; k++)
            {
                mats[k] = mat;
            }
            mr.sharedMaterials = mats;
        }

        // Skinned меши (анимированные модели)
        var skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedRenderers.Length; i++)
        {
            var smr = skinnedRenderers[i];
            if (smr == null) continue;

            var mats = smr.sharedMaterials;
            for (int k = 0; k < mats.Length; k++)
            {
                mats[k] = mat;
            }
            smr.sharedMaterials = mats;
        }
    }

    private void RecalculateMutationMultiplier()
    {
        mutationMultiplier = 1f;

        if (brainrotData == null || brainrotData.mutations == null || brainrotData.mutations.Count == 0)
            return;

        if (MutationManager.instance == null)
            return;

        for (int i = 0; i < brainrotData.mutations.Count; i++)
        {
            Mutation m = MutationManager.instance.GetById(brainrotData.mutations[i]);
            if (m == null) continue;

            mutationMultiplier *= m.multiplier;
        }
    }

    private void Update()
    {
        if (_isDespawning) return;
        if (_isRolling) return;
        if (state != BrainrotState.InWorld) return;
        if (_isFlying) return;

        _timer -= Time.deltaTime;
        if (_timer < 0f) _timer = 0f;

        UpdateTimerUI();

        if (_timer <= 0f)
            DespawnByTimer();
    }

    private void UpdateTimerUI()
    {
        if (_headInfo == null) return;

        int secondsLeft = Mathf.CeilToInt(_timer);
        if (secondsLeft == _lastShownSeconds) return;

        _lastShownSeconds = secondsLeft;
        _headInfo.UpdateTimer(secondsLeft);
    }

    private int PickRandomDespawnSeconds()
    {
        int count = ((maxDespawn - minDespawn) / step) + 1;
        int i = Random.Range(0, count);
        return minDespawn + i * step;
    }

    private void ResetTimer(bool refreshUI)
    {
        _timer = PickRandomDespawnSeconds();

        if (refreshUI)
        {
            _lastShownSeconds = -1;
            UpdateTimerUI();
        }
    }

    private void DespawnByTimer()
    {
        if (_isDespawning) return;
        _isDespawning = true;

        if (_chunk != null)
            _chunk.NotifyBrainrotTimedOut(this);

        Destroy(gameObject);
    }

    public void ChangeBrainrotState(BrainrotState brainrotState)
    {
        state = brainrotState;
        UpdateDisplay();

        if (_headInfo != null)
            _headInfo.SetTimerVisible(state == BrainrotState.InWorld);

        switch (state)
        {
            case BrainrotState.InWorld:
                ResetTimer(true);
                // тут физика должна быть включена
                EnablePhysics();
                break;

            case BrainrotState.InHand:
                // в руке физика не нужна — убираем Rigidbody
                DisablePhysics();
                break;

            case BrainrotState.InBase:
                // в базе обычно тоже не нужна физика, если они просто стоят в слотах
                DisablePhysics();
                break;
        }
    }


    public void UpdateDisplay()
    {
        if (brainrot != null)
            interactionName = brainrot.brainrotName;

        switch (state)
        {
            case BrainrotState.InWorld:
                canInteraction = true;

                interactionDescription = LangList.instance.pickupBrainrot;

                //if (IsLuckyBlock)
                    //interactionDescription = new LanguageDirectory("Открыть лаки блок", "Open lucky block", "Lucky blok ac");
                //else
                    //interactionDescription = LangList.instance.pickupBrainrot;

                return;
                break;

            case BrainrotState.InHand:
                canInteraction = false;
                break;

            case BrainrotState.InBase:

                if (_ownerPoint != null && _ownerPoint.owner != PlayersManager.instance.realPlayer)
                {
                    canInteraction = true;

                    interactionDescription = new LanguageDirectory("Украcть за 99" + UTF.manager.TextToSprite("\U0001F602"), "Steal for 99" + UTF.manager.TextToSprite("\U0001F602"), "Icin calmak 99" + UTF.manager.TextToSprite("\U0001F602"));
                }
                else if (_ownerPoint != null && _ownerPoint.owner == PlayersManager.instance.realPlayer)
                {
                    canInteraction = true;
                    LanguageDirectory langSell = LangList.instance.sellBrainrot;
                    string end = brainrot != null ? (" " + (brainrot.earningPerSecond * 5) + "$") : "";
                    interactionDescription = new LanguageDirectory(langSell.ru + end, langSell.en + end, langSell.tr + end);
                }

                if (IsLuckyBlock && _ownerPoint != null)
                {
                    canInteraction = true;
                    interactionDescription = new LanguageDirectory("Открыть лаки блок", "Open lucky block", "Lucky blok ac");
                }

                break;
        }
    }

    public override void Action(Player player)
    {
        if (!canInteraction) return;

        if (IsLuckyBlock && state == BrainrotState.InBase && _ownerPoint != null && !_isRolling)
        {
            _ownerPoint.OpenLuckyBrainrot(this);
            return;
        }

        if (state == BrainrotState.InBase && _ownerPoint != null)
        {
            bool isOurBase = (_ownerPoint.owner == PlayersManager.instance.realPlayer);

            if (isOurBase)
            {
                // ПРОДАЖА: деньги + очистить слот + уничтожить объект
                ValueSystem.AddBalance(brainrot.earningPerSecond * 5, ValueType.Coin);

                _ownerPoint.ClearBrainrot(needSave: true, destroyInstance: true);
                _ownerPoint = null;

                PlayersManager.instance.realPlayer.tsunamiReference.playerBase.RefreshInfoDisplay();

                Destroy(gameObject); // на всякий случай, если destroyInstance=false где-то поменяешь [web:338]
                return;
            }
            else
            {
                // КРАЖА: нужно место у игрока
                if (PlayersManager.instance.realPlayer.tsunamiReference.playerBase.FindFreePoint() == null)
                    return;

                if (!ValueSystem.SubstractionBalance(99, ValueType.Diamond))
                    return;

                // Очищаем слот у владельца (сейв чистим только если владелец = realPlayer)
                bool needSaveOwner = _ownerPoint.owner != null && _ownerPoint.owner.thisRealPlayer;
                _ownerPoint.ClearBrainrot(needSave: needSaveOwner, destroyInstance: false);

                // и уносим этот же объект в руку
                _ownerPoint = null;
            }
        }

        player.tsunamiReference.playerCarry.SetCarryObject(this);
    }


    public void ThrowFrom(Transform origin, Vector3 forward)
    {
        transform.SetParent(null, true);

        ChangeBrainrotState(BrainrotState.InWorld);

        EnablePhysics();

        transform.position = origin.position;

        float randomY = Random.Range(0f, 360f);
        transform.rotation = Quaternion.Euler(0f, randomY, 0f);

        Vector3 dir = forward.normalized;
        Vector3 velocity = dir * throwForceForward + Vector3.up * throwForceUp;

        _rb.velocity = velocity;

        _isFlying = true;
        canInteraction = false;
    }


    private void FixedUpdate()
    {
        if (!_isFlying || _rb == null) return;

        if (_rb.velocity.sqrMagnitude < stopVelocitySqr && _rb.IsSleeping())
            StopPhysics();
    }

    private void StopPhysics()
    {
        _isFlying = false;

        if (_rb != null)
        {
            Destroy(_rb);
            _rb = null;
        }

        canInteraction = true;
    }

    // ---------- Lucky block roll: 0 -> 1 -> 0, новый на 0.5 ----------

    public void StartLuckyRoll()
    {
        if (!IsLuckyBlock || _isRolling) return;
        if (!(brainrot is LuckyBrainrot luckyCfg)) return;

        SetLuckyBlockVisible(false);   // СКРЫТЬ блок на время открытия

        StartCoroutine(LuckyRollRoutine(luckyCfg));
    }

    private System.Collections.IEnumerator LuckyRollRoutine(LuckyBrainrot luckyCfg)
    {
        _isRolling = true;
        canInteraction = false;

        float elapsed = 0f;

        GameObject currentVisual = null;
        Transform parent = spinVisualParent != null ? spinVisualParent : transform;

        float currentInterval = minSwitchInterval;
        float currentLifeTime = 0f;
        bool spawnedNextForThisVisual = false;

        // почти максимум: пока интервал меньше этого — крутимся
        float slowThreshold = maxSwitchInterval * 0.95f;

        // заранее выбираем финального победителя
        Brainrot finalBrainrot = luckyCfg.GetRandomBrainrot();
        if (finalBrainrot == null || finalBrainrot.brainrotPrefab == null)
        {
            _isRolling = false;
            canInteraction = true;
            yield break;
        }

        // helper: спавн визуала по Brainrot
        System.Action<Brainrot> spawnVisualFor = (Brainrot br) =>
        {
            if (currentVisual != null)
                Destroy(currentVisual);

            if (br == null || br.brainrotPrefab == null)
            {
                currentVisual = null;
                return;
            }

            BrainrotInWorld prefabComp = br.brainrotPrefab.GetComponent<BrainrotInWorld>();
            GameObject visualPrefab = prefabComp != null ? prefabComp.gameObject : br.brainrotPrefab;

            currentVisual = Object.Instantiate(visualPrefab, parent);
            currentVisual.transform.localPosition = Vector3.zero;
            currentVisual.transform.localRotation = Quaternion.identity;
            currentVisual.transform.localScale = Vector3.zero;

            // ВАЖНО: временный визуал не интерактивен
            BrainrotInWorld visualBrainrot = currentVisual.GetComponent<BrainrotInWorld>();
            if (visualBrainrot != null)
            {
                visualBrainrot.canInteraction = false;
            }

            currentLifeTime = 0f;
            spawnedNextForThisVisual = false;

            float half = currentInterval * 0.5f;

            DG.Tweening.Sequence seq = DOTween.Sequence();
            seq.Append(currentVisual.transform.DOScale(Vector3.one, half).SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (UISound.manager != null)
                        UISound.manager.PlayClickShort(false);
                }));
            seq.Append(currentVisual.transform.DOScale(Vector3.zero, half).SetEase(Ease.InQuad));
        };

        System.Action spawnRandomVisual = () =>
        {
            Brainrot random = luckyCfg.GetRandomBrainrot();
            spawnVisualFor(random);
        };

        // Фаза 1: крутилка с рандомами до замедления
        spawnRandomVisual();

        while (true)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            float tNormGlobal = Mathf.Clamp01(elapsed / rollDuration);
            float k = slowdownCurve != null ? slowdownCurve.Evaluate(tNormGlobal) : tNormGlobal;
            currentInterval = Mathf.Lerp(minSwitchInterval, maxSwitchInterval, k);

            if (currentVisual != null)
            {
                currentLifeTime += dt;

                if (!spawnedNextForThisVisual)
                {
                    float lifeNorm = currentLifeTime / currentInterval;
                    if (lifeNorm >= 0.5f)
                    {
                        spawnedNextForThisVisual = true;
                        spawnRandomVisual();
                    }
                }
            }
            else
            {
                spawnRandomVisual();
            }

            // как только достаточно замедлились и прошло минимум rollDuration — выходим в финальную фазу
            if (elapsed >= rollDuration && currentInterval >= slowThreshold)
                break;

            yield return null;
        }

        // Фаза 2: показываем один раз именно финального брейнрота 0 -> 1 -> 0 и сразу дропаем
        spawnVisualFor(finalBrainrot);
        yield return new WaitForSeconds(currentInterval); // дождаться цикла финального визуала

        if (currentVisual != null)
            Destroy(currentVisual);

        if (finalBrainrot != null && _ownerPoint != null)
        {
            BrainrotData data = new BrainrotData();
            data.id = finalBrainrot.id;
            data.mutations = brainrotData.mutations;

            _ownerPoint.SetBrainrot(data, true);
        }

        if (_luckyBlockWin != null)
            Destroy(Instantiate(_luckyBlockWin, effectPivot.position, Quaternion.identity), 5f);

        Destroy(gameObject);
    }


    // ---------- helpers ----------

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null) return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform c = parent.GetChild(i);
            if (c.name == childName) return c;

            Transform r = FindDeepChild(c, childName);
            if (r != null) return r;
        }
        return null;
    }

    private void SetLuckyBlockVisible(bool visible)
    {
        if (!IsLuckyBlock) return;
        if (luckyBlockRenderers == null) return;

        for (int i = 0; i < luckyBlockRenderers.Length; i++)
        {
            if (luckyBlockRenderers[i] != null)
                luckyBlockRenderers[i].enabled = visible;
        }
    }

    public void EnablePhysics()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
            _rb = gameObject.AddComponent<Rigidbody>();

        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void DisablePhysics()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            Destroy(_rb);      // полностью убираем Rigidbody = «нет веса»
            _rb = null;
        }
    }

}
