using System.Collections.Generic;

namespace SnakeEscape.Domain
{
    /// <summary>
    /// Shared grid geometry for projecting a snake's straight exit path.
    /// </summary>
    public static class EscapeRouteGeometry
    {
        /// <summary>
        /// Returns the in-bounds cells starting one step ahead of the head and ending
        /// immediately before the board edge. The head cell is not included.
        /// </summary>
        public static List<GridCoord> ComputeInBoundsRoute(
            GridCoord head,
            GridCoord direction,
            int cols,
            int rows)
        {
            if (cols < 1) throw new System.ArgumentOutOfRangeException(nameof(cols));
            if (rows < 1) throw new System.ArgumentOutOfRangeException(nameof(rows));
            if (!direction.IsCardinalUnit)
                throw new System.ArgumentException("Exit direction must be a cardinal unit vector.", nameof(direction));

            var route = new List<GridCoord>();
            GridCoord current = head + direction;

            while (current.Col >= 0 && current.Col < cols && current.Row >= 0 && current.Row < rows)
            {
                route.Add(current);
                current += direction;
            }

            return route;
        }

        /// <summary>
        /// Counts route cells before the first cell reported as blocked.
        /// Returns the full route length when no cell is blocked.
        /// </summary>
        public static int CountClearCellsBeforeBlock(
            IReadOnlyList<GridCoord> route,
            System.Predicate<GridCoord> isBlocked)
        {
            if (route == null) throw new System.ArgumentNullException(nameof(route));
            if (isBlocked == null) throw new System.ArgumentNullException(nameof(isBlocked));

            for (int i = 0; i < route.Count; i++)
            {
                if (isBlocked(route[i]))
                    return i;
            }

            return route.Count;
        }

        public static MoveResult EvaluateMove(
            string snakeId,
            GridCoord head,
            GridCoord direction,
            int cols,
            int rows,
            System.Func<GridCoord, string> findBlockingSnake,
            out int clearCells)
        {
            if (findBlockingSnake == null)
                throw new System.ArgumentNullException(nameof(findBlockingSnake));

            var route = ComputeInBoundsRoute(head, direction, cols, rows);
            for (int i = 0; i < route.Count; i++)
            {
                string blockingSnakeId = findBlockingSnake(route[i]);
                if (string.IsNullOrEmpty(blockingSnakeId))
                    continue;

                clearCells = i;
                return MoveResult.Blocked(snakeId, blockingSnakeId, route[i]);
            }

            clearCells = route.Count;
            route.Add(head + direction * (route.Count + 1));
            return MoveResult.Valid(snakeId, route);
        }
    }

    /// <summary>
    /// Pure, stateless puzzle logic.
    /// All methods take explicit inputs and return results; no side effects.
    /// Do NOT add Unity MonoBehaviour or scene references here.
    /// </summary>
    public static class PuzzleRules
    {
        // ──────────────────────────────────────────────────────────────
        // Escape Route
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes all cells the snake must traverse to escape, from the first cell
        /// in front of the head through the board boundary.
        /// The final entry in the returned list is the first out-of-bounds exit cell
        /// (used as the animation target).
        /// Returns an empty list if ExitDirection is not a cardinal unit.
        /// </summary>
        public static List<GridCoord> ComputeEscapeRoute(SnakeDefinition snake, LevelDefinition level)
        {
            var route = new List<GridCoord>();
            if (snake == null || level == null || !snake.ExitDirection.IsCardinalUnit)
                return route;

            route = EscapeRouteGeometry.ComputeInBoundsRoute(
                snake.HeadCell,
                snake.ExitDirection,
                level.Cols,
                level.Rows);
            GridCoord exitCell = snake.HeadCell + snake.ExitDirection * (route.Count + 1);
            route.Add(exitCell); // First out-of-bounds cell = visual exit target
            return route;
        }

