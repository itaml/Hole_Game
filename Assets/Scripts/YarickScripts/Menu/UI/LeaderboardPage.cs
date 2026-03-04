using UnityEngine;
using DG.Tweening;
using Core.Configs;

namespace Menu.UI
{
    /// <summary>
    /// Leaderboard page with 2 states:
    /// - Locked: shows Header + LockedMain + Footer (like Collections)
    /// - Unlocked: shows Header + PlayersTopImage + PlayersImage + Footer
    ///
    /// Animation is per-page (DOTween). Tutorial is NOT handled here (Variant A uses pendingStartTutorialId in MetaFacade).
    /// </summary>
    public sealed class LeaderboardPage : UIPageBase
    {
        [Header("Dependencies")]
        [SerializeField] private MenuRoot menuRoot;
        [SerializeField] private UnlockConfig unlockConfig;

        [Header("Common UI")]
        [SerializeField] private CanvasGroup rootCG;
        [SerializeField] private RectTransform header;
        public RectTransform[] buttons;

        [Header("Locked UI (like Collections)")]
        [SerializeField] private GameObject lockedRoot;
        [SerializeField] private RectTransform lockedMain;

        [Header("Unlocked UI (Leaderboard content)")]
        [SerializeField] private GameObject unlockedRoot;
        [SerializeField] private RectTransform playersTopImage;
        [SerializeField] private RectTransform playersImage;

        [Header("Anim")]
        [SerializeField] private float inDelayFooter = 0.08f;
        [SerializeField] private float inDelayPlayersImage = 0.05f;

        [SerializeField] private Vector2 headerOffset = new(0, 60);
        [SerializeField] private Vector2 mainOffset = new(0, -80);
        [SerializeField] private Vector2 footerOffset = new(0, -120);

        [SerializeField] private Ease inEase = Ease.OutCubic;
        [SerializeField] private Ease outEase = Ease.InCubic;

        private Vector2 _headerPos, _footerPos, _lockedMainPos, _playersTopPos, _playersPos;
        private bool _prepared;

        private void Awake()
        {
            if (header) _headerPos = header.anchoredPosition;

            if (lockedMain) _lockedMainPos = lockedMain.anchoredPosition;
            if (playersTopImage) _playersTopPos = playersTopImage.anchoredPosition;
            if (playersImage) _playersPos = playersImage.anchoredPosition;

            // If designer forgot to set CanvasGroup, try to find
            if (rootCG == null) rootCG = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            RefreshStateInstant();

            // старт сезона + симуляция при первом открытии
            if (menuRoot != null && menuRoot.Meta != null)
                menuRoot.Meta.OnLeaderboardOpened();
        }

        private bool IsUnlocked()
        {
            if (menuRoot == null || unlockConfig == null) return false;

            var meta = menuRoot.Meta;
            if (meta == null || meta.Save == null) return false;

            // currentLevel = "next level" index (as in your comment)
            // unlock at N => currentLevel >= N
            return meta.Save.progress.currentLevel >= unlockConfig.leaderboardUnlockLevel;
        }

        private void SetState(bool unlocked)
        {
            if (lockedRoot) lockedRoot.SetActive(!unlocked);
            if (unlockedRoot) unlockedRoot.SetActive(unlocked);
        }

        /// <summary>
        /// Instantly puts UI into correct locked/unlocked state without animation.
        /// Useful when enabling page or when state might change.
        /// </summary>
        private void RefreshStateInstant()
        {
            bool unlocked = IsUnlocked();
            SetState(unlocked);

            if (rootCG != null)
            {
                // If this page is enabled due to PrepareShow, alpha is controlled there.
                // Here we do not force alpha=1 because page might be hidden.
            }
        }

        public override void PrepareShow()
        {
            base.PrepareShow();
            Kill();

            bool unlocked = IsUnlocked();
            SetState(unlocked);

            // Block input until animation completes
            if (rootCG != null)
            {
                rootCG.alpha = 0f;
                rootCG.interactable = false;
                rootCG.blocksRaycasts = false;
            }

            // Common start positions
            if (header) header.anchoredPosition = _headerPos + headerOffset;

            // State-specific start positions
            if (!unlocked)
            {
                if (lockedMain) lockedMain.anchoredPosition = _lockedMainPos + mainOffset;
            }
            else
            {
                if (playersTopImage) playersTopImage.anchoredPosition = _playersTopPos + mainOffset;
                if (playersImage) playersImage.anchoredPosition = _playersPos + mainOffset;
            }

            _prepared = true;
        }

        public override Sequence PlayIn()
        {
            if (!_prepared) PrepareShow();
            Kill();

            bool unlocked = IsUnlocked();

            seq = DOTween.Sequence();

            if (rootCG != null)
                seq.Join(rootCG.DOFade(1f, inDuration));

            if (header != null)
                seq.Join(header.DOAnchorPos(_headerPos, inDuration).SetEase(inEase));

            for (int i = 0; i < buttons.Length; i++)
            {
                var b = buttons[i];
                var p = b.anchoredPosition;
                b.anchoredPosition = p + new Vector2(0, -30);

                seq.Insert(0.12f + i * 0.05f, b.DOAnchorPos(p, 0.2f).SetEase(Ease.OutBack));
            }

            if (!unlocked)
            {
                if (lockedMain != null)
                    seq.Join(lockedMain.DOAnchorPos(_lockedMainPos, inDuration).SetEase(inEase));
            }
            else
            {
                if (playersTopImage != null)
                    seq.Join(playersTopImage.DOAnchorPos(_playersTopPos, inDuration).SetEase(inEase));

                if (playersImage != null)
                    seq.Insert(inDelayPlayersImage,
                        playersImage.DOAnchorPos(_playersPos, inDuration).SetEase(inEase));
            }

            seq.OnComplete(() =>
            {
                if (rootCG != null)
                {
                    rootCG.interactable = true;
                    rootCG.blocksRaycasts = true;
                }
            });

            return seq;
        }

        public override Sequence PlayOut()
        {
            Kill();

            if (rootCG != null)
            {
                rootCG.interactable = false;
                rootCG.blocksRaycasts = false;
            }

            bool unlocked = IsUnlocked();

            seq = DOTween.Sequence();

            if (rootCG != null)
                seq.Join(rootCG.DOFade(0f, outDuration));

            if (header != null)
                seq.Join(header.DOAnchorPos(_headerPos + headerOffset, outDuration).SetEase(outEase));

            if (!unlocked)
            {
                if (lockedMain != null)
                    seq.Join(lockedMain.DOAnchorPos(_lockedMainPos + mainOffset, outDuration).SetEase(outEase));
            }
            else
            {
                if (playersTopImage != null)
                    seq.Join(playersTopImage.DOAnchorPos(_playersTopPos + mainOffset, outDuration).SetEase(outEase));

                if (playersImage != null)
                    seq.Join(playersImage.DOAnchorPos(_playersPos + mainOffset, outDuration).SetEase(outEase));
            }

            return seq;
        }

        public override void PrepareHideInstant()
        {
            Kill();
            _prepared = false;

            if (rootCG != null)
            {
                rootCG.alpha = 0f;
                rootCG.interactable = false;
                rootCG.blocksRaycasts = false;
            }

            base.PrepareHideInstant();
        }
    }
}