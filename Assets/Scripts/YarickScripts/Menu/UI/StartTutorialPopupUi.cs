using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class StartTutorialPopupUi : MonoBehaviour
    {
        [SerializeField] private GameObject rootObject;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button closeButton2;

        [SerializeField] private PopupTween tween;

        private void Awake()
        {
            if (rootObject == null) rootObject = gameObject;
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (closeButton2 != null) closeButton2.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
            if (closeButton2 != null) closeButton2.onClick.AddListener(Hide);
        }

        public bool IsShown
        {
            get
            {
                if (rootObject != null) return rootObject.activeSelf;
                return gameObject.activeSelf;
            }
        }

        public void Show()
        {
            if (rootObject != null) rootObject.SetActive(true);
            else gameObject.SetActive(true);

            tween?.PlayShow();
        }

        public void Hide()
        {
            if (tween != null)
            {
                tween.PlayHide(() =>
                {
                    if (rootObject != null) rootObject.SetActive(false);
                    else gameObject.SetActive(false);
                });
            }
            else
            {
                if (rootObject != null) rootObject.SetActive(false);
                else gameObject.SetActive(false);
            }
        }
    }
}
