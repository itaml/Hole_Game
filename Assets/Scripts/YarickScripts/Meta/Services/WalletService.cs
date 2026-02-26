using Meta.State;

namespace Meta.Services
{
    public sealed class WalletService
    {
        public void AddCoins(PlayerSave save, int amount)
        {
            if (amount <= 0) return;
            save.wallet.coins += amount;
        }

        public bool TrySpend(PlayerSave save, int amount)
        {
            if (amount <= 0) return true;
            if (save.wallet.coins < amount) return false;
            save.wallet.coins -= amount;
            return true;
        }
    }
}
