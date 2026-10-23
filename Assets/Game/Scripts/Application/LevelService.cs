using System.Collections.Generic;
using SnakeEscape.Domain;
using UnityEngine;

namespace SnakeEscape.Application
{
    /// <summary>
    /// Loads LevelDefinition assets for a given campaign.
    /// In the initial implementation, levels are loaded via Resources.Load.
    /// Later this can be swapped for Addressables.
    ///
    /// Place level assets at: Assets/Game/Data/Levels/
    /// Load path:             "Levels/Level_XXX"
    /// </summary>
    public class LevelService
    {
        private const string LevelsResourcePath = "Levels";

        private List<LevelDefinition> _campaignLevels;

        /// <summary>
        /// Loads all LevelDefinition assets from Resources/Levels/ and sorts by SortOrder.
        /// Call once at startup or scene load.
        /// </summary>
        public void LoadCampaign()
        {
            var assets = Resources.LoadAll<LevelDefinition>(LevelsResourcePath);
            _campaignLevels = new List<LevelDefinition>(assets);
            _campaignLevels.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            Debug.Log($"[LevelService] Loaded {_campaignLevels.Count} level(s) from Resources/{LevelsResourcePath}/");
        }

        public int LevelCount => _campaignLevels?.Count ?? 0;

        public LevelDefinition GetLevel(int index)
        {
            if (_campaignLevels == null || index < 0 || index >= _campaignLevels.Count)
            {
                Debug.LogWarning($"[LevelService] Level index {index} out of range (count={LevelCount}).");
                return null;
            }
            return _campaignLevels[index];
        }

        public LevelDefinition GetLevelById(string levelId) =>
            _campaignLevels?.Find(l => l.LevelId == levelId);

        public IReadOnlyList<LevelDefinition> AllLevels => _campaignLevels;
    }
}