        // ──────────────────────────────────────────────────────────────
        // Move Evaluation
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates whether a snake can legally escape right now.
        /// Does NOT modify any state.
        /// </summary>
        public static MoveResult EvaluateMove(string snakeId, PuzzleState state)
        {
            if (state.EscapedSnakeIds.Contains(snakeId))
                return MoveResult.AlreadyEscaped(snakeId);

            var snake = state.FindActiveSnake(snakeId);
            if (snake == null)
                return MoveResult.NotFound(snakeId);

            var route = ComputeEscapeRoute(snake, state.Level);

            // Check every in-bounds route cell for blocking snakes.
            // Skip the final out-of-bounds exit cell (last element).
            for (int i = 0; i < route.Count - 1; i++)
            {
                GridCoord cell = route[i];
                if (state.Occupancy.TryGetOccupant(cell, out string occupant) && occupant != snakeId)
                    return MoveResult.Blocked(snakeId, occupant, cell);
            }

            return MoveResult.Valid(snakeId, route);
        }

        // ──────────────────────────────────────────────────────────────
        // Multi-move helpers
        // ──────────────────────────────────────────────────────────────

        /// <summary>Returns IDs of all snakes that can legally escape in the current state.</summary>
        public static List<string> FindAllValidMoves(PuzzleState state)
        {
            var valid = new List<string>();
            foreach (var snake in state.Level.Snakes)
            {
                if (state.EscapedSnakeIds.Contains(snake.Id)) continue;
                if (EvaluateMove(snake.Id, state).IsValid)
                    valid.Add(snake.Id);
            }
            return valid;
        }

        // ──────────────────────────────────────────────────────────────
        // Win Detection
        // ──────────────────────────────────────────────────────────────

        public static bool IsLevelComplete(PuzzleState state) =>
            state.Level?.Snakes != null &&
            state.Level.Snakes.Count > 0 &&
            state.EscapedSnakeIds.Count >= state.Level.Snakes.Count;

        // ──────────────────────────────────────────────────────────────
        // Level Validation
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Validates a LevelDefinition for structural correctness.
        /// Returns a list of human-readable error strings (empty = valid).
        /// </summary>
        public static List<string> ValidateLevel(LevelDefinition level)
        {
            var errors = new List<string>();

            if (level == null)
            {
                errors.Add("Level is null.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(level.LevelId))
                errors.Add("Level has no LevelId.");

            if (level.Cols < 2 || level.Rows < 2)
                errors.Add($"Board dimensions too small: {level.Cols}×{level.Rows}. Minimum is 2×2.");

            if (level.Snakes == null || level.Snakes.Count == 0)
            {
                errors.Add("Level has no snakes.");
                return errors;
            }

            var idsSeen = new System.Collections.Generic.HashSet<string>();
            var occupiedCells = new Dictionary<GridCoord, string>(); // cell → snakeId

            foreach (var snake in level.Snakes)
            {
                if (snake == null) { errors.Add("Null snake entry in level."); continue; }

                // Structural validation
                if (!snake.IsValid(out string snakeError))
                {
                    errors.Add(snakeError);
                    continue;
                }

                // Duplicate IDs
                if (!idsSeen.Add(snake.Id))
                    errors.Add($"Duplicate snake ID: '{snake.Id}'.");

                // Board boundary + inter-snake overlap
                foreach (var cell in snake.BodyCells)
                {
                    if (!level.IsInBounds(cell))
                        errors.Add($"Snake '{snake.Id}': cell {cell} is outside the board ({level.Cols}×{level.Rows}).");

                    if (occupiedCells.TryGetValue(cell, out var existingId))
                        errors.Add($"Snake '{snake.Id}' overlaps snake '{existingId}' at {cell}.");
                    else
                        occupiedCells[cell] = snake.Id;
                }

                // Escape route must contain at least one cell (i.e., head is not already at the edge facing outward with no path)
                var route = ComputeEscapeRoute(snake, level);
                if (route.Count < 1)
                    errors.Add($"Snake '{snake.Id}': escape route is empty (head may already be outside board).");
            }

            return errors;
        }
    }
}
