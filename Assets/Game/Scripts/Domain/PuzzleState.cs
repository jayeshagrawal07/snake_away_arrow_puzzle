using System;
using System.Collections.Generic;
using System.Linq;

namespace SnakeEscape.Domain
{
    /// <summary>
    /// Authoritative runtime state of an in-progress puzzle.
    /// Mutations are transactional: only CommitEscape modifies state, and only after a Valid MoveResult.
    /// Clone() produces a full deep copy for undo snapshots and solver branching.
    /// </summary>
    public class PuzzleState
    {
        public LevelDefinition Level { get; }

        /// <summary>Snake IDs that have successfully escaped and no longer occupy the board.</summary>
        public HashSet<string> EscapedSnakeIds { get; private set; }

        /// <summary>Current cell→snakeId mapping. Rebuilt from scratch when needed.</summary>
        public OccupancyMap Occupancy { get; private set; }

        public bool IsComplete => PuzzleRules.IsLevelComplete(this);

        // --- Construction ---

        public PuzzleState(LevelDefinition level)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            EscapedSnakeIds = new HashSet<string>();
            RebuildOccupancy();
        }

        // Private clone constructor
        private PuzzleState(LevelDefinition level, HashSet<string> escapedIds)
        {
            Level = level;
            EscapedSnakeIds = new HashSet<string>(escapedIds);
            RebuildOccupancy();
        }

        // --- Mutations (called by MoveService after animation completes) ---

        /// <summary>
        /// Records that a snake has escaped. Updates occupancy immediately.
        /// Precondition: the move was already validated as Valid.
        /// </summary>
        public void CommitEscape(string snakeId)
        {
            if (string.IsNullOrEmpty(snakeId)) throw new ArgumentNullException(nameof(snakeId));
            if (!EscapedSnakeIds.Add(snakeId))
                return; // already escaped, idempotent
            Occupancy.Vacate(snakeId);
        }

        /// <summary>Reverses a CommitEscape. Used by UndoService.</summary>
        public void UndoEscape(string snakeId)
        {
            if (EscapedSnakeIds.Remove(snakeId))
                RebuildOccupancy(); // Full rebuild to restore the snake's cells
        }

        // --- Queries ---

        /// <summary>Returns the SnakeDefinition only if the snake is still active (not escaped).</summary>
        public SnakeDefinition FindActiveSnake(string snakeId)
        {
            if (EscapedSnakeIds.Contains(snakeId)) return null;
            return Level.FindSnake(snakeId);
        }

        public List<SnakeDefinition> GetActiveSnakes() =>
            Level.Snakes.Where(s => !EscapedSnakeIds.Contains(s.Id)).ToList();

        // --- Snapshot ---

        /// <summary>
        /// Creates a full deep copy of the current state.
        /// Used by UndoService (before a move) and by PuzzleSolver (when branching).
        /// </summary>
        public PuzzleState Clone() => new PuzzleState(Level, EscapedSnakeIds);

        // --- Internal ---

        public void RebuildOccupancy() =>
            Occupancy = OccupancyMap.BuildFrom(Level, EscapedSnakeIds);
    }
}
