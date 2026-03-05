using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Firebase;
using Firebase.Extensions;
using Firebase.RemoteConfig;

using Core.Configs;

namespace Core.RemoteConfig
{
    public sealed class FirebaseConfigsApplier : MonoBehaviour
    {
        [Header("Assign ScriptableObject defaults (same as in MenuRoot)")]
        public EconomyConfig economy;
        public BankConfig bank;
        public ChestConfig starsChest;
        public ChestConfig levelsChest;
        public BattlepassConfig battlepass;
        public BountyConfig bounty;

        [Header("Fetch settings")]
        public bool fetchOnStart = true;

        // В проде обычно 12 часов. В деве можно 0, чтобы изменения сразу прилетали.
        public int minimumFetchIntervalSeconds = 43200; // 12h

        private const string KEY_ECO = "cfg_economy";
        private const string KEY_BANK = "cfg_bank";
        private const string KEY_STARS = "cfg_chest_stars";
        private const string KEY_LEVELS = "cfg_chest_levels";
        private const string KEY_BP = "cfg_battlepass";
        private const string KEY_BOUNTY = "cfg_bounty";

        private async void Start()
        {
            if (!fetchOnStart) return;
            await FetchAndApplySafe();
        }

        public async Task FetchAndApplySafe()
        {
            try
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                                minimumFetchIntervalSeconds = 0;
                #else
                                        minimumFetchIntervalSeconds = 43200; // 12h
                #endif

                // 1) Firebase deps
                var dep = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (dep != DependencyStatus.Available)
                {
                    Debug.LogWarning($"Firebase dependencies not available: {dep}");
                    return;
                }

                // 2) Defaults (чтобы до фетча игра жила на значениях из ScriptableObject)
                var defaults = BuildDefaultsFromSO();
                await FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults);

                var settings = new ConfigSettings
                {
                    MinimumFetchIntervalInMilliseconds =
                        (ulong)(minimumFetchIntervalSeconds * 1000)
                };

                await FirebaseRemoteConfig.DefaultInstance.SetConfigSettingsAsync(settings);

                await FirebaseRemoteConfig.DefaultInstance.FetchAsync();
                await FirebaseRemoteConfig.DefaultInstance.ActivateAsync();

                Debug.Log("Unity platform: " + Application.platform);
                Debug.Log("Firebase AppId: " + FirebaseApp.DefaultInstance.Options.AppId);
                Debug.Log("Firebase ProjectId: " + FirebaseApp.DefaultInstance.Options.ProjectId);

                var info = FirebaseRemoteConfig.DefaultInstance.Info;
                Debug.Log($"RemoteConfig fetch status: {info.LastFetchStatus}, fetchTime: {info.FetchTime}");

                Debug.Log("cfg_chest_levels JSON = " + FirebaseRemoteConfig.DefaultInstance.GetValue("cfg_chest_levels").StringValue);

                // 4) Apply
                ApplyAllFromRemoteConfig();

