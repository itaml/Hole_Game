using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Bootstrap
{
    /// <summary>
    /// Scene-agnostic services container (DontDestroyOnLoad).
    /// Put this on a prefab in your first scene (Menu), or call EnsureExists() from MenuRoot.
    /// Game and Menu can access Analytics / RemoteConfig via AppServices.* statics.
    /// </summary>
    public sealed class AppServices : MonoBehaviour
    {
        private static AppServices _instance;

        public static AppServices Instance
        {
            get
            {
                if (_instance == null) EnsureExists();
                return _instance;
            }
        }

        public static RemoteConfig.RemoteConfigService RemoteConfig => Instance._remoteConfig;
        public static Monetization.Ads.InterstitialAdService InterstitialAds => Instance._interstitialAds;
        public static Monetization.IAP.IAPService IAP => Instance._iap;

        [SerializeField] private bool dontDestroyOnLoad = true;

        private RemoteConfig.RemoteConfigService _remoteConfig;
        private Monetization.Ads.InterstitialAdService _interstitialAds;
        private Monetization.IAP.IAPService _iap;

        public static void EnsureExists()
        {
            if (_instance != null) return;

            var existing = FindFirstObjectByType<AppServices>();
            if (existing != null)
            {
                _instance = existing;
                return;
            }

            var go = new GameObject("[AppServices]");
            _instance = go.AddComponent<AppServices>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            _remoteConfig = new RemoteConfig.RemoteConfigService();
            _interstitialAds = new Monetization.Ads.InterstitialAdService();
            _iap = new Monetization.IAP.IAPService();

            _remoteConfig.Initialize();

            _interstitialAds.Initialize();
            _iap.Initialize();
        }

        private void OnApplicationPause(bool pause)
        {
            if (!pause)
            {
                // refresh RC when coming back from background (optional)
                _remoteConfig.TryFetchAndActivate();
            }
        }
    }
}
