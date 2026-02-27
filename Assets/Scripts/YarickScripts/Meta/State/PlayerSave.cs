using System;

namespace Meta.State
{
    [Serializable]
    public sealed class PlayerSave
    {
        public ProgressState progress = new ProgressState();
        public WalletState wallet = new WalletState();
        public LivesState lives = new LivesState();
        public StarsChestState starsChest = new StarsChestState();
        public LevelsChestState levelsChest = new LevelsChestState();
        public BankState bank = new BankState();
        public BattlepassState battlepass = new BattlepassState();
        public WinStreakState winStreak = new WinStreakState();

        public InventoryState inventory = new InventoryState();
        public TimeBonusesState timeBonuses = new TimeBonusesState();
    }
}
