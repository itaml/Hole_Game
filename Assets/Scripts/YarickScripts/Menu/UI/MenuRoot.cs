using UnityEngine;
using Core.Bootstrap;
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

        public MetaFacade Meta => _meta;

        private void Awake()
        {
            AppServices.EnsureExists();

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
                var r = SceneFlow.PendingLevelResult;
                _meta.ApplyLevelResult(r);

                // Analytics: level outcome
                AnalyticsService.LogEvent("level_complete", ("level", r.levelIndex), ("result", r.outcome == LevelOutcome.Win),("stars", r.starsEarned));
                // Interstitial (LevelPlay/IronSource): show after win when player returns to menu,
                // starting from unlock level (default 10) controlled by your UnlockConfig / policy.
                if (r.outcome == LevelOutcome.Win && _meta.ShouldShowInterstitialAfterWin(r.levelIndex))
                {
                    // Optional RC switch
                    if (AppServices.RemoteConfig.GetBool("ads_interstitial_after_win", true))
                    {
                        AnalyticsService.LogEvent("ad_interstitial_request_after_win");

                        // Requires LevelPlayInterstitialController in your bootstrap scene (DontDestroyOnLoad)
                        if (LevelPlayInterstitialController.Instance != null)
                            LevelPlayInterstitialController.Instance.Show("after_win_menu");
                        else
                            Debug.LogWarning("[Menu] LevelPlayInterstitialController.Instance is null (no interstitial)");
                    }
                }
            }

            _meta.Tick();
        }

        public void OnClickStart(bool boost1Selected, bool boost2Selected)
        {
            if (!_meta.CanStartGame())
            {
                Debug.Log("Can't start: no lives.");
                return;
            }

            var cfg = _meta.BuildRunConfig(boost1Selected, boost2Selected);
            AnalyticsService.LogEvent("level_started", ("level", cfg.levelIndex));
            SceneFlow.StartGame(cfg);
        }

        public void HardResetSave()
        {
            _saveSystem.HardReset();
            _meta.Tick();
        }
    }
}
