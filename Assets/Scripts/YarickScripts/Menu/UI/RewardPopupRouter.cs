using Core.Configs;
using Menu.UI;
using System.Collections.Generic;
using UnityEngine;

public sealed class RewardPopupRouter : MonoBehaviour
{
    [SerializeField] private RewardPopupQueue queue;

    [Header("Popups (optional, assign what you have)")]
    [SerializeField] private CoinsRewardPopupUi coinsPopup;

    [SerializeField] private BuffRewardPopupUi buff1Popup;
    [SerializeField] private BuffRewardPopupUi buff2Popup;
    [SerializeField] private BuffRewardPopupUi buff3Popup;
    [SerializeField] private BuffRewardPopupUi buff4Popup;

    // ƒќЅј¬№ (сделай префабы/объекты как дл€ BuffRewardPopupUi)
    [SerializeField] private BuffRewardPopupUi boost1Popup;
    [SerializeField] private BuffRewardPopupUi boost2Popup;

    private void Awake()
    {
        if (queue == null) queue = FindFirstObjectByType<RewardPopupQueue>();

        coinsPopup?.Init(queue);

        buff1Popup?.Init(queue);
        buff2Popup?.Init(queue);
        buff3Popup?.Init(queue);
        buff4Popup?.Init(queue);

        boost1Popup?.Init(queue);
        boost2Popup?.Init(queue);
    }

    public void ShowRewards(IEnumerable<Reward> rewards)
    {
        if (queue == null || rewards == null) return;

        foreach (var r in rewards)
        {
            if (r == null) continue;

            if (r.coins > 0 && coinsPopup != null)
                queue.Enqueue(() => coinsPopup.Show(r.coins));

            if (r.buff1Amount > 0 && buff1Popup != null)
                queue.Enqueue(() => buff1Popup.Show(r.buff1Amount));

            if (r.buff2Amount > 0 && buff2Popup != null)
                queue.Enqueue(() => buff2Popup.Show(r.buff2Amount));

            if (r.buff3Amount > 0 && buff3Popup != null)
                queue.Enqueue(() => buff3Popup.Show(r.buff3Amount));

            if (r.buff4Amount > 0 && buff4Popup != null)
                queue.Enqueue(() => buff4Popup.Show(r.buff4Amount));

            if (r.boost1Amount > 0 && boost1Popup != null)
                queue.Enqueue(() => boost1Popup.Show(r.boost1Amount));

            if (r.boost2Amount > 0 && boost2Popup != null)
                queue.Enqueue(() => boost2Popup.Show(r.boost2Amount));
        }
    }
}