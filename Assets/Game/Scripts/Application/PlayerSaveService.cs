using System;
using UnityEngine;

namespace SnakeEscape.Application
{
    public interface IPlayerSaveStore
    {
        bool HasKey(string key);
        int GetInt(string key, int defaultValue);
        string GetString(string key, string defaultValue);
        void SetInt(string key, int value);
        void SetString(string key, string value);
        void Save();
    }

    [Serializable]
    public sealed class PlayerSaveData
    {
        public int schemaVersion;
        public int currentLevel;
        public int hints;
        public int gridGuides;
        public int coins;
        public int challengeRunCount;
        public bool challengeUnlocked;
        public bool hapticsEnabled;
        public int checksum;
    }

    public sealed class PlayerSaveService
    {
        private const int CurrentSchemaVersion = 1;
        private const string VersionedSaveKey = "SnakePuzzle_Save_v1";
        private const string FreshStartKey = "SnakePuzzle_FreshStart_V8";
        private const string LevelKey = "SnakePuzzle_Level";
        private const string HintsKey = "SnakePuzzle_Hints";
        private const string GridsKey = "SnakePuzzle_Grids";
        private const string CoinsKey = "SnakePuzzle_Coins";
        private const string ChallengeRunsKey = "SnakePuzzle_ChallengeRuns";
        private const string ChallengeUnlockedKey = "SnakePuzzle_ChallengeUnlocked";
        private const string HapticsKey = "SnakePuzzle_Haptics";

        private readonly IPlayerSaveStore store;

        public PlayerSaveService(IPlayerSaveStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public PlayerSaveData LoadOrMigrate(bool resetToDefaults = false)
        {
            if (resetToDefaults)
            {
                var defaults = CreateDefaults();
                Save(defaults);
                return defaults;
            }

            string json = store.GetString(VersionedSaveKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                if (TryReadVersioned(json, out PlayerSaveData versioned))
                    return versioned;

                Debug.LogWarning("[PlayerSaveService] Versioned save data was invalid; falling back to legacy PlayerPrefs.");
            }

            var migrated = HasLegacySaveData() ? ReadLegacySaveData() : CreateDefaults();
            Save(migrated);
            return migrated;
        }

        public void Save(PlayerSaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            Normalize(data);
            data.schemaVersion = CurrentSchemaVersion;
            data.checksum = CalculateChecksum(data);

            store.SetString(VersionedSaveKey, JsonUtility.ToJson(data));
            store.SetInt(LevelKey, data.currentLevel);
            store.SetInt(HintsKey, data.hints);
            store.SetInt(GridsKey, data.gridGuides);
            store.SetInt(CoinsKey, data.coins);
            store.SetInt(ChallengeRunsKey, data.challengeRunCount);
            store.SetInt(ChallengeUnlockedKey, data.challengeUnlocked ? 1 : 0);
            store.SetInt(HapticsKey, data.hapticsEnabled ? 1 : 0);
            store.SetInt(FreshStartKey, 1);
            store.Save();
        }

        public static PlayerSaveData CreateDefaults()
        {
            return new PlayerSaveData
            {
                schemaVersion = CurrentSchemaVersion,
                currentLevel = 1,
                hints = 1,
                gridGuides = 1,
                coins = 760,
                challengeRunCount = 0,
                challengeUnlocked = false,
                hapticsEnabled = true
            };
        }

        private bool HasLegacySaveData()
        {
            return store.HasKey(LevelKey)
                || store.HasKey(HintsKey)
                || store.HasKey(GridsKey)
                || store.HasKey(CoinsKey)
                || store.HasKey(ChallengeRunsKey)
                || store.HasKey(ChallengeUnlockedKey)
                || store.HasKey(HapticsKey);
        }

        private PlayerSaveData ReadLegacySaveData()
        {
            var data = new PlayerSaveData
            {
                currentLevel = store.GetInt(LevelKey, 1),
                hints = store.GetInt(HintsKey, 1),
                gridGuides = store.GetInt(GridsKey, 1),
                coins = store.GetInt(CoinsKey, 760),
                challengeRunCount = store.GetInt(ChallengeRunsKey, 0),
                challengeUnlocked = store.GetInt(ChallengeUnlockedKey, 0) == 1,
                hapticsEnabled = store.GetInt(HapticsKey, 1) == 1
            };
            Normalize(data);
            return data;
        }

        private static bool TryReadVersioned(string json, out PlayerSaveData data)
        {
            data = null;
            try
            {
                data = JsonUtility.FromJson<PlayerSaveData>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (data == null
                || data.schemaVersion != CurrentSchemaVersion
                || data.currentLevel < 1
                || data.hints < 0
                || data.gridGuides < 0
                || data.coins < 0
                || data.challengeRunCount < 0
                || data.checksum != CalculateChecksum(data))
            {
                data = null;
                return false;
            }

            return true;
        }

        private static void Normalize(PlayerSaveData data)
        {
            data.currentLevel = Math.Max(1, data.currentLevel);
            data.hints = Math.Max(0, data.hints);
            data.gridGuides = Math.Max(0, data.gridGuides);
            data.coins = Math.Max(0, data.coins);
            data.challengeRunCount = Math.Max(0, data.challengeRunCount);
        }

        private static int CalculateChecksum(PlayerSaveData data)
        {
            unchecked
            {
                int checksum = 17;
                checksum = checksum * 31 + data.schemaVersion;
                checksum = checksum * 31 + data.currentLevel;
                checksum = checksum * 31 + data.hints;
                checksum = checksum * 31 + data.gridGuides;
                checksum = checksum * 31 + data.coins;
                checksum = checksum * 31 + data.challengeRunCount;
                checksum = checksum * 31 + (data.challengeUnlocked ? 1 : 0);
                checksum = checksum * 31 + (data.hapticsEnabled ? 1 : 0);
                return checksum;
            }
        }
    }

    public sealed class UnityPlayerSaveStore : IPlayerSaveStore
    {
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public int GetInt(string key, int defaultValue) => PlayerPrefs.GetInt(key, defaultValue);
        public string GetString(string key, string defaultValue) => PlayerPrefs.GetString(key, defaultValue);
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public void Save() => PlayerPrefs.Save();
    }
}
