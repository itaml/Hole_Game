using Menu.UI;
using UnityEngine;
using UnityEngine.UI;

public class OptionsPopupUi : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject rootObject;
    [SerializeField] private Button closeButton;

    [SerializeField] private PopupTween tween;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);

        if (tween == null) tween = GetComponent<PopupTween>();

        if (rootObject == null)
            rootObject = gameObject;
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
    }

    public void Show()
    {
        if (rootObject != null) rootObject.SetActive(true);
        else gameObject.SetActive(true);

        tween?.PlayShow(); // добавить
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
