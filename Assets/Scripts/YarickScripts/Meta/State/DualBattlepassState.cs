using System;

namespace Meta.State
{
    [Serializable]
    public sealed class DualBattlepassState
    {
        public long seasonStartUtcTicks = 0;

        // Прогресс: кол-во побед в сезоне
        public int wins = 0;

        // Сколько FREE тиров уже выдано (индекс next-to-grant)
        public int freeGranted = 0;

        // Куплен ли Premium
        public bool premiumActive = false;

        // Сколько PREMIUM тиров уже выдано
        public int premiumGranted = 0;
    }
}