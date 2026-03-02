using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class LevelsChestPopupUi : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject rootObject;
        [SerializeField] private Image bar;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button closeButton2;
        [SerializeField] private TMP_Text progressText;

        [SerializeField] private PopupTween tween; // добавить

        private MenuRoot _menu;


        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (closeButton2 != null) closeButton2.onClick.AddListener(Hide);
            if (rootObject == null) rootObject = gameObject;

            if (tween == null) tween = GetComponent<PopupTween>(); // добавить
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
            if (closeButton2 != null) closeButton2.onClick.RemoveListener(Hide);
        }

        public void Show(MenuRoot menu)
        {
            _menu = menu;

            if (rootObject != null) rootObject.SetActive(true);
            else gameObject.SetActive(true);

            tween?.PlayShow(); // добавить

            Render();
        }

        public void Hide()
        {
            _menu = null;

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

        private void Update()
        {
            if (_menu == null || _menu.Meta == null) return;
            Render();
        }

        private void Render()
        {
            var save = _menu.Meta.Save;

            int coins = save.levelsChest.progress;
            int cap = (_menu.levelsChestConfig != null) ? _menu.levelsChestConfig.threshold : 0;

            progressText.text = coins.ToString() + "/" + cap.ToString();

            float fill = 0f;
            if (cap > 0)
                fill = Mathf.Clamp01(coins / (float)cap);

            if (bar != null)
                bar.fillAmount = fill;
        }
    }
}