using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TsunamiReference : MonoBehaviour
{
    private Player owner;

    public Base playerBase;
    public PlayerCarry playerCarry;

    public void Initialization(Player owner, Base playerBase)
    {
        this.owner = owner;
        this.playerBase = playerBase;

        playerBase.Initialization(owner);

        playerCarry = GetComponent<PlayerCarry>();
        playerCarry.Initialization(owner);

        if (!owner.thisRealPlayer)
        {
            owner.npcController.npcBase = playerBase;
            owner.npcController.carrier.Initialization(owner, playerBase);

            Debug.Log("Initialization NPC: " + owner.name);
        }
    }
}
