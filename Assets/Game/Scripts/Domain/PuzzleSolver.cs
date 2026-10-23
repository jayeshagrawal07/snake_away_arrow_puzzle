using System.Collections.Generic;
using System.Linq;

namespace SnakeEscape.Domain
{
    public enum SolveStatus
    {
        /// <summary>A complete solution sequence was found.</summary>
        Solved,

        /// <summary>The search space was fully explored with no solution found.</summary>
        Unsolvable,

        /// <summary>The level failed structural validation before the search began.</summary>
        InvalidLevel,

        /// <summary>
        /// The search limit was reached. This does NOT prove the level is unsolvable;
        /// it means the search was too expensive. Increase the limit or simplify the level.
        /// </summary>
        SearchLimitReached
    }

    public class SolverResult
    {
        public SolveStatus   Status;
        public List<string>  SolutionSequence; // Snake IDs in order of escape (only when Solved)
        public int           StatesExplored;
        public string        Message;

        public bool IsSolved => Status == SolveStatus.Solved;
    }

    /// <summary>
    /// Deterministic BFS puzzle solver with memoization.
    /// Operates purely on domain types — zero Unity dependencies.
    ///
    /// State representation: the set of not-yet-escaped snake IDs.
    /// This is valid because snake paths are fixed and removing one snake
    /// never changes the geometry of any other snake.
    /// </summary>
    public class PuzzleSolver
    {
        public const int DefaultSearchLimit = 200_000;

        private readonly int _searchLimit;

        public PuzzleSolver(int searchLimit = DefaultSearchLimit)
        {
            _searchLimit = searchLimit;
        }

        public SolverResult Solve(LevelDefinition level)
        {
            // ── 1. Validate ────────────────────────────────────────────
            var errors = PuzzleRules.ValidateLevel(level);
            if (errors.Count > 0)
                return new SolverResult
                {
                    Status  = SolveStatus.InvalidLevel,
                    Message = "Level validation failed:\n" + string.Join("\n", errors)
                };

            // ── 2. Trivial case ────────────────────────────────────────
            var initial = new PuzzleState(level);
            if (initial.IsComplete)
                return new SolverResult
                {
                    Status           = SolveStatus.Solved,
                    SolutionSequence = new List<string>(),
                    StatesExplored   = 1,
                    Message          = "Level is empty (already complete)."
                };

            // ── 3. BFS ────────────────────────────────────────────────
            // Queue entries: (current state snapshot, sequence of moves taken to reach it)
            var queue   = new Queue<(PuzzleState state, List<string> moves)>();
            var visited = new HashSet<string>();

            string initialKey = StateKey(initial);
            queue.Enqueue((initial, new List<string>()));
            visited.Add(initialKey);

            int explored = 0;

            while (queue.Count > 0)
            {
                if (++explored > _searchLimit)
                    return new SolverResult
                    {
                        Status         = SolveStatus.SearchLimitReached,
                        StatesExplored = explored,
                        Message        = $"Search limit of {_searchLimit:N0} states reached. " +
                                         "This does not prove the level is unsolvable."
                    };

                var (current, moves) = queue.Dequeue();
                var validMoves = PuzzleRules.FindAllValidMoves(current);

                foreach (var snakeId in validMoves)
                {
                    // Clone before mutation (solver needs non-destructive branching)
                    var next      = current.Clone();
                    var nextMoves = new List<string>(moves) { snakeId };
                    next.CommitEscape(snakeId);

                    if (next.IsComplete)
                        return new SolverResult
                        {
                            Status           = SolveStatus.Solved,
                            SolutionSequence = nextMoves,
                            StatesExplored   = explored,
                            Message          = $"Solved in {nextMoves.Count} move(s). States explored: {explored:N0}."
                        };

                    var key = StateKey(next);
                    if (!visited.Contains(key))
                    {
                        visited.Add(key);
                        queue.Enqueue((next, nextMoves));
                    }
                }
            }

            return new SolverResult
            {
                Status         = SolveStatus.Unsolvable,
                StatesExplored = explored,
                Message        = $"Level is unsolvable. All {explored:N0} reachable states explored."
            };
        }

        /// <summary>
        /// State key = sorted, comma-joined list of remaining (not-escaped) snake IDs.
        /// Valid because snake paths are fixed — only which snakes remain determines the state.
        /// </summary>
        private static string StateKey(PuzzleState state)
        {
            var remaining = state.Level.Snakes
                .Select(s => s.Id)
                .Where(id => !state.EscapedSnakeIds.Contains(id))
                .OrderBy(id => id, System.StringComparer.Ordinal);
            return string.Join(",", remaining);
        }
    }
}
