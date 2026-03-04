using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class BattlepassRewardItemView : MonoBehaviour
    {
        [SerializeField] private Transform iconParent;
        [SerializeField] private TMP_Text valueText;    // "x500" или "2h"
        [SerializeField] private GameObject checkMark;  // галка

        private void Awake()
        {
            for (int i = 0; i < iconParent.childCount; i++) 
            {
                iconParent.transform.GetChild(i).gameObject.SetActive(false);
            }
        }

        public void SetIcon(int spr)
        {
            if (!iconParent) return;
            iconParent.transform.GetChild(spr).gameObject.SetActive(true);
        }

        public void SetValue(string txt)
        {
            if (valueText) valueText.text = txt ?? "";
        }

        public void SetClaimed(bool claimed)
        {
            if (checkMark) checkMark.SetActive(claimed);
        }
    }
}