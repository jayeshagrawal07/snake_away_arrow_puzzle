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
    }
}
