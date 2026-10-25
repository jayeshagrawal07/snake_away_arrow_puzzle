using System.Collections.Generic;
using NUnit.Framework;
using SnakeEscape.Application;
using SnakeEscape.Domain;
using UnityEngine;

namespace SnakeEscape.Tests
{
    [TestFixture]
    public class LevelServiceTests
    {
        private readonly List<LevelDefinition> levelsToDestroy = new List<LevelDefinition>();

        [TearDown]
        public void TearDown()
        {
            foreach (LevelDefinition level in levelsToDestroy)
                Object.DestroyImmediate(level);
            levelsToDestroy.Clear();
        }

        [Test]
        public void LoadCampaign_SortsAssetsAndSupportsIndexAndIdLookup()
        {
            LevelDefinition later = CreateLevel("level_2", 2);
            LevelDefinition earlier = CreateLevel("level_1", 1);
            var service = new LevelService();

            service.LoadCampaign(new[] { later, earlier });

            Assert.AreEqual(2, service.LevelCount);
            Assert.AreSame(earlier, service.GetLevel(0));
            Assert.AreSame(later, service.GetLevel(1));
            Assert.AreSame(earlier, service.GetLevelById("level_1"));
        }

        [Test]
        public void LoadCampaign_RejectsNullAssetCollection()
        {
            var service = new LevelService();

            Assert.Throws<System.ArgumentNullException>(() => service.LoadCampaign(null));
        }

        private LevelDefinition CreateLevel(string id, int sortOrder)
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = id;
            level.SortOrder = sortOrder;
            levelsToDestroy.Add(level);
            return level;
        }
    }
}
