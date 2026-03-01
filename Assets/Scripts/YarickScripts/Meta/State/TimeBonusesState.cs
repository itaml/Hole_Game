using System;

namespace Meta.State
{
    [Serializable]
    public sealed class TimeBonusesState
    {
        public long infiniteLivesUntilUtcTicks = 5000;

        // 🔥 NEW: отдельно по бустам
        public long infiniteBoost1UntilUtcTicks = 5000;
        public long infiniteBoost2UntilUtcTicks = 5000;
    }
}