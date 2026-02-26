using Meta.State;

namespace Meta.Services
{
    public sealed class WinStreakService
    {
        public void OnWin(PlayerSave save)
        {
            save.winStreak.currentStreak++;
            if (save.winStreak.currentStreak >= 3)
                save.winStreak.bonusBagArmed = true;
        }

        public void OnLose(PlayerSave save)
        {
            save.winStreak.currentStreak = 0;
            save.winStreak.bonusBagArmed = false;
        }

        public bool ConsumeBonusBagIfArmed(PlayerSave save)
        {
            if (!save.winStreak.bonusBagArmed) return false;
            save.winStreak.bonusBagArmed = false;
            return true;
        }
    }
}
