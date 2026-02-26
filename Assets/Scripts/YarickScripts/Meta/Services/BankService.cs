using Core.Configs;
using Meta.State;

namespace Meta.Services
{
    public sealed class BankService
    {
        private readonly BankConfig _cfg;

        public BankService(BankConfig cfg) { _cfg = cfg; }

        public void AddToBank(PlayerSave save, int amount)
        {
            if (amount <= 0) return;

            save.bank.bankCoins += amount;
            if (_cfg.capacity > 0)
                save.bank.bankCoins = System.Math.Min(save.bank.bankCoins, _cfg.capacity);
        }

        /// <summary>
        /// IAP flow should call this: returns amount to add to wallet, resets bank to 0.
        /// </summary>
        public int ClaimBank(PlayerSave save, int iapBonusCoins)
        {
            int total = save.bank.bankCoins + iapBonusCoins;
            save.bank.bankCoins = 0;
            return total;
        }
    }
}
