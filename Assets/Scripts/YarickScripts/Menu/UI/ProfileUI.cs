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
    [SerializeField] private GameObject profileLabel;
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

    private void Start()
    {
        SetData();

        if (profileOpenBtn != null) profileOpenBtn.onClick.AddListener(OnClickProfileOpen);
        if (editOpenBtn != null) editOpenBtn.onClick.AddListener(OnClickEditOpen);
        if (profileCloseBtn != null) profileCloseBtn.onClick.AddListener(OnClickProfileClose);
        if (editCloseBtn != null) editCloseBtn.onClick.AddListener(OnClickEditClose);
        if (editSaveBtn != null) editSaveBtn.onClick.AddListener(OnClickEditSave);
        if (switchBtn != null) switchBtn.onClick.AddListener(OnClickSwitchBtn);
    }

    private void OnDestroy()
    {
        if (profileOpenBtn != null) profileOpenBtn.onClick.RemoveListener(OnClickProfileOpen);
        if (editOpenBtn != null) editOpenBtn.onClick.RemoveListener(OnClickEditOpen);
        if (profileCloseBtn != null) profileCloseBtn.onClick.RemoveListener(OnClickProfileClose);
        if (editCloseBtn != null) editCloseBtn.onClick.AddListener(OnClickEditClose);
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

    private void OnClickProfileOpen()
    {
        profileLabel.SetActive(true);
    }

    private void OnClickProfileClose()
    {
        profileLabel.SetActive(false);
    }

    private void OnClickEditOpen()
    {
        profileLabel.SetActive(false);
        editLabel.SetActive(true);
    }

    private void OnClickEditClose()
    {
        profileLabel.SetActive(true);
        editLabel.SetActive(false);
    }

    private void OnClickEditSave()
    {
        profileLabel.SetActive(true);
        editLabel.SetActive(false);

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
