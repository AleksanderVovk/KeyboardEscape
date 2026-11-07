using UnityEngine;

public class NPCBrainrotCarrier : MonoBehaviour
{
    public BrainrotInWorld brainrotInHand;
    [SerializeField] private Transform handPoint;
    public Base npcBase;            // База, на которую он носит
    public Player npcAsPlayer;      // "owner", чтобы реюзать часть логики

    public bool HasBrainrot => brainrotInHand != null;
    public bool BaseHasFreePoint => npcBase != null && npcBase.FindFreePoint() != null;

    public void Initialization(Player owner, Base npcBase)
    {
        this.npcAsPlayer = owner;
        this.npcBase = npcBase;
    }

    public bool TryPickup(BrainrotInWorld brainrot)
    {
        if (brainrotInHand != null) return false;
        if (!BaseHasFreePoint) return false;

        brainrotInHand = brainrot;
        brainrot.ChangeBrainrotState(BrainrotState.InHand);
        brainrot.transform.SetParent(handPoint, false);
        brainrot.transform.localPosition = Vector3.zero;
        brainrot.transform.localRotation = handPoint.localRotation;

        // анимация как у игрока
        if (npcAsPlayer != null && npcAsPlayer.animator != null)
            npcAsPlayer.animator.SetBool("isCarry", true);

        return true;
    }

    public void PlaceToBasePoint(BrainrotPoint point)
    {
        if (brainrotInHand == null || point == null) return;

        // та же логика, что Base.PlaceBrainrotInPoint, но без туториалов/инедксов
        point.SetBrainrot(brainrotInHand.brainrotData, true);

        Destroy(brainrotInHand.gameObject);
        brainrotInHand = null;

        if (npcAsPlayer != null && npcAsPlayer.animator != null)
            npcAsPlayer.animator.SetBool("isCarry", false);
    }

    public void OnThrowPressed()
    {
        if (brainrotInHand == null) return;

        // направление броска — куда смотрит игрок
        Vector3 forward = npcAsPlayer.transform.forward;

        var toThrow = brainrotInHand;
        brainrotInHand = null;

        npcAsPlayer.animator.SetBool("isCarry", false);

        toThrow.ThrowFrom(handPoint, forward);
    }
}