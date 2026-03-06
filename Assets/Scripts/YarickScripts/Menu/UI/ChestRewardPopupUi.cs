using Core.Configs;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class ChestRewardPopupUi : MonoBehaviour
    {
        [System.Serializable]
        private sealed class RewardIcons
        {
            public Sprite coins;
            public Sprite buff1;
            public Sprite buff2;
            public Sprite buff3;
            public Sprite buff4;
            public Sprite boost1;
            public Sprite boost2;
            public Sprite infiniteLives;
            public Sprite infiniteBoost1;
            public Sprite infiniteBoost2;
        }

        private enum RewardKind
        {
            None = 0,
            Coins = 1,
            Buff1 = 2,
            Buff2 = 3,
            Buff3 = 4,
            Buff4 = 5,
            Boost1 = 6,
            Boost2 = 7,
            InfiniteLives = 8,
            InfiniteBoost1 = 9,
            InfiniteBoost2 = 10
        }

        [Header("Root")]
        [SerializeField] private GameObject rootObject;
        [SerializeField] private CanvasGroup overlayCanvasGroup;
        [SerializeField] private RectTransform contentRoot;

        [Header("Tap area")]
        [SerializeField] private Button tapAnywhereButton;

        [Header("Closed chest state")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Image chestImg;
        [SerializeField] private Sprite closedChestSprite;

        [Header("Opened chest state")]
        [Tooltip("Если назначен этот объект, то при открытии chestImg будет выключен, а chestOpen включен.")]
        [SerializeField] private GameObject chestOpen;
        [SerializeField] private Sprite openedChestSprite;

        [Header("Reward state")]
        [SerializeField] private GameObject rewardImg;
        [SerializeField] private Image rewardIcon;
        [SerializeField] private TMP_Text rewardCount;
        [SerializeField] private GameObject titleImage;

        [Header("Buttons")]
        [SerializeField] private Button x2Button;
        [SerializeField] private Button continueButton;

        [Header("Reward icons")]
        [SerializeField] private RewardIcons icons;

        [Header("Timings")]
        [SerializeField] private float showFadeTime = 0.20f;
        [SerializeField] private float popupIntroTime = 0.28f;
        [SerializeField] private float chestShakeTime = 0.28f;
        [SerializeField] private float chestOpenTime = 0.35f;
        [SerializeField] private float rewardAppearTime = 0.32f;
        [SerializeField] private float buttonsAppearTime = 0.24f;
        [SerializeField] private float hideTime = 0.18f;

        [Header("Scale multipliers")]
        [SerializeField] private float popupStartScaleMultiplier = 0.88f;
        [SerializeField] private float chestStartScaleMultiplier = 0.82f;
        [SerializeField] private float rewardStartScaleMultiplier = 0.50f;
        [SerializeField] private float titleImageStartScaleMultiplier = 0.75f;
        [SerializeField] private float buttonStartScaleMultiplier = 0.80f;
        [SerializeField] private float closeScaleMultiplier = 0.92f;

        private RewardPopupQueue _queue;
        private bool _isOpened;
        private bool _isClosing;
        private Sequence _sequence;

        private Vector3 _contentRootOriginalScale;
        private Vector3 _chestImgOriginalScale;
        private Vector3 _chestOpenOriginalScale;
        private Vector3 _rewardImgOriginalScale;
        private Vector3 _titleImageOriginalScale;
        private Vector3 _x2ButtonOriginalScale;
        private Vector3 _continueButtonOriginalScale;

        private void Awake()
        {
            if (rootObject == null)
                rootObject = gameObject;

            CacheOriginalScales();

            if (tapAnywhereButton != null)
                tapAnywhereButton.onClick.AddListener(OnTapToOpen);

            if (x2Button != null)
                x2Button.onClick.AddListener(Close);

            if (continueButton != null)
                continueButton.onClick.AddListener(Close);

            rootObject.SetActive(false);
        }

        public void Init(RewardPopupQueue queue)
        {
            _queue = queue;
        }

        public void Show(Reward reward)
        {
            _isOpened = false;
            _isClosing = false;

            _sequence?.Kill();

            CacheOriginalScales();
            PrepareInitialState();
            ApplyRewardVisual(reward);

            rootObject.SetActive(true);

            _sequence = DOTween.Sequence().SetUpdate(true);

            if (overlayCanvasGroup != null)
            {
                overlayCanvasGroup.alpha = 0f;
                overlayCanvasGroup.blocksRaycasts = true;
                overlayCanvasGroup.interactable = true;
                _sequence.Join(overlayCanvasGroup.DOFade(1f, showFadeTime).SetEase(Ease.OutQuad));
            }

            if (contentRoot != null)
            {
                contentRoot.localScale = _contentRootOriginalScale * popupStartScaleMultiplier;
                _sequence.Join(contentRoot.DOScale(_contentRootOriginalScale, popupIntroTime).SetEase(Ease.OutBack));
            }

            if (chestImg != null)
            {
                chestImg.transform.localScale = _chestImgOriginalScale * chestStartScaleMultiplier;
                _sequence.Join(chestImg.transform.DOScale(_chestImgOriginalScale, popupIntroTime).SetEase(Ease.OutBack));
            }
        }

        private void CacheOriginalScales()
        {
            _contentRootOriginalScale = ReadSafeScale(contentRoot != null ? contentRoot.localScale : Vector3.one);
            _chestImgOriginalScale = ReadSafeScale(chestImg != null ? chestImg.transform.localScale : Vector3.one);
            _chestOpenOriginalScale = ReadSafeScale(chestOpen != null ? chestOpen.transform.localScale : Vector3.one);
            _rewardImgOriginalScale = ReadSafeScale(rewardImg != null ? rewardImg.transform.localScale : Vector3.one);
            _titleImageOriginalScale = ReadSafeScale(titleImage != null ? titleImage.transform.localScale : Vector3.one);
            _x2ButtonOriginalScale = ReadSafeScale(x2Button != null ? x2Button.transform.localScale : Vector3.one);
            _continueButtonOriginalScale = ReadSafeScale(continueButton != null ? continueButton.transform.localScale : Vector3.one);
        }

        private static Vector3 ReadSafeScale(Vector3 value)
        {
            const float eps = 0.0001f;

            if (Mathf.Abs(value.x) < eps && Mathf.Abs(value.y) < eps && Mathf.Abs(value.z) < eps)
                return Vector3.one;

            return new Vector3(
                Mathf.Abs(value.x) < eps ? 1f : value.x,
                Mathf.Abs(value.y) < eps ? 1f : value.y,
                Mathf.Abs(value.z) < eps ? 1f : value.z
            );
        }

        private void PrepareInitialState()
        {
            if (overlayCanvasGroup != null)
            {
                overlayCanvasGroup.alpha = 0f;
                overlayCanvasGroup.blocksRaycasts = true;
                overlayCanvasGroup.interactable = true;
            }

            if (contentRoot != null)
                contentRoot.localScale = _contentRootOriginalScale;

            if (titleText != null)
                titleText.gameObject.SetActive(true);

            if (titleImage != null)
            {
                titleImage.SetActive(false);
                titleImage.transform.localScale = _titleImageOriginalScale;
            }

            if (rewardImg != null)
            {
                rewardImg.SetActive(false);
                rewardImg.transform.localScale = _rewardImgOriginalScale;
            }

            if (x2Button != null)
            {
                x2Button.gameObject.SetActive(false);
                x2Button.transform.localScale = _x2ButtonOriginalScale;
                x2Button.interactable = false;
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
                continueButton.transform.localScale = _continueButtonOriginalScale;
                continueButton.interactable = false;
            }

            if (tapAnywhereButton != null)
            {
                tapAnywhereButton.gameObject.SetActive(true);
                tapAnywhereButton.interactable = true;
            }

            if (chestImg != null)
            {
                chestImg.gameObject.SetActive(true);
                if (closedChestSprite != null)
                    chestImg.sprite = closedChestSprite;

                chestImg.transform.localScale = _chestImgOriginalScale;
                chestImg.transform.localRotation = Quaternion.identity;
                chestImg.transform.localPosition = Vector3.zero;
            }

            if (chestOpen != null)
            {
                chestOpen.SetActive(false);
                chestOpen.transform.localScale = _chestOpenOriginalScale;
                chestOpen.transform.localRotation = Quaternion.identity;
                chestOpen.transform.localPosition = Vector3.zero;
            }
        }

        private void ApplyRewardVisual(Reward reward)
        {
            RewardKind kind = GetRewardKind(reward);

            if (rewardIcon != null)
                rewardIcon.sprite = GetRewardSprite(kind);

            if (rewardCount != null)
                rewardCount.text = GetRewardText(reward, kind);
        }

        private void OnTapToOpen()
        {
            if (_isOpened || _isClosing)
                return;

            _isOpened = true;

            if (tapAnywhereButton != null)
                tapAnywhereButton.interactable = false;

            // Если игрок тапнул до завершения intro fade,
            // не оставляем фон на промежуточной альфе.
            if (overlayCanvasGroup != null)
                overlayCanvasGroup.alpha = 1f;

            _sequence?.Kill();
            _sequence = DOTween.Sequence().SetUpdate(true);

            Transform chestTransform = GetActiveChestTransform();

            if (chestTransform != null)
            {
                Vector3 baseScale = chestTransform == chestImg?.transform
                    ? _chestImgOriginalScale
                    : _chestOpenOriginalScale;

                chestTransform.localScale = baseScale;
                _sequence.Append(chestTransform.DOPunchScale(baseScale * 0.18f, chestShakeTime, 10, 0.9f));
                _sequence.Join(chestTransform.DOPunchRotation(new Vector3(0f, 0f, 14f), chestShakeTime, 12, 0.85f));
            }

            _sequence.AppendInterval(0.05f);
            _sequence.AppendCallback(SwitchChestToOpenState);

            _sequence.AppendCallback(() =>
            {
                var openedTransform = GetActiveChestTransform();
                if (openedTransform == null) return;

                Vector3 baseScale = openedTransform == chestImg?.transform
                    ? _chestImgOriginalScale
                    : _chestOpenOriginalScale;

                openedTransform.localScale = baseScale * 0.72f;
            });

            _sequence.AppendCallback(() =>
            {
                var openedTransform = GetActiveChestTransform();
                if (openedTransform == null) return;

                Vector3 baseScale = openedTransform == chestImg?.transform
                    ? _chestImgOriginalScale
                    : _chestOpenOriginalScale;

                openedTransform.DOScale(baseScale * 1.06f, chestOpenTime * 0.55f)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            });

            _sequence.AppendInterval(chestOpenTime * 0.55f);

            _sequence.AppendCallback(() =>
            {
                var openedTransform = GetActiveChestTransform();
                if (openedTransform == null) return;

                Vector3 baseScale = openedTransform == chestImg?.transform
                    ? _chestImgOriginalScale
                    : _chestOpenOriginalScale;

                openedTransform.DOScale(baseScale, chestOpenTime * 0.45f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            });

            _sequence.AppendInterval(chestOpenTime * 0.45f);
            _sequence.AppendInterval(0.03f);
            _sequence.AppendCallback(ShowRewardState);
        }

        private Transform GetActiveChestTransform()
        {
            if (chestOpen != null && chestOpen.activeSelf)
                return chestOpen.transform;

            if (chestImg != null && chestImg.gameObject.activeSelf)
                return chestImg.transform;

            return null;
        }

        private void SwitchChestToOpenState()
        {
            if (titleText != null)
                titleText.gameObject.SetActive(false);

            bool hasOpenObject = chestOpen != null;
            bool hasOpenSprite = chestImg != null && openedChestSprite != null;

            if (hasOpenObject)
            {
                if (chestImg != null)
                    chestImg.gameObject.SetActive(false);

                chestOpen.SetActive(true);
                chestOpen.transform.localScale = _chestOpenOriginalScale;
                chestOpen.transform.localRotation = Quaternion.identity;
                chestOpen.transform.localPosition = Vector3.zero;
                return;
            }

            if (hasOpenSprite)
            {
                chestImg.gameObject.SetActive(true);
                chestImg.sprite = openedChestSprite;
                chestImg.transform.localScale = _chestImgOriginalScale;
                chestImg.transform.localRotation = Quaternion.identity;
                chestImg.transform.localPosition = Vector3.zero;
            }
        }

        private void ShowRewardState()
        {
            Sequence s = DOTween.Sequence().SetUpdate(true);

            if (rewardImg != null)
            {
                rewardImg.SetActive(true);
                rewardImg.transform.localScale = _rewardImgOriginalScale * rewardStartScaleMultiplier;
                s.Join(rewardImg.transform.DOScale(_rewardImgOriginalScale, rewardAppearTime).SetEase(Ease.OutBack));
                s.Join(rewardImg.transform.DOPunchScale(_rewardImgOriginalScale * 0.06f, 0.20f, 8, 0.8f));
            }

            if (titleImage != null)
            {
                titleImage.SetActive(true);
                titleImage.transform.localScale = _titleImageOriginalScale * titleImageStartScaleMultiplier;
                s.Join(titleImage.transform.DOScale(_titleImageOriginalScale, 0.26f).SetEase(Ease.OutBack));
            }

            if (x2Button != null)
            {
                x2Button.gameObject.SetActive(true);
                x2Button.transform.localScale = _x2ButtonOriginalScale * buttonStartScaleMultiplier;
                s.Append(x2Button.transform.DOScale(_x2ButtonOriginalScale, buttonsAppearTime).SetEase(Ease.OutBack));
                s.Join(x2Button.transform.DOPunchScale(_x2ButtonOriginalScale * 0.03f, 0.18f, 8, 0.8f));
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.transform.localScale = _continueButtonOriginalScale * buttonStartScaleMultiplier;
                s.Join(continueButton.transform.DOScale(_continueButtonOriginalScale, buttonsAppearTime).SetEase(Ease.OutBack));
                s.Join(continueButton.transform.DOPunchScale(_continueButtonOriginalScale * 0.03f, 0.18f, 8, 0.8f));
            }

            s.OnComplete(() =>
            {
                if (x2Button != null)
                    x2Button.interactable = true;

                if (continueButton != null)
                    continueButton.interactable = true;
            });
        }

        private void Close()
        {
            if (_isClosing)
                return;

            _isClosing = true;
            _sequence?.Kill();

            if (x2Button != null) x2Button.interactable = false;
            if (continueButton != null) continueButton.interactable = false;
            if (tapAnywhereButton != null) tapAnywhereButton.interactable = false;

            Sequence s = DOTween.Sequence().SetUpdate(true);

            if (contentRoot != null)
                s.Join(contentRoot.DOScale(_contentRootOriginalScale * closeScaleMultiplier, hideTime).SetEase(Ease.InBack));

            if (overlayCanvasGroup != null)
                s.Join(overlayCanvasGroup.DOFade(0f, hideTime).SetEase(Ease.InQuad));

            s.OnComplete(() =>
            {
                ResetAnimatedObjectsToOriginalScale();
                rootObject.SetActive(false);
                _queue?.NotifyClosed();
            });
        }

        private void ResetAnimatedObjectsToOriginalScale()
        {
            if (contentRoot != null)
                contentRoot.localScale = _contentRootOriginalScale;

            if (chestImg != null)
            {
                chestImg.transform.localScale = _chestImgOriginalScale;
                chestImg.transform.localRotation = Quaternion.identity;
                chestImg.transform.localPosition = Vector3.zero;
                chestImg.gameObject.SetActive(true);
                if (closedChestSprite != null)
                    chestImg.sprite = closedChestSprite;
            }

            if (chestOpen != null)
            {
                chestOpen.transform.localScale = _chestOpenOriginalScale;
                chestOpen.transform.localRotation = Quaternion.identity;
                chestOpen.transform.localPosition = Vector3.zero;
                chestOpen.SetActive(false);
            }

            if (rewardImg != null)
                rewardImg.transform.localScale = _rewardImgOriginalScale;

            if (titleImage != null)
                titleImage.transform.localScale = _titleImageOriginalScale;

            if (x2Button != null)
                x2Button.transform.localScale = _x2ButtonOriginalScale;

            if (continueButton != null)
                continueButton.transform.localScale = _continueButtonOriginalScale;
        }

        private RewardKind GetRewardKind(Reward reward)
        {
            if (reward == null) return RewardKind.None;

            if (reward.coins > 0) return RewardKind.Coins;
            if (reward.buff1Amount > 0) return RewardKind.Buff1;
            if (reward.buff2Amount > 0) return RewardKind.Buff2;
            if (reward.buff3Amount > 0) return RewardKind.Buff3;
            if (reward.buff4Amount > 0) return RewardKind.Buff4;
            if (reward.boost1Amount > 0) return RewardKind.Boost1;
            if (reward.boost2Amount > 0) return RewardKind.Boost2;
            if (reward.infiniteLivesMinutes > 0) return RewardKind.InfiniteLives;
            if (reward.infiniteBoost1Minutes > 0) return RewardKind.InfiniteBoost1;
            if (reward.infiniteBoost2Minutes > 0) return RewardKind.InfiniteBoost2;

            return RewardKind.None;
        }

        private Sprite GetRewardSprite(RewardKind kind)
        {
            return kind switch
            {
                RewardKind.Coins => icons.coins,
                RewardKind.Buff1 => icons.buff1,
                RewardKind.Buff2 => icons.buff2,
                RewardKind.Buff3 => icons.buff3,
                RewardKind.Buff4 => icons.buff4,
                RewardKind.Boost1 => icons.boost1,
                RewardKind.Boost2 => icons.boost2,
                RewardKind.InfiniteLives => icons.infiniteLives,
                RewardKind.InfiniteBoost1 => icons.infiniteBoost1,
                RewardKind.InfiniteBoost2 => icons.infiniteBoost2,
                _ => null
            };
        }

        private string GetRewardText(Reward reward, RewardKind kind)
        {
            if (reward == null) return string.Empty;

            return kind switch
            {
                RewardKind.Coins => $"+{reward.coins}",
                RewardKind.Buff1 => $"x{reward.buff1Amount}",
                RewardKind.Buff2 => $"x{reward.buff2Amount}",
                RewardKind.Buff3 => $"x{reward.buff3Amount}",
                RewardKind.Buff4 => $"x{reward.buff4Amount}",
                RewardKind.Boost1 => $"x{reward.boost1Amount}",
                RewardKind.Boost2 => $"x{reward.boost2Amount}",
                RewardKind.InfiniteLives => $"{reward.infiniteLivesMinutes}m",
                RewardKind.InfiniteBoost1 => $"{reward.infiniteBoost1Minutes}m",
                RewardKind.InfiniteBoost2 => $"{reward.infiniteBoost2Minutes}m",
                _ => string.Empty
            };
        }
    }
}