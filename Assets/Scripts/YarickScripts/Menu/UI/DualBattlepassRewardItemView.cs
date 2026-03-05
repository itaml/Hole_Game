using TMPro;
using UnityEngine;

namespace Menu.UI
{
    public sealed class DualBattlepassRewardItemView : MonoBehaviour
    {
        [Header("FREE")]
        [SerializeField] private Transform freeIconParent;
        [SerializeField] private TMP_Text freeValueText;
        [SerializeField] private GameObject freeCheckMark;

        [Header("PREMIUM")]
        [SerializeField] private Transform premiumIconParent;
        [SerializeField] private TMP_Text premiumValueText;
        [SerializeField] private GameObject premiumCheckMark;

        [Tooltip("Опционально: затемнение/замок на premium, если не куплен")]
        [SerializeField] private GameObject premiumLockedOverlay;

        private void Awake()
        {
            DeactivateAllChildren(freeIconParent);
            DeactivateAllChildren(premiumIconParent);
        }

        private static void DeactivateAllChildren(Transform parent)
        {
            if (!parent) return;
            for (int i = 0; i < parent.childCount; i++)
                parent.GetChild(i).gameObject.SetActive(false);
        }

        public void SetFree(int iconId, string value, bool claimed)
        {
            DeactivateAllChildren(freeIconParent);
            if (freeIconParent && iconId >= 0 && iconId < freeIconParent.childCount)
                freeIconParent.GetChild(iconId).gameObject.SetActive(true);

            if (freeValueText) freeValueText.text = value ?? "";
            if (freeCheckMark) freeCheckMark.SetActive(claimed);
        }

        public void SetPremium(int iconId, string value, bool claimed, bool premiumActive)
        {
            DeactivateAllChildren(premiumIconParent);
            if (premiumIconParent && iconId >= 0 && iconId < premiumIconParent.childCount)
                premiumIconParent.GetChild(iconId).gameObject.SetActive(true);

            if (premiumValueText) premiumValueText.text = value ?? "";
            if (premiumCheckMark) premiumCheckMark.SetActive(claimed);

            if (premiumLockedOverlay) premiumLockedOverlay.SetActive(!premiumActive);
        }
    }
}