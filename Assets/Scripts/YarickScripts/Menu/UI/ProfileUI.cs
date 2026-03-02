using Menu.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileUI : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private MenuRoot menu;
    [SerializeField] private Image frameImgMain;
    [SerializeField] private Image avatarImgMain;
    [SerializeField] private Sprite[] framesSpr;
    [SerializeField] private Sprite[] avatarsSpr;
    [SerializeField] private Button profileOpenBtn;
    [SerializeField] private Button optionsOpenBtn;
    [SerializeField] private Button optionsCloseBtn;
    [SerializeField] private GameObject profileLabel;
    [SerializeField] private GameObject optionsLabel;
    [SerializeField] private GameObject mallLabel;
    [SerializeField] private Button mallOpenBtn;
    [SerializeField] private Button mallCloseBtn;
    [SerializeField] private Button mallCloseBtn2;
    [Header("Profile")]
    [SerializeField] private Image frameImgProfile;
    [SerializeField] private Image avatarImgProfile;
    [SerializeField] private TMP_Text playerNameProfile;
    [SerializeField] private TMP_Text lvlText;
    [SerializeField] private TMP_Text firstTryWinsText;
    [SerializeField] private TMP_Text longestStreakText;
    [SerializeField] private TMP_Text threeStarsText;
    [SerializeField] private TMP_Text loginDaysText;
    [SerializeField] private Button editOpenBtn;
    [SerializeField] private GameObject editLabel;
    [SerializeField] private Button profileCloseBtn;
    [Header("Edit")]
    [SerializeField] private Image frameImgEdit;
    [SerializeField] private Image avatarImgEdit;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button editCloseBtn;
    [SerializeField] private Button editSaveBtn;
    [SerializeField] private Button switchBtn;
    [SerializeField] private GameObject framesLabel;
    [SerializeField] private GameObject avatarsLabel;
    [SerializeField] private Sprite frameSelectedSpr;
    [SerializeField] private Sprite avatarSelectedSpr;
    [SerializeField] private Image[] avatarInFramesImgs;
    private int avatar, frame;

    [SerializeField] private PopupTween profileTween;
    [SerializeField] private PopupTween optionsTween;
    [SerializeField] private PopupTween editTween;
    [SerializeField] private PopupTween mallTween;

    private void Start()
    {
        SetData();

        if (mallCloseBtn2 != null) mallCloseBtn2.onClick.AddListener(OnClickMallClosed);
        if (mallCloseBtn != null) mallCloseBtn.onClick.AddListener(OnClickMallClosed);
        if (mallOpenBtn != null) mallOpenBtn.onClick.AddListener(OnClickMallOpen);
        if (profileOpenBtn != null) profileOpenBtn.onClick.AddListener(OnClickProfileOpen);
        if (optionsOpenBtn != null) optionsOpenBtn.onClick.AddListener(OnClickOptionsOpen);
        if (optionsCloseBtn != null) optionsCloseBtn.onClick.AddListener(OnClickOptionsClosed);
        if (editOpenBtn != null) editOpenBtn.onClick.AddListener(OnClickEditOpen);
        if (profileCloseBtn != null) profileCloseBtn.onClick.AddListener(OnClickProfileClose);
        if (editCloseBtn != null) editCloseBtn.onClick.AddListener(OnClickEditClose);
        if (editSaveBtn != null) editSaveBtn.onClick.AddListener(OnClickEditSave);
        if (switchBtn != null) switchBtn.onClick.AddListener(OnClickSwitchBtn);
    }

    private void OnDestroy()
    {
        if (mallCloseBtn2 != null) mallCloseBtn2.onClick.RemoveListener(OnClickMallClosed);
        if (mallCloseBtn != null) mallCloseBtn.onClick.RemoveListener(OnClickMallClosed);
        if (mallOpenBtn != null) mallOpenBtn.onClick.RemoveListener(OnClickMallOpen);
        if (profileOpenBtn != null) profileOpenBtn.onClick.RemoveListener(OnClickProfileOpen);
        if (optionsOpenBtn != null) optionsOpenBtn.onClick.RemoveListener(OnClickOptionsOpen);
        if (optionsCloseBtn != null) optionsCloseBtn.onClick.RemoveListener(OnClickOptionsClosed);
        if (editOpenBtn != null) editOpenBtn.onClick.RemoveListener(OnClickEditOpen);
        if (profileCloseBtn != null) profileCloseBtn.onClick.RemoveListener(OnClickProfileClose);
        if (editCloseBtn != null) editCloseBtn.onClick.RemoveListener(OnClickEditClose);
        if (editSaveBtn != null) editSaveBtn.onClick.RemoveListener(OnClickEditSave);
        if (switchBtn != null) switchBtn.onClick.RemoveListener(OnClickSwitchBtn);
    }
    
    public void SelectAvatar(int i)
    {
        avatar = i;
        avatarImgEdit.sprite = avatarsSpr[avatar];

        for (int f = 0; f < avatarInFramesImgs.Length; f++)
        {
            avatarInFramesImgs[f].sprite = avatarsSpr[avatar];
        }
    }

    public void SelectFrame(int i)
    {
        frame = i;
        frameImgEdit.sprite = framesSpr[frame];
    }

    void SaveData()
    {
        menu.Meta.SetAvatar(avatar);
        menu.Meta.SetFrame(frame);

        menu.Meta.SetCharacterName(nameInput.text);
    }

    void SetData()
    {
        var save = menu.Meta.Save;

        frameImgMain.sprite = framesSpr[menu.Meta.GetFrameId()];
        avatarImgMain.sprite = avatarsSpr[menu.Meta.GetAvatarId()];

        frameImgProfile.sprite = framesSpr[menu.Meta.GetFrameId()];
        avatarImgProfile.sprite = avatarsSpr[menu.Meta.GetAvatarId()];
        playerNameProfile.text = menu.Meta.GetCharacterName();
        lvlText.text = save.progress.currentLevel.ToString() + " lvl";
        firstTryWinsText.text = save.profile.firstTryWins.ToString("000");
        longestStreakText.text = save.profile.longestWinStreak.ToString("000");
        threeStarsText.text = save.profile.threeStarWins.ToString("000");
        loginDaysText.text = save.profile.loginDaysStreak.ToString("000");

        frameImgEdit.sprite = framesSpr[menu.Meta.GetFrameId()];
        avatarImgEdit.sprite = avatarsSpr[menu.Meta.GetAvatarId()];

        nameInput.text = menu.Meta.GetCharacterName();

        for (int i = 0; i < avatarInFramesImgs.Length; i++)
        {
            avatarInFramesImgs[i].sprite = avatarsSpr[menu.Meta.GetAvatarId()];
        }
    }

    private void ShowLabel(GameObject go, PopupTween tween)
    {
        if (go == null) return;
        go.SetActive(true);
        tween?.PlayShow();
    }

    private void HideLabel(GameObject go, PopupTween tween)
    {
        if (go == null) return;

        if (tween != null)
        {
            tween.PlayHide(() => go.SetActive(false));
        }
        else
        {
            go.SetActive(false);
        }
    }

    private void OnClickMallOpen()
    {
        ShowLabel(mallLabel, mallTween);
    }

    private void OnClickMallClosed()
    {
        HideLabel(mallLabel, mallTween);
    }

    private void OnClickOptionsOpen()
    {
        ShowLabel(optionsLabel, optionsTween);
    }

    private void OnClickOptionsClosed()
    {
        HideLabel(optionsLabel, optionsTween);
    }

    private void OnClickProfileOpen()
    {
        ShowLabel(profileLabel, profileTween);
    }

    private void OnClickProfileClose()
    {
        HideLabel(profileLabel, profileTween);
    }

    private void OnClickEditOpen()
    {
        HideLabel(profileLabel, profileTween);
        ShowLabel(editLabel, editTween);
    }

    private void OnClickEditClose()
    {
        HideLabel(editLabel, editTween);
        ShowLabel(profileLabel, profileTween);
    }

    private void OnClickEditSave()
    {
        HideLabel(editLabel, editTween);
        ShowLabel(profileLabel, profileTween);

        SaveData();
        SetData();
    }

    private void OnClickSwitchBtn()
    {
        if (switchBtn.GetComponent<Image>().sprite == frameSelectedSpr)
        {
            switchBtn.GetComponent<Image>().sprite = avatarSelectedSpr;
            framesLabel.SetActive(false);
            avatarsLabel.SetActive(true);
        }
        else 
        {
            switchBtn.GetComponent<Image>().sprite = frameSelectedSpr;
            framesLabel.SetActive(true);
            avatarsLabel.SetActive(false);
        }
    }
}
