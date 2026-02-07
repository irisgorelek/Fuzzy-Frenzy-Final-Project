using System;
using UnityEngine;
public enum PowerUpType
{
    ExtraMove,
    Bomb,
    TimerBomb
}

[Serializable]
public class PowerUpSaveData
{
    public int saveVersion = 1; // Creates a default for if something changes

    public int extraMove = 0;
    public int bomb = 0;
    public int timerBomb = 0;
}
