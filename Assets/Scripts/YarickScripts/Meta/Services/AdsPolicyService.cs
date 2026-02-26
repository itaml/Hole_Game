namespace Meta.Services
{
    public sealed class AdsPolicyService
    {
        /// <summary>Interstitial from level 10 after each win.</summary>
        public bool ShouldShowInterstitial(UnlockService unlocks, int levelIndexJustWon)
        {
            return unlocks.IsInterstitialUnlocked(levelIndexJustWon);
        }
    }
}
