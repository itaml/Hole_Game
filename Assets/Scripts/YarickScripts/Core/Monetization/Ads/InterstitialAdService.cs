using System;
using UnityEngine;

namespace Core.Monetization.Ads
{
    /// <summary>
    /// Interstitial ads wrapper.
    /// Recommended SDK: Google Mobile Ads (AdMob). If you use it, define ADMOB.
    /// </summary>
    public sealed class InterstitialAdService
    {
        private bool _initialized;

#if ADMOB
        private GoogleMobileAds.Api.InterstitialAd _ad;
#endif

        public void Initialize()
        {
#if ADMOB
            if (_initialized) return;

            GoogleMobileAds.Api.MobileAds.Initialize(initStatus =>
            {
                _initialized = true;
                Debug.Log("[Ads] AdMob initialized");
                Load();
            });
#else
            _initialized = true;
            Debug.Log("[Ads] ADMOB define not set. Interstitial will be no-op.");
#endif
        }

        public bool IsReady()
        {
#if ADMOB
            return _ad != null;
#else
            return false;
#endif
        }

        public void Load()
        {
#if ADMOB
            if (!_initialized) return;

            // TODO: replace with your real Ad Unit Ids
#if UNITY_ANDROID
            string adUnitId = "ca-app-pub-3940256099942544/1033173712"; // test
#elif UNITY_IOS
            string adUnitId = "ca-app-pub-3940256099942544/4411468910"; // test
#else
            string adUnitId = "unused";
#endif

            var request = new GoogleMobileAds.Api.AdRequest();

            // New API: InterstitialAd.Load
            GoogleMobileAds.Api.InterstitialAd.Load(adUnitId, request, (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning("[Ads] Failed to load interstitial: " + error);
                    return;
                }

                _ad = ad;
                RegisterEvents(ad);
                Debug.Log("[Ads] Interstitial loaded");
            });
#endif
        }

#if ADMOB
        private void RegisterEvents(GoogleMobileAds.Api.InterstitialAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[Ads] Interstitial closed");
                _ad?.Destroy();
                _ad = null;
                Load();
            };

            ad.OnAdFullScreenContentFailed += (err) =>
            {
                Debug.LogWarning("[Ads] Interstitial failed: " + err);
                _ad?.Destroy();
                _ad = null;
                Load();
            };
        }
#endif

        public void Show(string placement, Action onClosed = null)
        {
#if ADMOB
            if (_ad == null)
            {
                Load();
                onClosed?.Invoke();
                return;
            }

            // Hook close callback (one-shot)
            void HandleClosed()
            {
                onClosed?.Invoke();
            }

            _ad.OnAdFullScreenContentClosed += HandleClosed;
            _ad.OnAdFullScreenContentFailed += (err) => HandleClosed();

            _ad.Show();
#else
            onClosed?.Invoke();
#endif
        }
    }
}
