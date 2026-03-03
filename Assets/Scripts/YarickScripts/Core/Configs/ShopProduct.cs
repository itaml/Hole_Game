using System;
using UnityEngine;

public enum ShopProductKind
{
    Consumable,
    NonConsumable
}

[Serializable]
public class ShopProduct
{
    public string productId;
    public ShopProductKind kind = ShopProductKind.Consumable;

    public ShopReward reward;

    [Header("Special flags")]
    public bool removesAds; // если это бандл "Remove Ads"
}