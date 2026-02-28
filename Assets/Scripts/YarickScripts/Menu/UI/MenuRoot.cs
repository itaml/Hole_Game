using UnityEngine;
using Core.Configs;
using Core.Save;
using Core.Time;
using GameBridge.SceneFlow;
using GameBridge.Contracts;
using Meta.Services;

namespace Menu.UI
{
    public sealed class MenuRoot : MonoBehaviour
    {
        [Header("Configs (assign ScriptableObjects)")]
        public UnlockConfig unlockConfig;
        public EconomyConfig economyConfig;
        public ChestConfig starsChestConfig;
        public ChestConfig levelsChestConfig;
        public BankConfig bankConfig;
        public BattlepassConfig battlepassConfig;

        private ITimeProvider _time;
        private SaveSystem _saveSystem;
        private MetaFacade _meta;

        public bool boost1Selected = false;
        public bool boost2Selected = false;

        public MetaFacade Meta => _meta;

        private void Awake()
        {
            _time = new DeviceTimeProvider();

            _saveSystem = new SaveSystem(new PlayerPrefsJsonStorage());
            _saveSystem.LoadOrCreate();

            var unlocks = new UnlockService(unlockConfig);
            var wallet = new WalletService();
            var lives = new LivesService(economyConfig, _time);
            var chests = new ChestService(starsChestConfig, levelsChestConfig, wallet, _time);
            var bank = new BankService(bankConfig);
            var battlepass = new BattlepassService(battlepassConfig, _time, wallet);
            var streak = new WinStreakService();
            var ads = new AdsPolicyService();

            _meta = new MetaFacade(_saveSystem, unlocks, lives, wallet, chests, bank, battlepass, streak, ads, _time);

            // Apply pending level result if returned from Game
            if (SceneFlow.PendingLevelResult != null)
            {
                _meta.ApplyLevelResult(SceneFlow.PendingLevelResult);

                // Example: show interstitial after win (your ad system later)
                // if (SceneFlow.PendingLevelResult.outcome == LevelOutcome.Win &&
                //     _meta.ShouldShowInterstitialAfterWin(SceneFlow.PendingLevelResult.levelIndex))
                // {
                //     // Show interstitial here
                // }
            }

            _meta.Tick();
        }

        public void OnClickStart()
        {
            if (!_meta.CanStartGame())
            {
                Debug.Log("Can't start: no lives.");
                return;
            }

            var cfg = _meta.BuildRunConfig(boost1Selected, boost2Selected);
            SceneFlow.StartGame(cfg);
        }

        public void HardResetSave()
        {
            _saveSystem.HardReset();
            _meta.Tick();
        }
    }
}
