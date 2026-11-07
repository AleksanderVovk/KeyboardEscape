using System;
using UnityEngine;

[Serializable]
public struct ReplayFrame
{
    public float time;

    public Vector3 position;
    public Quaternion rootRotation;  // rotation root объекта игрока (transform.rotation)

    public Quaternion bodyLocalRotation; // локальный rotation модели (_modelRoot.localRotation)

    public Vector3 velocity;

    public short moveX;
    public short moveY;
    public short lookX;
    public short lookY;

    public byte flags;
}

public static class ReplayFlags
{
    public const byte Run = 1 << 0;
    public const byte Jump = 1 << 1;
    public const byte OnLadder = 1 << 2;
    public const byte IsPushed = 1 << 3;
    public const byte Grounded = 1 << 4;
    public const byte Walking = 1 << 5;
}