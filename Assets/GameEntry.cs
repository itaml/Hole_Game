using UnityEngine;
using GameBridge.SceneFlow;
using GameBridge.Contracts;

namespace Game
{
    public sealed class GameEntry : MonoBehaviour
    {
        private RunConfig _cfg;
        private bool _initialized;
        private bool _ended;

        public void Init()
        {
            if (_initialized) return;

            _cfg = SceneFlow.PendingRunConfig;
            if (_cfg == null)
            {
                Debug.LogError("GameEntry.Init: RunConfig is null. Start Game scene from Menu via SceneFlow.StartGame(cfg).");
                return;
            }

            Debug.Log($"Init OK. Level={_cfg.levelIndex} boost1={_cfg.boost1Activated} boost2={_cfg.boost2Activated} bonusSpawn={_cfg.bonusSpawnActive} " +
                $"buff1={_cfg.buff1Count} buff2={_cfg.buff2Count} buff3={_cfg.buff3Count} buff4={_cfg.buff4Count} coins={_cfg.walletCoinsSnapshot}");

            _initialized = true;
        }

        public void OnWin(int starsEarned, int coinsToWallet, int coinsToBank, int battlepassItems, int coinsSpent, int buff1Used, int buff2Used, int buff3Used, int buff4Used)
        {
            if (!_initialized || _ended) return;
            ReturnToMenu(LevelOutcome.Win, starsEarned, coinsToWallet, coinsToBank, battlepassItems, coinsSpent, buff1Used, buff2Used, buff3Used, buff4Used);
        }

        public void OnLose()
        {
            if (!_initialized || _ended) return;
            ReturnToMenu(LevelOutcome.Lose, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        private void ReturnToMenu(LevelOutcome outcome, int stars, int coinsWallet, int coinsBank, int bpItems, int coinsSpent, int buff1Used, int buff2Used, int buff3Used, int buff4Used)
        {
            _ended = true;

            var result = new LevelResult
            {
                levelIndex = _cfg.levelIndex,
                outcome = outcome,

                starsEarned = (outcome == LevelOutcome.Win) ? Mathf.Max(0, stars) : 0,
                coinsEarnedToWallet = Mathf.Max(0, coinsWallet),
                coinsEarnedToBank = Mathf.Max(0, coinsBank),
                battlepassItemsCollected = Mathf.Max(0, bpItems),
                coinsSpentInGame = Mathf.Max(0, coinsSpent),
                buff1Used = Mathf.Max(0, buff1Used),
                buff2Used = Mathf.Max(0, buff2Used),
                buff3Used = Mathf.Max(0, buff3Used),
                buff4Used = Mathf.Max(0, buff4Used),
            };

            Debug.Log(
                $"ReturnToMenu: outcome={outcome} level={result.levelIndex} " +
                $"stars={result.starsEarned} wallet+={result.coinsEarnedToWallet} bank+={result.coinsEarnedToBank} bpItems={result.battlepassItemsCollected} " +
                $"spent={result.coinsSpentInGame} buffsUsed=({result.buff1Used},{result.buff2Used},{result.buff3Used},{result.buff4Used})"
            );

            SceneFlow.ReturnToMenu(result);
        }
    }
}