using Meta.State;

namespace Meta.Services
{
    public sealed class WinStreakService
    {
        public void OnWin(PlayerSave save)
        {
            save.winStreak.currentStreak++;
            if (save.winStreak.currentStreak >= 3)
                save.winStreak.currentStreak = 3;
        }

        public void OnLose(PlayerSave save)
        {
            save.winStreak.currentStreak = 0;
        }

        public int GetStreak(PlayerSave save)
        {
            return save.winStreak.currentStreak;
        }
    }
}
