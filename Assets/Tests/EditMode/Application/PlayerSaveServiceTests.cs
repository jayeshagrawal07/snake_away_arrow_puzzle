using System.Collections.Generic;
using NUnit.Framework;
using SnakeEscape.Application;
using UnityEngine;
using UnityEngine.TestTools;

namespace SnakeEscape.Tests
{
    [TestFixture]
    public class PlayerSaveServiceTests
    {
        private sealed class MemorySaveStore : IPlayerSaveStore
        {
            private readonly Dictionary<string, int> ints = new Dictionary<string, int>();
            private readonly Dictionary<string, string> strings = new Dictionary<string, string>();

            public bool HasKey(string key) => ints.ContainsKey(key) || strings.ContainsKey(key);
            public int GetInt(string key, int defaultValue) => ints.TryGetValue(key, out int value) ? value : defaultValue;
            public string GetString(string key, string defaultValue) => strings.TryGetValue(key, out string value) ? value : defaultValue;
            public void SetInt(string key, int value) => ints[key] = value;
            public void SetString(string key, string value) => strings[key] = value;
            public void Save() { }
        }

        [Test]
        public void LoadOrMigrate_UsesDefaultsWhenNoSaveExists()
        {
            var service = new PlayerSaveService(new MemorySaveStore());

            PlayerSaveData data = service.LoadOrMigrate();

            Assert.AreEqual(1, data.currentLevel);
            Assert.AreEqual(1, data.hints);
            Assert.AreEqual(1, data.gridGuides);
            Assert.AreEqual(760, data.coins);
            Assert.IsTrue(data.hapticsEnabled);
            Assert.IsTrue(data.soundEffectsEnabled);
        }

        [Test]
        public void LoadOrMigrate_PreservesLegacyProgressWithoutFreshStartMarker()
        {
            var store = new MemorySaveStore();
            store.SetInt("SnakePuzzle_Level", 12);
            store.SetInt("SnakePuzzle_Hints", 4);
            store.SetInt("SnakePuzzle_Grids", 2);
            store.SetInt("SnakePuzzle_Coins", 1350);
            store.SetInt("SnakePuzzle_ChallengeRuns", 3);
            store.SetInt("SnakePuzzle_ChallengeUnlocked", 1);
            store.SetInt("SnakePuzzle_Haptics", 0);
            var service = new PlayerSaveService(store);

            PlayerSaveData data = service.LoadOrMigrate();

            Assert.AreEqual(12, data.currentLevel);
            Assert.AreEqual(4, data.hints);
            Assert.AreEqual(2, data.gridGuides);
            Assert.AreEqual(1350, data.coins);
            Assert.AreEqual(3, data.challengeRunCount);
            Assert.IsTrue(data.challengeUnlocked);
            Assert.IsFalse(data.hapticsEnabled);
            Assert.IsTrue(store.HasKey("SnakePuzzle_Save_v1"));
            Assert.AreEqual(1, store.GetInt("SnakePuzzle_FreshStart_V8", 0));
        }

        [Test]
        public void LoadOrMigrate_NormalizesNegativeLegacyValues()
        {
            var store = new MemorySaveStore();
            store.SetInt("SnakePuzzle_Level", -5);
            store.SetInt("SnakePuzzle_Hints", -1);
            store.SetInt("SnakePuzzle_Grids", -2);
            store.SetInt("SnakePuzzle_Coins", -10);
            store.SetInt("SnakePuzzle_ChallengeRuns", -3);
            var service = new PlayerSaveService(store);

            PlayerSaveData data = service.LoadOrMigrate();

            Assert.AreEqual(1, data.currentLevel);
            Assert.AreEqual(0, data.hints);
            Assert.AreEqual(0, data.gridGuides);
            Assert.AreEqual(0, data.coins);
            Assert.AreEqual(0, data.challengeRunCount);
        }

