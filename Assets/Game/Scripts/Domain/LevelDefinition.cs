using System.Collections.Generic;
using UnityEngine;

namespace SnakeEscape.Domain
{
    /// <summary>
    /// ScriptableObject asset that fully describes one puzzle level.
    /// Create via: Assets → Create → Snake Escape → Level Definition.
    /// </summary>
    [CreateAssetMenu(fileName = "Level_000", menuName = "Snake Escape/Level Definition", order = 1)]
    public class LevelDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable, unique level ID. Never change after a level is shipped.")]
        public string LevelId;

        [Tooltip("Campaign chapter/pack this level belongs to.")]
        public string CampaignChapter;

        [Tooltip("Sort order within the campaign. Lower = earlier.")]
        public int SortOrder;

        [Header("Board")]
        [Tooltip("Number of columns (width).")]
        [Min(2)] public int Cols = 8;

        [Tooltip("Number of rows (height).")]
        [Min(2)] public int Rows = 8;

        [Header("Snakes")]
        public List<SnakeDefinition> Snakes = new List<SnakeDefinition>();

        [Header("Metadata")]
        public string DifficultyCategory;
        [TextArea(1, 3)] public string LearningObjective;
        [TextArea(2, 5)] public string DesignerNotes;
        public string Author;
        public string ContentVersion = "1.0";

        [Header("Validation (set by validator tool)")]
        public bool ValidationPassed;
        [TextArea(2, 6)] public string ValidationReport;

        // --- Helpers ---

        public bool IsInBounds(GridCoord cell) =>
            cell.Col >= 0 && cell.Col < Cols && cell.Row >= 0 && cell.Row < Rows;

        public SnakeDefinition FindSnake(string id) =>
            Snakes?.Find(s => s.Id == id);
    }
}
