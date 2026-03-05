using Core.Configs;
using TMPro;
using UnityEngine;

namespace Menu.UI
{
    public sealed class StarsChestAutoOpenUi : MonoBehaviour
    {
        [SerializeField] private MenuRoot root;

        [Header("Popups")]
        [SerializeField] private RewardPopupRouter popupRouter;

        // Чтобы не показывать попапы каждый кадр:
        private int _lastSeenRewardHash;

        private void Reset()
        {
            root = FindFirstObjectByType<MenuRoot>();
        }

        private void Update()
        {
            // 🔥 ВАЖНО: авто-открытие уже произошло в MetaFacade.ApplyLevelResult через ChestService,
            // поэтому тут мы только "снимаем" выданные награды и показываем попапы.
            TryConsumeAndShowGrantedRewards();
        }

        private void TryConsumeAndShowGrantedRewards()
        {
            // MetaFacade.ConsumeGrantedRewards() — ты должен был добавить (я писал выше).
            // Он возвращает массив Reward и очищает внутренний список.

            if (root != null && root.SuppressAutoRewardPopups)
                return;

            Reward[] rewards = root.Meta.ConsumeGrantedRewards();
            if (rewards == null || rewards.Length == 0) return;

            // простая защита от случайного повторного показа (если вдруг вызов дважды)
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