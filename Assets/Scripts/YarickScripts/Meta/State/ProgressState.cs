using System;
using System.Collections.Generic;

namespace Meta.State
{
    [Serializable]
    public sealed class ProgressState
    {
        /// <summary>Next available level to start (no level select; can't go back after win).</summary>
        public int currentLevel = 1;

        public HashSet<int> failedLevels = new HashSet<int>();
    }
}
