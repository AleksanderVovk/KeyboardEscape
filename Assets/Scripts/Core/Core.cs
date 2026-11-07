using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YG;

public class Core : MonoBehaviour
{
    public static Core manager;

    public Player player;

    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        YG2.GameReadyAPI();
    }
}