        [Test]
        public void LoadOrMigrate_PreservesMaximumCoinValue()
        {
            var store = new MemorySaveStore();
            store.SetInt("SnakePuzzle_Coins", int.MaxValue);
            var service = new PlayerSaveService(store);

            PlayerSaveData data = service.LoadOrMigrate();
            PlayerSaveData reloaded = service.LoadOrMigrate();

            Assert.AreEqual(int.MaxValue, data.coins);
            Assert.AreEqual(int.MaxValue, reloaded.coins);
        }

        [Test]
        public void Save_RoundTripsLevelAndResourcesAcrossServiceInstances()
        {
            var store = new MemorySaveStore();
            var service = new PlayerSaveService(store);
            service.Save(new PlayerSaveData
            {
                currentLevel = 14,
                hints = 3,
                gridGuides = 2,
                coins = 1250,
                challengeRunCount = 4,
                eraserFreeUseLevel = -4,
                challengeUnlocked = true,
                hapticsEnabled = false,
                soundEffectsEnabled = false
            });

            PlayerSaveData reloaded = new PlayerSaveService(store).LoadOrMigrate();

            Assert.AreEqual(14, reloaded.currentLevel);
            Assert.AreEqual(3, reloaded.hints);
            Assert.AreEqual(2, reloaded.gridGuides);
            Assert.AreEqual(1250, reloaded.coins);
            Assert.AreEqual(4, reloaded.challengeRunCount);
            Assert.AreEqual(-4, reloaded.eraserFreeUseLevel);
            Assert.IsTrue(reloaded.challengeUnlocked);
            Assert.IsFalse(reloaded.hapticsEnabled);
            Assert.IsFalse(reloaded.soundEffectsEnabled);
        }

        [Test]
        public void LoadOrMigrate_UpgradesVersionOneSaveAndEnablesSoundEffectsByDefault()
        {
            var store = new MemorySaveStore();
            var legacyVersion = new PlayerSaveData
            {
                schemaVersion = 1,
                currentLevel = 14,
                hints = 3,
                gridGuides = 2,
                coins = 1250,
                challengeRunCount = 4,
                challengeUnlocked = true,
                hapticsEnabled = false
            };
            legacyVersion.checksum = CalculateVersionOneChecksum(legacyVersion);
            store.SetString("SnakePuzzle_Save_v1", JsonUtility.ToJson(legacyVersion));
            var service = new PlayerSaveService(store);

            PlayerSaveData migrated = service.LoadOrMigrate();
            PlayerSaveData persisted = JsonUtility.FromJson<PlayerSaveData>(
                store.GetString("SnakePuzzle_Save_v1", string.Empty));

            Assert.AreEqual(14, migrated.currentLevel);
            Assert.IsTrue(migrated.soundEffectsEnabled);
            Assert.AreEqual(3, persisted.schemaVersion);
            Assert.IsTrue(persisted.soundEffectsEnabled);
            Assert.AreEqual(1, store.GetInt("SnakePuzzle_SoundEffects", 0));
        }

        [Test]
        public void LoadOrMigrate_UpgradesVersionTwoSaveAndDefaultsFreeEraserUsage()
        {
            var store = new MemorySaveStore();
            var legacyVersion = new PlayerSaveData
            {
                schemaVersion = 2,
                currentLevel = 14,
                hints = 3,
                gridGuides = 2,
                coins = 1250,
                challengeRunCount = 4,
                challengeUnlocked = true,
                hapticsEnabled = false,
                soundEffectsEnabled = false
            };
            legacyVersion.checksum = CalculateVersionTwoChecksum(legacyVersion);
            store.SetString("SnakePuzzle_Save_v1", JsonUtility.ToJson(legacyVersion));

            PlayerSaveData migrated = new PlayerSaveService(store).LoadOrMigrate();
            PlayerSaveData persisted = JsonUtility.FromJson<PlayerSaveData>(
                store.GetString("SnakePuzzle_Save_v1", string.Empty));

            Assert.AreEqual(14, migrated.currentLevel);
            Assert.AreEqual(0, migrated.eraserFreeUseLevel);
            Assert.IsFalse(migrated.soundEffectsEnabled);
            Assert.AreEqual(3, persisted.schemaVersion);
            Assert.AreEqual(0, persisted.eraserFreeUseLevel);
        }

