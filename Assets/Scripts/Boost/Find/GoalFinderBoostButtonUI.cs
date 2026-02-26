using UnityEngine;
using UnityEngine.UI;

public class GoalFinderBoostButtonUI : MonoBehaviour
{
    [SerializeField] private GoalFinderBoost boost;
    [SerializeField] private Image fillImage;
    [SerializeField] private Button button;

    private void Awake()
    {
        if (fillImage != null) fillImage.type = Image.Type.Filled;
    }

    private void Update()
    {
        if (boost == null || fillImage == null) return;

        if (boost.IsActive)
        {
            fillImage.fillAmount = Mathf.Clamp01(boost.Remaining / Mathf.Max(0.001f, boost.Duration));
            if (button) button.interactable = false;
        }
        else
        {
            fillImage.fillAmount = 0f;
            if (button) button.interactable = true;
        }
    }

    public void Click()
    {
        boost?.Activate();
    }
}