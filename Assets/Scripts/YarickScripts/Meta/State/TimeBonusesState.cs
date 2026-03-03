using System;

namespace Meta.State
{
    [Serializable]
    public sealed class TimeBonusesState
    {
        public long infiniteLivesUntilUtcTicks =0;

        // 🔥 NEW: отдельно по бустам
        public long infiniteBoost1UntilUtcTicks = 0;
        public long infiniteBoost2UntilUtcTicks = 0;
    }
}