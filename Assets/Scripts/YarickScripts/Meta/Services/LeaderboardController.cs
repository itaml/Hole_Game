using Meta.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.UI
{
    public sealed class LeaderboardController : MonoBehaviour
    {
        [Header("Deps")]
        [SerializeField] private MenuRoot menuRoot;

        [Header("Timer")]
        [SerializeField] private TMP_Text timerText; // "6d 16h"
        [SerializeField] private TMP_Text timerText2; // "6d 16h"

        [Header("Player")]
        [SerializeField] private TMP_Text playerRankText;  // "#5321"
        [SerializeField] private TMP_Text playerScoreText; // "9"
        [SerializeField] private TMP_Text playerNameText;  // "#5321"

        [Header("Top-7 Text")]
        [SerializeField] private TMP_Text[] topNick;   // size 7
        [SerializeField] private TMP_Text[] topScore;  // size 7

        [Header("Top-7 Avatars")]
        [SerializeField] private Image playerAvatarImg;
        [SerializeField] private Image playerAvatarFrameImg;
        [SerializeField] private Image[] avatarImgs;        // size 7
        [SerializeField] private Image[] avatarFramesImgs;  // size 7
        [SerializeField] private Sprite[] avatarSpr;        // indices 0..8
        [SerializeField] private Sprite[] avatarFramesSpr;  // indices 0..8 (или сколько у тебя)

        private void OnEnable()
        {
            menuRoot?.Meta?.OnLeaderboardOpened();
        }

        private void Update()
        {
            RefreshTimerOnly();
            Refresh();
        }

        public void Refresh()
        {
            if (menuRoot?.Meta == null) return;

            LeaderboardSnapshot snap = menuRoot.Meta.GetLeaderboardSnapshot();

            SetTimer(snap.Remaining);

            if (playerNameText) playerNameText.text = menuRoot.Meta.Save.profile.characterName;
            if (playerRankText) playerRankText.text = $"{snap.PlayerRank}";
            if (playerScoreText) playerScoreText.text = snap.PlayerScore.ToString();

            playerAvatarImg.sprite = avatarSpr[menuRoot.Meta.Save.profile.avatarId];
            playerAvatarFrameImg.sprite = avatarFramesSpr[menuRoot.Meta.Save.profile.frameId];

            for (int i = 0; i < 7; i++)
            {
                if (i >= snap.Top.Length) break;

                var e = snap.Top[i];

                if (topNick != null && i < topNick.Length && topNick[i] != null)
                    topNick[i].text = e.nickName;

                if (topScore != null && i < topScore.Length && topScore[i] != null)
                    topScore[i].text = e.score.ToString();

                // avatar sprite
                if (avatarImgs != null && i < avatarImgs.Length && avatarImgs[i] != null)
                {
                    avatarImgs[i].sprite = GetSafeSprite(avatarSpr, e.avatarId);
                    avatarImgs[i].enabled = avatarImgs[i].sprite != null;
                }

                // frame sprite
                if (avatarFramesImgs != null && i < avatarFramesImgs.Length && avatarFramesImgs[i] != null)
                {
                    avatarFramesImgs[i].sprite = GetSafeSprite(avatarFramesSpr, e.avatarFrameId);
                    avatarFramesImgs[i].enabled = avatarFramesImgs[i].sprite != null;
                }
            }
        }

        private void RefreshTimerOnly()
        {
            if (menuRoot?.Meta == null) return;
            var snap = menuRoot.Meta.GetLeaderboardSnapshot();
            SetTimer(snap.Remaining);
        }

        private void SetTimer(System.TimeSpan remaining)
        {
            int d = Mathf.Max(0, remaining.Days);
            int h = Mathf.Max(0, remaining.Hours);
            if (timerText) timerText.text = $"{d}d {h}h";
            if (timerText2) timerText2.text = $"{d}d {h}h";
        }

        private static Sprite GetSafeSprite(Sprite[] arr, int id)
        {
            if (arr == null || arr.Length == 0) return null;
            if (id < 0) id = 0;
            if (id >= arr.Length) id = id % arr.Length; // чтобы не падать если frameId выходит за массив
            return arr[id];
        }
    }
}