                Debug.Log("Remote Config: applied successfully.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Remote Config: fetch/apply failed. Using local defaults. Exception: {e}");
            }
        }

        private Dictionary<string, object> BuildDefaultsFromSO()
        {
            // В Remote Config defaults лучше класть строки (JSON),
            // чтобы типы были стабильные.
            return new Dictionary<string, object>
            {
                { KEY_ECO, JsonUtility.ToJson(new EconomyDto(economy)) },
                { KEY_BANK, JsonUtility.ToJson(new BankDto(bank)) },
                { KEY_STARS, JsonUtility.ToJson(new ChestDto(starsChest)) },
                { KEY_LEVELS, JsonUtility.ToJson(new ChestDto(levelsChest)) },
                { KEY_BP, JsonUtility.ToJson(new BattlepassDto(battlepass)) },
            };
        }

        private void ApplyAllFromRemoteConfig()
        {
            ApplyJsonIfValid(KEY_BOUNTY, json => ApplyBounty(json));
            ApplyJsonIfValid(KEY_ECO, json => ApplyEconomy(json));
            ApplyJsonIfValid(KEY_BANK, json => ApplyBank(json));
            ApplyJsonIfValid(KEY_STARS, json => ApplyChest(starsChest, json));
            ApplyJsonIfValid(KEY_LEVELS, json => ApplyChest(levelsChest, json));
            ApplyJsonIfValid(KEY_BP, json => ApplyBattlepass(json));
        }

        private void ApplyJsonIfValid(string key, Action<string> apply)
        {
            var v = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
            var json = v.StringValue;

            if (string.IsNullOrWhiteSpace(json))
                return;

            apply(json);
        }

        // -------- APPLY METHODS --------

        private void ApplyBounty(string json)
        {
            var dto = JsonUtility.FromJson<BountyDto>(json);
            if (dto == null) return;

            bounty.refreshDays = dto.refreshDays;
            bounty.possibleRewards = dto.possibleRewards ?? Array.Empty<Reward>();
        }

        private void ApplyEconomy(string json)
        {
            var dto = JsonUtility.FromJson<EconomyDto>(json);
            if (dto == null) return;

            economy.lifeRestoreSeconds = dto.lifeRestoreSeconds;
            economy.buyLifeCostCoins = dto.buyLifeCostCoins;
        }

        private void ApplyBank(string json)
        {
            var dto = JsonUtility.FromJson<BankDto>(json);
            if (dto == null) return;

            bank.capacity = dto.capacity;
            bank.depositOnWin = dto.depositOnWin;
        }

        private void ApplyChest(ChestConfig target, string json)
        {
            var dto = JsonUtility.FromJson<ChestDto>(json);
            if (dto == null) return;

            target.threshold = dto.threshold;
            target.possibleRewards = dto.possibleRewards ?? Array.Empty<Reward>();
        }

        private void ApplyBattlepass(string json)
        {
            var dto = JsonUtility.FromJson<BattlepassDto>(json);
            if (dto == null) return;

            battlepass.seasonDays = dto.seasonDays;
            battlepass.tiers = dto.tiers ?? Array.Empty<BattlepassTier>();
        }

        // -------- DTOs (под твою структуру) --------

        [Serializable]
        private sealed class BountyDto
        {
            public int refreshDays = 2;
            public Reward[] possibleRewards;

            public BountyDto() { }
            public BountyDto(BountyConfig so)
            {
                if (so == null) return;
                refreshDays = so.refreshDays;
                possibleRewards = so.possibleRewards;
            }
        }

        [Serializable]
        private sealed class EconomyDto
        {
            public int lifeRestoreSeconds = 900;
            public int buyLifeCostCoins = 100;

            public EconomyDto() { }
            public EconomyDto(EconomyConfig so)
            {
                if (so == null) return;
                lifeRestoreSeconds = so.lifeRestoreSeconds;
                buyLifeCostCoins = so.buyLifeCostCoins;
            }
        }

        [Serializable]
        private sealed class BankDto
        {
            public int capacity = 0;
            public int depositOnWin = 50;

            public BankDto() { }
            public BankDto(BankConfig so)
            {
                if (so == null) return;
                capacity = so.capacity;
                depositOnWin = so.depositOnWin;
            }
        }

        [Serializable]
        private sealed class ChestDto
        {
            public int threshold = 20;
            public Reward[] possibleRewards;

            public ChestDto() { }
            public ChestDto(ChestConfig so)
            {
                if (so == null) return;
                threshold = so.threshold;
                possibleRewards = so.possibleRewards;
            }
        }

        [Serializable]
        private sealed class BattlepassDto
        {
            public int seasonDays = 7;
            public BattlepassTier[] tiers;

            public BattlepassDto() { }
            public BattlepassDto(BattlepassConfig so)
            {
                if (so == null) return;
                seasonDays = so.seasonDays;
                tiers = so.tiers;
            }
        }
    }
}