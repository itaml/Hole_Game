using System;
using Core.Configs;

namespace Meta.State
{
    [Serializable]
    public sealed class BountyState
    {
        public bool initialized;              // пул уже создан хоть раз
        public long generatedAtUtcTicks;       // когда сгенерировали

        public int claimedMask;               // битовая маска 6 слотов
        public Reward[] slots = new Reward[6];

        public bool IsClaimed(int index) => (claimedMask & (1 << index)) != 0;
        public void SetClaimed(int index) => claimedMask |= (1 << index);
        public void ResetClaims() => claimedMask = 0;
    }
}