using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class BuffRewardPopupUi : MonoBehaviour
    {
        [SerializeField] private GameObject rootObject;
        [SerializeField] private TMP_Text amountText; // "x3"
        [SerializeField] private Button closeButton;

        [SerializeField] private CanvasGroup cg;
        [SerializeField] private RectTransform panel;

        [SerializeField] PopupTween tween;

        private RewardPopupQueue _queue;

        private void Awake()
        {
            if (rootObject == null) rootObject = gameObject;
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        public void Init(RewardPopupQueue queue) => _queue = queue;

        public void Show(int amount)
        {
            if (amountText != null) amountText.text = $"x{amount}";
            rootObject.SetActive(true);

            tween.PlayShow();
        }

        private void Close()
        {
            tween.PlayHide(() => { rootObject.SetActive(false); _queue.NotifyClosed(); });
        }
    }
}