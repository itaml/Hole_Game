using Core.Configs;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Menu.UI
{
    public sealed class LevelsChestAutoOpenUi : MonoBehaviour
    {
        [SerializeField] private MenuRoot root;

        [Header("Popups")]
        [SerializeField] private RewardPopupRouter popupRouter;

        private int _lastSeenRewardHash;

        private void Reset()
        {
            root = FindFirstObjectByType<MenuRoot>();
        }

        private void Update()
        {
            TryConsumeAndShowGrantedRewards();
        }

        private void TryConsumeAndShowGrantedRewards()
        {
            if (root != null && root.SuppressAutoRewardPopups)
                return;

            //showED levels reward
            Reward[] rewards = root.Meta.ConsumeGrantedRewards();
            if (rewards == null || rewards.Length == 0) return;

            int hash = ComputeRewardsHash(rewards);
            if (hash == _lastSeenRewardHash) return;
            _lastSeenRewardHash = hash;

            popupRouter?.ShowRewards(rewards);
        }

        private int ComputeRewardsHash(Reward[] rewards)
        {
            unchecked
            {
                int h = 17;
                for (int i = 0; i < rewards.Length; i++)
                {
                    var r = rewards[i];
                    if (r == null) continue;

                    h = h * 31 + r.coins;

                    h = h * 31 + r.buff1Amount;
                    h = h * 31 + r.buff2Amount;
                    h = h * 31 + r.buff3Amount;
                    h = h * 31 + r.buff4Amount;

                    h = h * 31 + r.boost1Amount;
                    h = h * 31 + r.boost2Amount;

                    h = h * 31 + r.infiniteLivesMinutes;
                    h = h * 31 + r.infiniteBoost1Minutes;
                    h = h * 31 + r.infiniteBoost2Minutes;
                }
                return h;
            }
        }
    }
}