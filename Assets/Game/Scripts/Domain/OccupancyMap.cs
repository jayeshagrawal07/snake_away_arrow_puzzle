using System.Collections.Generic;

namespace SnakeEscape.Domain
{
    /// <summary>
    /// Authoritative mapping from board cells to the snake IDs occupying them.
    /// This is the single source of truth for collision detection.
    /// Updated transactionally: a rejected move never partially modifies occupancy.
    /// </summary>
    public class OccupancyMap
    {
        private readonly Dictionary<GridCoord, string> _cells = new Dictionary<GridCoord, string>();

        // --- Factory ---

        /// <summary>Rebuilds the map from scratch based on level data and which snakes have escaped.</summary>
        public static OccupancyMap BuildFrom(LevelDefinition level, IReadOnlyCollection<string> escapedIds)
        {
            var map = new OccupancyMap();
            if (level?.Snakes == null) return map;

            foreach (var snake in level.Snakes)
            {
                if (snake == null || escapedIds.Contains(snake.Id)) continue;
                if (snake.BodyCells == null) continue;

                foreach (var cell in snake.BodyCells)
                    map._cells[cell] = snake.Id;
            }
            return map;
        }

        // --- Queries ---

        public bool IsOccupied(GridCoord cell) => _cells.ContainsKey(cell);

        public bool TryGetOccupant(GridCoord cell, out string snakeId) =>
            _cells.TryGetValue(cell, out snakeId);

        public string GetOccupant(GridCoord cell) =>
            _cells.TryGetValue(cell, out var id) ? id : null;

        public IReadOnlyDictionary<GridCoord, string> Cells => _cells;

        // --- Mutations (called only by PuzzleState after a validated move) ---

        /// <summary>Removes all cells belonging to the given snake (after it escapes).</summary>
        public void Vacate(string snakeId)
        {
            var toRemove = new System.Collections.Generic.List<GridCoord>();
            foreach (var kv in _cells)
                if (kv.Value == snakeId) toRemove.Add(kv.Key);
            foreach (var cell in toRemove)
                _cells.Remove(cell);
        }
    }
}
