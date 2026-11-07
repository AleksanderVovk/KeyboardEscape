using UnityEngine;
using YG;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;

    [Header("Refs")]
    public Player player;
    public Transform baseTarget;
    public WorldLinePointer arrow;

    [Header("Tutorial UI")]
    public TutorialStepView stepView;

    [Header("Step texts")]
    public LanguageDirectory msgGoToBrainrot = new LanguageDirectory(
        "Подними ближайшего брейнрота",
        "Pick up the nearest brainrot",
        "En yakin brainrot'u al"
    );

    public LanguageDirectory msgCarryToBase = new LanguageDirectory(
        "Отнеси брейнрота на базу",
        "Bring the brainrot to your base",
        "Brainrot'u üsse götür"
    );

    public LanguageDirectory msgDone = new LanguageDirectory(
        "Обучение завершено!",
        "Tutorial complete!",
        "Eğitim tamamlandı!"
    );

    [Header("Search")]
    public float searchRadius = 200f;
    public float rescanInterval = 1.0f;
    public bool includeInactive = false;

    [SerializeField] private BrainrotInWorld _worldTarget;
    [SerializeField] private BrainrotInWorld _carriedTarget;

    private float _rescanTimer;

    private enum Step { Off, GoToBrainrot, CarryToBase, Done }
    [SerializeField] private Step _step = Step.Off;
    private Step _lastStep = Step.Off;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    public bool IsActive => _step != Step.Off && _step != Step.Done;

    public void TryStartTutorial(Transform baseTransform)
    {
        if (YG2.saves == null || YG2.saves.trainingData == null) return;

        baseTarget = baseTransform;

        if (YG2.saves.trainingData.training_brainrot_pickup &&
            YG2.saves.trainingData.training_brainrot_complete)
        {
            StopTutorial();
            return;
        }

        if (player == null) player = FindFirstObjectByType<Player>();
        if (player == null || baseTarget == null || arrow == null || stepView == null)
        {
            Debug.LogWarning("TutorialManager: set player/baseTarget/arrow/stepView");
            return;
        }

        arrow.SetFrom(player.transform);
        arrow.Show(true);

        _step = Step.GoToBrainrot;
        _lastStep = Step.Off;

        AcquireOrReselectWorldTarget(force: true);
        PointLineToCurrentGoal();
        UpdateStepUIIfNeeded();
    }

    private void Update()
    {
        if (_step == Step.Off || _step == Step.Done) return;
        if (player == null) return;

        bool hasCarried = TryGetCarriedBrainrot(player, out BrainrotInWorld carried);

        if (_step == Step.GoToBrainrot)
        {
            if (hasCarried)
            {
                _carriedTarget = carried;

                YG2.saves.trainingData.training_brainrot_pickup = true;
                YG2.SaveProgress();

                _step = Step.CarryToBase;
                PointLineToCurrentGoal();
                UpdateStepUIIfNeeded();
                return;
            }

            if (_worldTarget == null || _worldTarget.state != BrainrotState.InWorld)
                AcquireOrReselectWorldTarget(force: false);

            PointLineToCurrentGoal();
            UpdateStepUIIfNeeded();
            return;
        }

        if (_step == Step.CarryToBase)
        {
            if (!hasCarried)
            {
                _carriedTarget = null;
                _step = Step.GoToBrainrot;

                AcquireOrReselectWorldTarget(force: true);
                PointLineToCurrentGoal();
                UpdateStepUIIfNeeded();
                return;
            }

            if (_carriedTarget == null || _carriedTarget != carried)
                _carriedTarget = carried;

            PointLineToCurrentGoal();
            UpdateStepUIIfNeeded();
        }
    }

    private void AcquireOrReselectWorldTarget(bool force)
    {
        _rescanTimer -= Time.deltaTime;
        if (!force && _rescanTimer > 0f) return;
        _rescanTimer = rescanInterval;

        _worldTarget = FindClosestBrainrot(player.transform.position, searchRadius);
    }

    private void PointLineToCurrentGoal()
    {
        if (arrow == null) return;

        if (arrow.from == null && player != null)
            arrow.SetFrom(player.transform);

        switch (_step)
        {
            case Step.GoToBrainrot:
                arrow.SetTarget(_worldTarget != null ? _worldTarget.transform : null);
                break;
            case Step.CarryToBase:
                arrow.SetTarget(baseTarget);
                break;
        }
    }

    private void UpdateStepUIIfNeeded()
    {
        if (_lastStep == _step) return;
        _lastStep = _step;

        if (stepView == null) return;

        switch (_step)
        {
            case Step.GoToBrainrot:
                stepView.Show(msgGoToBrainrot);
                break;
            case Step.CarryToBase:
                stepView.Show(msgCarryToBase);
                break;
            case Step.Done:
                stepView.ShowForSeconds(msgDone, 3f);
                break;
            case Step.Off:
                stepView.Hide();
                break;
        }
    }

    public void FinishTutorialFromBase()
    {
        if (_step == Step.Off || _step == Step.Done) return;

        YG2.saves.trainingData.training_brainrot_complete = true;
        YG2.SaveProgress();

        _step = Step.Done;

        YG2.MetricaSend("tutorial_end");

        if (arrow != null) arrow.Show(false);
        UpdateStepUIIfNeeded();

        RewardsPanel.instance.OpenDailyReward();

        Debug.Log("Tutorial finished from base");
    }

    private void StopTutorial()
    {
        _step = Step.Off;
        _lastStep = Step.Off;

        if (arrow != null) arrow.Show(false);
        if (stepView != null) stepView.HideInstant();
    }

    private BrainrotInWorld FindClosestBrainrot(Vector3 from, float radius)
    {
        BrainrotInWorld[] all = FindObjectsByType<BrainrotInWorld>(FindObjectsSortMode.None);

        BrainrotInWorld best = null;
        float bestSqr = float.PositiveInfinity;
        float rSqr = radius * radius;

        for (int i = 0; i < all.Length; i++)
        {
            var b = all[i];
            if (b == null) continue;
            if (!includeInactive && !b.gameObject.activeInHierarchy) continue;
            if (b.state != BrainrotState.InWorld) continue;

            float sqr = (b.transform.position - from).sqrMagnitude;
            if (sqr < bestSqr && sqr <= rSqr)
            {
                bestSqr = sqr;
                best = b;
            }
        }
        return best;
    }

    // ГЛАВНОЕ ИСПРАВЛЕНИЕ: работаем со списком brainrotsInHand
    private bool TryGetCarriedBrainrot(Player p, out BrainrotInWorld carried)
    {
        carried = null;

        if (p == null || p.tsunamiReference == null || p.tsunamiReference.playerCarry == null)
            return false;

        var carry = p.tsunamiReference.playerCarry;

        if (carry.brainrotsInHand == null || carry.brainrotsInHand.Count == 0)
            return false;

        // туториалу всё равно, сколько их — берём первый (или последний)
        for (int i = 0; i < carry.brainrotsInHand.Count; i++)
        {
            var br = carry.brainrotsInHand[i];
            if (br != null && br.state == BrainrotState.InHand)
            {
                carried = br;
                return true;
            }
        }

        return false;
    }
}
