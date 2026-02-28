using TMPro;
using UnityEngine;

namespace Menu.UI
{
    public sealed class LevelsChestAutoOpenUi : MonoBehaviour
    {
        [SerializeField] private MenuRoot root;

        [Header("UI")]
        [SerializeField] private TMP_Text progressText;  // "05/20"

        private void Reset()
        {
            root = FindFirstObjectByType<MenuRoot>();
        }

        private void Update()
        {
            if (root == null || root.Meta == null || root.levelsChestConfig == null) return;

            var save = root.Meta.Save;

            int progress = save.levelsChest.progress;
            int threshold = Mathf.Max(0, root.levelsChestConfig.threshold);

            if (progressText != null)
                progressText.text = $"{progress:00}/{threshold:00}";

            bool ready = threshold > 0 && progress >= threshold;
        }
    }
}