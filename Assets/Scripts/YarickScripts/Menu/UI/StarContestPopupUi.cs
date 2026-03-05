using System;
using UnityEngine;

namespace Menu.UI
{
    public sealed class StarContestPopupUi : MonoBehaviour
    {
        [SerializeField] private StarContestController controller;
        [SerializeField] private StarContestPopupUiTween tween; // ниже дам класс (или можешь в этот же файл)
        [SerializeField] private GameObject root; // можно this.gameObject если не надо

        private MenuRoot _menuRoot;
        private bool _isShown;

        private void Awake()
        {
            if (root == null) root = gameObject;
            if (root != null) root.SetActive(false);
        }

        public void Show(MenuRoot rootRef)
        {
            if (rootRef == null) return;

            _menuRoot = rootRef;

            if (root != null && !root.activeSelf)
                root.SetActive(true);

            _isShown = true;

            if (controller != null)
                controller.Bind(_menuRoot, this); // прокидываем root + ссылку на popup (для Close)

            if (tween != null)
                tween.PlayShow();
        }

        public void Hide()
        {
            if (!_isShown) return;
            _isShown = false;

            if (tween != null)
            {
                tween.PlayHide(() =>
                {
                    if (root != null) root.SetActive(false);
                });
            }
            else
            {
                if (root != null) root.SetActive(false);
            }
        }
    }
}