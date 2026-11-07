using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GiveRewardButton : MonoBehaviour
{
    public Bonus bonus;

    public void Action()
    {
        RewardAD.manager.MyRewardAdvShow_Bonus("0", bonus);
    }
}
