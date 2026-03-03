using Menu.UI;
using UnityEngine;

public class HideIfAdsRemoved : MonoBehaviour
{
    [SerializeField] private MenuRoot menuRoot;         // где Save
    [SerializeField] private ShopController shop;       // чтобы обновл€ть когда покупка прошла
    [SerializeField] private GameObject targetToHide;   // сама карточка (если null Ч this.gameObject)

    private void Awake()
    {
        if (targetToHide == null) targetToHide = gameObject;
    }

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        var save = menuRoot?.Meta?.Save;
        if (save == null) return;

        bool hide = save.profile.adsRemoved;
        targetToHide.SetActive(!hide);
    }
}