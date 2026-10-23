using System;
using System.Collections.Generic;
using UnityEngine;

namespace SnakeEscape.Domain
{
    /// <summary>
    /// Data describing one snake in a level. Serializable so it lives inside LevelDefinition assets.
    /// The head is always index 0; the tail is the last element.
    /// </summary>
    [Serializable]
    public class SnakeDefinition
    {
        [Tooltip("Unique identifier within a level. E.g. 'snake_0', 'snake_1'.")]
        public string Id;

        [Tooltip("Display color for the snake body.")]
        public Color BodyColor = new Color(0.18f, 0.72f, 0.44f);

        [Tooltip("Ordered body cells: index 0 = head, last index = tail.")]
        public List<GridCoord> BodyCells = new List<GridCoord>();

        [Tooltip("Cardinal unit direction the head will exit toward. Must be one of Up/Down/Left/Right.")]
        public GridCoord ExitDirection = GridCoord.Right;

        // --- Computed helpers (not serialized) ---

        public GridCoord HeadCell => BodyCells != null && BodyCells.Count > 0 ? BodyCells[0] : GridCoord.Zero;
        public GridCoord TailCell => BodyCells != null && BodyCells.Count > 0 ? BodyCells[BodyCells.Count - 1] : GridCoord.Zero;
        public int Length => BodyCells?.Count ?? 0;

        /// <summary>
        /// Validates structural integrity. Populates <paramref name="error"/> with the first problem found.
        /// </summary>
        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                error = "Snake has no ID.";
                return false;
            }

            if (BodyCells == null || BodyCells.Count == 0)
            {
                error = $"Snake '{Id}' has no body cells.";
                return false;
            }

            // Path continuity
            for (int i = 0; i < BodyCells.Count - 1; i++)
            {
                if (!BodyCells[i].IsCardinallyAdjacentTo(BodyCells[i + 1]))
                {
                    error = $"Snake '{Id}': gap between cells {BodyCells[i]} and {BodyCells[i + 1]} (index {i} → {i + 1}).";
                    return false;
                }
            }

            // Self-overlap
            var seen = new HashSet<GridCoord>();
            foreach (var cell in BodyCells)
            {
                if (!seen.Add(cell))
                {
                    error = $"Snake '{Id}': self-overlap at {cell}.";
                    return false;
                }
            }

            // Exit direction must be a cardinal unit vector
            if (!ExitDirection.IsCardinalUnit)
            {
                error = $"Snake '{Id}': ExitDirection {ExitDirection} is not a cardinal unit vector.";
                return false;
            }

            error = null;
            return true;
        }

        public override string ToString() => $"Snake[{Id}] head={HeadCell} exit={ExitDirection} len={Length}";
    }
}
