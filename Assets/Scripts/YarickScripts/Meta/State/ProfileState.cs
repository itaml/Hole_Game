using System;
using UnityEngine;

namespace Meta.State
{
    [Serializable]
    public sealed class ProfileState
    {
        public string characterName = "Player";

        public bool adsRemoved;

        // Обычно удобнее хранить ID (int) или ключ (string), не сам Sprite.
        public int avatarId = 0;
        public int frameId = 0;

        // 1) Сколько уровней пройдено с первого раза
        public int firstTryWins = 0;

        // 2) Самая длинная серия побед без поражений
        public int longestWinStreak = 0;

        // 3) Сколько уровней выиграно на 3 звезды
        public int threeStarWins = 0;

        // 4) Login streak (сколько дней подряд заходил)
        public int loginDaysStreak = 0;

        // Для расчёта login streak
        public long lastLoginUtcTicks = 0;

        // Текущая серия побед (для расчёта longest)
        public int currentWinStreak = 0;
    }
}