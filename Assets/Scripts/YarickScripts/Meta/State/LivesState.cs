using System;

namespace Meta.State
{
    [Serializable]
    public sealed class LivesState
    {
        public int currentLives = 5;
        public int maxLives = 5;

        /// <summary>0 if regen timer isn't running; otherwise UTC ticks when next life is ready.</summary>
        public long nextLifeReadyAtUtcTicks = 0;
    }
}