        [Test]
        public void LoadOrMigrate_FallsBackToLegacyValuesWhenVersionedJsonIsInvalid()
        {
            var store = new MemorySaveStore();
            store.SetString("SnakePuzzle_Save_v1", "not valid save json");
            store.SetInt("SnakePuzzle_Level", 8);
            store.SetInt("SnakePuzzle_Coins", 420);
            var service = new PlayerSaveService(store);
            LogAssert.Expect(LogType.Warning, "[PlayerSaveService] Versioned save data was invalid; falling back to legacy PlayerPrefs.");

            PlayerSaveData data = service.LoadOrMigrate();

            Assert.AreEqual(8, data.currentLevel);
            Assert.AreEqual(420, data.coins);
            Assert.AreNotEqual("not valid save json", store.GetString("SnakePuzzle_Save_v1", string.Empty));
        }

        [Test]
        public void LoadOrMigrate_FallsBackToLegacyValuesWhenVersionedChecksumIsInvalid()
        {
            var store = new MemorySaveStore();
            store.SetString("SnakePuzzle_Save_v1", JsonUtility.ToJson(new PlayerSaveData
            {
                schemaVersion = 1,
                currentLevel = 99,
                hints = 7,
                gridGuides = 5,
                coins = 9000,
                challengeRunCount = 8,
                challengeUnlocked = true,
                hapticsEnabled = false,
                checksum = 0
            }));
            store.SetInt("SnakePuzzle_Level", 14);
            store.SetInt("SnakePuzzle_Hints", 3);
            store.SetInt("SnakePuzzle_Grids", 2);
            store.SetInt("SnakePuzzle_Coins", 1250);
            store.SetInt("SnakePuzzle_ChallengeRuns", 4);
            store.SetInt("SnakePuzzle_ChallengeUnlocked", 1);
            store.SetInt("SnakePuzzle_Haptics", 0);
            var service = new PlayerSaveService(store);
            LogAssert.Expect(LogType.Warning, "[PlayerSaveService] Versioned save data was invalid; falling back to legacy PlayerPrefs.");

            PlayerSaveData data = service.LoadOrMigrate();

            Assert.AreEqual(14, data.currentLevel);
            Assert.AreEqual(3, data.hints);
            Assert.AreEqual(2, data.gridGuides);
            Assert.AreEqual(1250, data.coins);
            Assert.AreEqual(4, data.challengeRunCount);
            Assert.IsTrue(data.challengeUnlocked);
            Assert.IsFalse(data.hapticsEnabled);
            Assert.IsTrue(data.soundEffectsEnabled);
            Assert.IsNotEmpty(store.GetString("SnakePuzzle_Save_v1", string.Empty));
        }

        [Test]
        public void LoadOrMigrate_ExplicitResetReplacesExistingProgressWithDefaults()
        {
            var store = new MemorySaveStore();
            store.SetInt("SnakePuzzle_Level", 25);
            store.SetInt("SnakePuzzle_Coins", 9000);
            var service = new PlayerSaveService(store);

            PlayerSaveData data = service.LoadOrMigrate(resetToDefaults: true);

            Assert.AreEqual(1, data.currentLevel);
            Assert.AreEqual(760, data.coins);
        }

        private static int CalculateVersionOneChecksum(PlayerSaveData data)
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

        private static int CalculateVersionTwoChecksum(PlayerSaveData data)
        {
            unchecked
            {
                int checksum = CalculateVersionOneChecksum(data);
                checksum = checksum * 31 + (data.soundEffectsEnabled ? 1 : 0);
                return checksum;
            }
        }
    }
}
