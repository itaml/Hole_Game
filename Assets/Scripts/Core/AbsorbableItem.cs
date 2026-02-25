using UnityEngine;

public enum ItemType
{
    Generic = 0,
    Apple = 1,
    Pear = 2,
    Coin = 3,
    // добавишь что нужно
}

public class AbsorbableItem : MonoBehaviour
{
    [field: SerializeField] public ItemType Type { get; private set; } = ItemType.Generic;
    [field: SerializeField] public int XpValue { get; private set; } = 1;
    [field: SerializeField] public Sprite UiIcon { get; private set; }

    [Header("Absorb")]
    [SerializeField] private float minHoleLevelToAbsorb = 1; // если нужно ограничение

    public bool CanBeAbsorbedBy(HoleController hole)
    {
        if (hole == null) return false;
        return hole.Level >= minHoleLevelToAbsorb;
    }
}