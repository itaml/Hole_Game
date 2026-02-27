using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.RemoteConfig
{
    /// <summary>
    /// Wrapper around Firebase Remote Config.
    /// If Firebase SDK is present, define FIREBASE.
    /// </summary>
    public sealed class RemoteConfigService
    {
        private bool _ready;

        // Local fallback overrides (useful for editor testing without Firebase)
        private readonly Dictionary<string, object> _local = new();

        public bool IsReady => _ready;

        public void Initialize()
        {
#if FIREBASE
            Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                var status = task.Result;
                if (status == Firebase.DependencyStatus.Available)
                {
                    _ready = true;
                    Debug.Log("[RemoteConfig] Firebase ready");
                    SetDefaults();
                    TryFetchAndActivate();
                }
                else
                {
                    _ready = false;
                    Debug.LogWarning("[RemoteConfig] Firebase dependencies not available: " + status);
                }
            });
#else
            _ready = true;
            Debug.Log("[RemoteConfig] FIREBASE define not set. RemoteConfig will use local defaults only.");
            SetDefaults();
#endif
        }

        private void SetDefaults()
        {
            // Put HERE the keys you want to be able to tweak without an update.
            // Examples:
            //  - ads_interstitial_every_win (bool) : show after each win (true)
            //  - ads_interstitial_unlock_level (int) : (10)
            //  - economy_start_lives (int)
            SetDefault("ads_interstitial_unlock_level", 10);
            SetDefault("ads_interstitial_after_win", true);
            SetDefault("rc_example_multiplier", 1.0f);
        }

        public void SetDefault(string key, int value) => _local[key] = value;
        public void SetDefault(string key, bool value) => _local[key] = value;
        public void SetDefault(string key, float value) => _local[key] = value;
        public void SetDefault(string key, string value) => _local[key] = value;

        public void TryFetchAndActivate()
        {
#if FIREBASE
            if (!_ready) return;

            var rc = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance;

            // Fetch every 30 minutes in release; immediate in editor.
            var cache = Application.isEditor ? TimeSpan.Zero : TimeSpan.FromMinutes(30);

            rc.FetchAsync(cache).ContinueWith(fetchTask =>
            {
                if (fetchTask.IsFaulted)
                {
                    Debug.LogWarning("[RemoteConfig] Fetch failed: " + fetchTask.Exception);
                    return;
                }

                rc.ActivateAsync().ContinueWith(actTask =>
                {
                    if (actTask.IsFaulted)
                        Debug.LogWarning("[RemoteConfig] Activate failed: " + actTask.Exception);
                    else
                        Debug.Log("[RemoteConfig] Activated");
                });
            });
#endif
        }

        public int GetInt(string key, int fallback = 0)
        {
#if FIREBASE
            if (_ready)
            {
                try { return (int)Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(key).LongValue; }
                catch { /* ignored */ }
            }
#endif
            if (_local.TryGetValue(key, out var v))
            {
                if (v is int iv) return iv;
                if (v is long lv) return (int)lv;
                if (v is float fv) return (int)fv;
                if (v is double dv) return (int)dv;
                if (v is string sv && int.TryParse(sv, out var p)) return p;
            }
            return fallback;
        }

        public bool GetBool(string key, bool fallback = false)
        {
#if FIREBASE
            if (_ready)
            {
                try { return Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(key).BooleanValue; }
                catch { /* ignored */ }
            }
#endif
            if (_local.TryGetValue(key, out var v))
            {
                if (v is bool bv) return bv;
                if (v is int iv) return iv != 0;
                if (v is long lv) return lv != 0;
                if (v is string sv && bool.TryParse(sv, out var p)) return p;
            }
            return fallback;
        }

        public float GetFloat(string key, float fallback = 0f)
        {
#if FIREBASE
            if (_ready)
            {
                try { return (float)Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(key).DoubleValue; }
                catch { /* ignored */ }
            }
#endif
            if (_local.TryGetValue(key, out var v))
            {
                if (v is float fv) return fv;
                if (v is double dv) return (float)dv;
                if (v is int iv) return iv;
                if (v is long lv) return lv;
                if (v is string sv && float.TryParse(sv, out var p)) return p;
            }
            return fallback;
        }

        public string GetString(string key, string fallback = "")
        {
#if FIREBASE
            if (_ready)
            {
                try { return Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue; }
                catch { /* ignored */ }
            }
#endif
            if (_local.TryGetValue(key, out var v))
                return v?.ToString() ?? fallback;
            return fallback;
        }
    }
}
