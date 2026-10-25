using System;
using System.IO;
using NUnit.Framework;
using SnakeEscape.Application;
using SnakeEscape.Domain;
using UnityEditor;
using UnityEngine;

namespace SnakeEscape.Tests
{
    [TestFixture]
    public class LevelServiceResourceTests
    {
        [Test]
        public void LoadCampaign_FindsAssetSavedUnderCanonicalResourcesPath()
        {
            string levelId = "resource_test_" + Guid.NewGuid().ToString("N");
            string assetPath = $"Assets/Resources/{LevelService.LevelsResourcePath}/{levelId}.asset";
            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = levelId;
            level.SortOrder = 1;

            try
            {
                Directory.CreateDirectory("Assets/Resources/Levels");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.CreateAsset(level, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                var service = new LevelService();
                service.LoadCampaign();

                Assert.AreSame(Resources.Load<LevelDefinition>($"{LevelService.LevelsResourcePath}/{levelId}"), service.GetLevelById(levelId));
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.SaveAssets();
                UnityEngine.Object.DestroyImmediate(level);
            }
        }
    }
}
