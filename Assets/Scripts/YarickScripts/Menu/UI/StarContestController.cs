// StarContestController.cs
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    /// <summary>
    /// Логика + отображение данных Star Contest.
    /// Анимация открытия/закрытия попапа делается в StarContestPopupUi.
    /// </summary>
    public sealed class StarContestController : MonoBehaviour
    {
        [Serializable]
        private sealed class PlayerFieldView
        {
            public GameObject root;

            [Header("Texts")]
            public TMP_Text placeText;
            public TMP_Text nameText;
            public TMP_Text starsText;

            public Image bgImage; // <-- добавь

            [Header("Images")]
            public Image avatarImg;
            public Image avatarFrameImg;
        }

        [Header("Root")]
        [SerializeField] private MenuRoot menuRoot; // можно оставить null, если всегда делаешь Bind(root)
        private MenuRoot _root;
        private StarContestPopupUi _popup;

        [Header("Header UI")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text timerText2;

        [Header("Kef Images (x1,x2,x4,x6,x10)")]
        [SerializeField] private Image[] kefImgs;
        [SerializeField] private Sprite kefActiveSpr;
        [SerializeField] private Sprite kefDefSpr;
        [SerializeField] private TMP_FontAsset fontActive;
        [SerializeField] private TMP_FontAsset fontDef;
        [SerializeField] private Color colorActive;
        [SerializeField] private Color colorDef;

        [Header("Player Fields (Top 7)")]
        [SerializeField] private PlayerFieldView[] top7Fields;

        [SerializeField] private Sprite playerSpr;
        [SerializeField] private Sprite DefSpr;

        [Header("Extra Player Field (rank > 7)")]
        [SerializeField] private PlayerFieldView extraPlayerField;

        [Header("Avatars")]
        [SerializeField] private Sprite[] avatarSprites;       // index = avatarId
        [SerializeField] private Sprite[] avatarFrameSprites;  // index = avatarFrameId

        [Header("Buttons")]
        [SerializeField] private Button closeBtn;
        [SerializeField] private Button infoBtn;

        [Header("Info Popup")]
        [SerializeField] private PopupTween infoPopupTween; // твой PopupTween
        [SerializeField] private GameObject infoLabelRoot;  // корень инфо-попапа (если нужно включать)

        private float _timerAcc;
        private bool _bound;

        private void Reset()
        {
            if (menuRoot == null) menuRoot = FindFirstObjectByType<MenuRoot>();
        }

        private void Awake()
        {
            if (closeBtn != null) closeBtn.onClick.AddListener(OnCloseClicked);
            if (infoBtn != null) infoBtn.onClick.AddListener(OnInfoClicked);

            // safety: скрыть extra
            SetFieldActive(extraPlayerField, false);
        }

        private void OnDestroy()
        {
            if (closeBtn != null) closeBtn.onClick.RemoveListener(OnCloseClicked);
            if (infoBtn != null) infoBtn.onClick.RemoveListener(OnInfoClicked);
        }

        private void OnEnable()
        {
            _timerAcc = 0f;

            // если не делаешь Bind() из PopupUi, можно работать так:
            if (!_bound && menuRoot != null)
            {
                Bind(menuRoot, null);
            }
        }

        private void OnDisable()
        {
            _timerAcc = 0f;
        }

        /// <summary>
        /// Вызывай из StarContestPopupUi.Show(root).
        /// </summary>
        public void Bind(MenuRoot root, StarContestPopupUi popup)
        {
            _root = root;
            _popup = popup;
            _bound = true;

            // Дернуть мету, чтобы обновился сезон/награды/симуляция
            if (_root != null && _root.Meta != null)
                _root.Meta.OnStarContestOpened();

            RefreshAll();
        }

        private void Update()
        {
            if (_root == null) return;
            if (_root.Meta == null) return;

            _timerAcc += Time.unscaledDeltaTime;
            if (_timerAcc >= 1f)
            {
                _timerAcc = 0f;
                RefreshTimerOnly();
            }
        }

        // -----------------------
        // Rendering
        // -----------------------

        private void RefreshAll()
        {
            var snap = GetSnapshotSafe();
            ApplyTimer(snap);
            ApplyKef(snap.Multiplier);
            ApplyLeaderboard(snap);
        }

        private void RefreshTimerOnly()
        {
            var snap = GetSnapshotSafe();
            ApplyTimer(snap);
        }

        private Meta.Services.StarContestSnapshot GetSnapshotSafe()
        {
            if (_root == null || _root.Meta == null)
                return default;

            return _root.Meta.GetStarContestSnapshot();
        }

        private void ApplyTimer(Meta.Services.StarContestSnapshot snap)
        {
            if (timerText == null) return;

            var rem = snap.Remaining;
            if (rem <= TimeSpan.Zero)
            {
                timerText.text = "0h 0m";
                timerText2.text = "0h 0m";
                return;
            }

            int hours = (int)Math.Floor(rem.TotalHours);
            int mins = rem.Minutes;

            timerText.text = $"{hours}h {mins}m";
            timerText2.text = $"{hours}h {mins}m";
        }

        private void ApplyKef(int multiplier)
        {
            if (kefImgs == null || kefImgs.Length == 0) return;

            int idx = MultToIndex(multiplier);

            for (int i = 0; i < kefImgs.Length; i++)
            {
                var img = kefImgs[i];
                if (img == null) continue;

                if (i == idx)
                {
                    img.sprite = kefActiveSpr;
                    img.transform.GetChild(0).GetComponent<TMP_Text>().color = colorActive;
                    img.transform.GetChild(0).GetComponent<TMP_Text>().font = fontActive;
                }
                else
                {
                    img.sprite = kefDefSpr;
                    img.transform.GetChild(0).GetComponent<TMP_Text>().color = colorDef;
                    img.transform.GetChild(0).GetComponent<TMP_Text>().font = fontDef;
                }
            }
        }

        private int MultToIndex(int mult)
        {
            // order must be: x1, x2, x4, x6, x10
            if (mult <= 1) return 0;
            if (mult <= 2) return 1;
            if (mult <= 4) return 2;
            if (mult <= 6) return 3;
            return 4;
        }

        private void ApplyLeaderboard(Meta.Services.StarContestSnapshot snap)
        {
            // hide all first
            if (top7Fields != null)
            {
                for (int i = 0; i < top7Fields.Length; i++)
                    SetFieldActive(top7Fields[i], false);
            }

            SetFieldActive(extraPlayerField, false);

            var top = snap.Top;
            if (top == null || top.Length == 0) return;

            int topSlots = top7Fields != null ? top7Fields.Length : 0;
            int shown = Mathf.Min(7, Mathf.Min(top.Length, topSlots));

            // fill top 7
            for (int i = 0; i < shown; i++)
            {
                var f = top7Fields[i];
                SetFieldActive(f, true);

                var e = top[i];

                ApplyEntryToField(
                    f,
                    place: i + 1,
                    name: e.nickName,
                    stars: e.stars,
                    avatarId: e.avatarId,
                    frameId: e.avatarFrameId
                );

                // ⭐ если это игрок — меняем фон PlayerField
                if (f.bgImage != null)
                {
                    f.bgImage.sprite = e.isPlayer ? playerSpr : DefSpr;
                }
            }

            // if player rank > 7 => show extra player field
            if (snap.PlayerRank > 7)
            {
                SetFieldActive(extraPlayerField, true);

                string playerName = "You";
                int playerAvatarId = 0;
                int playerFrameId = 0;

                // Берем из сейва профайл (как у тебя в LeaderboardController делается)
                try
                {
                    var save = _root.Meta.Save;
                    if (save != null)
                    {
                        playerName = save.profile.characterName;
                        playerAvatarId = save.profile.avatarId;
                        playerFrameId = save.profile.frameId;
                    }
                }
                catch
                {
                    // ignore
                }

                ApplyEntryToField(
                    extraPlayerField,
                    place: snap.PlayerRank,
                    name: playerName,
                    stars: snap.PlayerStars,
                    avatarId: playerAvatarId,
                    frameId: playerFrameId
                );
            }
            else
            {
                SetFieldActive(extraPlayerField, false);
            }
        }

        private void ApplyEntryToField(PlayerFieldView field, int place, string name, int stars, int avatarId, int frameId)
        {
            if (field == null) return;

            if (field.placeText != null) field.placeText.text = place.ToString();
            if (field.nameText != null) field.nameText.text = name ?? "";
            if (field.starsText != null) field.starsText.text = stars.ToString();

            if (field.avatarImg != null)
                field.avatarImg.sprite = GetSpriteSafe(avatarSprites, avatarId);

            if (field.avatarFrameImg != null)
                field.avatarFrameImg.sprite = GetSpriteSafe(avatarFrameSprites, frameId);
        }

        private Sprite GetSpriteSafe(Sprite[] arr, int idx)
        {
            if (arr == null || arr.Length == 0) return null;
            if (idx < 0 || idx >= arr.Length) idx = 0;
            return arr[idx];
        }

        private void SetFieldActive(PlayerFieldView f, bool active)
        {
            if (f == null || f.root == null) return;
            if (f.root.activeSelf != active) f.root.SetActive(active);
        }

        // -----------------------
        // Buttons
        // -----------------------

        private void OnCloseClicked()
        {
            // закрыть инфо если надо
            if (infoLabelRoot != null) infoLabelRoot.SetActive(false);

            if (_popup != null)
            {
                _popup.Hide();
                return;
            }

            // fallback
            gameObject.SetActive(false);
        }

        private void OnInfoClicked()
        {
            if (infoLabelRoot != null && !infoLabelRoot.activeSelf)
                infoLabelRoot.SetActive(true);

            if (infoPopupTween != null)
                infoPopupTween.PlayShow();
        }

        // Вешай на кнопку "X" у инфо-лейбла
        public void CloseInfo()
        {
            if (infoPopupTween == null)
            {
                if (infoLabelRoot != null) infoLabelRoot.SetActive(false);
                return;
            }

            infoPopupTween.PlayHide(() =>
            {
                if (infoLabelRoot != null) infoLabelRoot.SetActive(false);
            });
        }
    }
}