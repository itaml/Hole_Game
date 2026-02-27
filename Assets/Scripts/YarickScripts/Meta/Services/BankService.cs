using System;
using Core.Configs;
using Meta.State;

namespace Meta.Services
{
    public sealed class BankService
    {
        private readonly BankConfig _config;

        public BankService(BankConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Начисляет монеты в банк при победе (НЕ забирая из wallet).
        /// Возвращает сколько реально начислили (с учётом capacity).
        /// </summary>
        public int AddWinDeposit(PlayerSave save)
        {
            if (save == null) return 0;

            int deposit = Math.Max(0, _config.depositOnWin);
            if (deposit <= 0) return 0;

            // Capacity (0 = unlimited)
            if (_config.capacity > 0)
            {
                int spaceLeft = _config.capacity - save.bank.bankCoins;
                if (spaceLeft <= 0) return 0;

                deposit = Math.Min(deposit, spaceLeft);
                if (deposit <= 0) return 0;
            }

            save.bank.bankCoins += deposit;

            // safety
            if (save.bank.bankCoins < 0) save.bank.bankCoins = 0;

            return deposit;
        }
    }
}