using System.Collections.Generic;
using UnityEngine;

namespace SnakeEscape.Application
{
    /// <summary>
    /// Manages player progression: which levels are unlocked and completed.
    /// Persists via PlayerPrefs (JSON). Versioned for future migrations.
    ///
    /// TODO (M6): Replace with SaveService using JSON file + versioned schema.
    /// </summary>
    public class ProgressionService
    {
        private const string SaveKey     = "SnakeEscape_Progression_v1";
        private const int    SchemaVersion = 1;

        [System.Serializable]
        private class ProgressionData
        {
            public int              schemaVersion = SchemaVersion;
            public List<string>     completedLevelIds = new List<string>();
            public List<string>     unlockedLevelIds  = new List<string>();
            public int              highestUnlockedIndex = 0;
        }

        private ProgressionData _data = new ProgressionData();

        // ── Persistence ───────────────────────────────────────────────

        public void Load()
        {
            string json = PlayerPrefs.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(json))
            {
                _data = new ProgressionData();
                return;
            }
            try   { _data = JsonUtility.FromJson<ProgressionData>(json) ?? new ProgressionData(); }
            catch { _data = new ProgressionData(); Debug.LogWarning("[ProgressionService] Save data was corrupt; reset."); }
        }

        public void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(_data));
            PlayerPrefs.Save();
        }

        public void ResetAll()
        {
            _data = new ProgressionData();
            Save();
        }

        // ── Queries ───────────────────────────────────────────────────

        public bool IsCompleted(string levelId) => _data.completedLevelIds.Contains(levelId);
        public bool IsUnlocked(string levelId)  => _data.unlockedLevelIds.Contains(levelId);

        // ── Mutations ─────────────────────────────────────────────────

        public void MarkCompleted(string levelId, int levelIndex)
        {
            if (!_data.completedLevelIds.Contains(levelId))
                _data.completedLevelIds.Add(levelId);

            // Unlock next level
            int nextIndex = levelIndex + 1;
            if (nextIndex > _data.highestUnlockedIndex)
                _data.highestUnlockedIndex = nextIndex;

            Save();
        }

        public void UnlockLevel(string levelId)
        {
            if (!_data.unlockedLevelIds.Contains(levelId))
                _data.unlockedLevelIds.Add(levelId);
        }
    }
}
