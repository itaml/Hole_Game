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

        public void OnWin(int starsEarned, int coins, int battlepassItems, int buff1, int buff2, int buff3, int buff4)
        {
            if (!_initialized || _ended) return;
            ReturnToMenu(LevelOutcome.Win, starsEarned, coins, battlepassItems, buff1, buff2, buff3, buff4);
        }

        public void OnLose(int coins, int buff1, int buff2, int buff3, int buff4)
        {
            if (!_initialized || _ended) return;
            ReturnToMenu(LevelOutcome.Lose, 0, coins, 0, buff1, buff2, buff3, buff4);
        }

        private void ReturnToMenu(LevelOutcome outcome, int stars, int coins, int bpItems, int buff1, int buff2, int buff3, int buff4)
        {
            _ended = true;

            var result = new LevelResult
            {
                levelIndex = _cfg.levelIndex,
                outcome = outcome,

                starsEarned = (outcome == LevelOutcome.Win) ? Mathf.Max(0, stars) : 0,
                coinsResult = Mathf.Max(0, coins),
                battlepassItemsCollected = Mathf.Max(0, bpItems),
                buff1Count = Mathf.Max(0, buff1),
                buff2Count = Mathf.Max(0, buff2),
                buff3Count = Mathf.Max(0, buff3),
                buff4Count = Mathf.Max(0, buff4),
            };

            Debug.Log(
                $"ReturnToMenu: outcome={outcome} level={result.levelIndex} " +
                $"stars={result.starsEarned} wallet+={result.coinsResult} bpItems={result.battlepassItemsCollected} " +
                $"buffsCount=({result.buff1Count},{result.buff1Count},{result.buff1Count},{result.buff1Count})"
            );

            SceneFlow.ReturnToMenu(result);
        }
    }
}