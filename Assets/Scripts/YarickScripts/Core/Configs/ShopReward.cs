using System;
using UnityEngine;

[Serializable]
public struct ShopReward
{
    [Header("Currency")]
    public int coins;

    [Header("Buffs")]
    public int buff1;
    public int buff2;
    public int buff3;
    public int buff4;

    [Header("Boosts")]
    public int boost1;
    public int boost2;

    [Header("Time bonuses")]
    public int infiniteLivesHours;   // например 6 часов бесконечных жизней
}