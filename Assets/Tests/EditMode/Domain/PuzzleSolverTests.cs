using System.Collections.Generic;
using NUnit.Framework;
using SnakeEscape.Domain;
using UnityEngine;

namespace SnakeEscape.Tests
{
    [TestFixture]
    public class PuzzleSolverTests
    {
        private static LevelDefinition MakeLevel(int cols, int rows, params SnakeDefinition[] snakes)
        {
            var level  = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = "solver_test";
            level.Cols    = cols;
            level.Rows    = rows;
            level.Snakes  = new List<SnakeDefinition>(snakes);
            return level;
        }

        private static SnakeDefinition MakeSnake(string id, GridCoord exitDir, params GridCoord[] cells) =>
            new SnakeDefinition { Id = id, ExitDirection = exitDir, BodyCells = new List<GridCoord>(cells) };

        // ── Trivial cases ──────────────────────────────────────────────

        [Test]
        public void Solve_SingleFreeSnake_ReturnsSolved()
        {
            var snake  = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0));
            var level  = MakeLevel(8, 8, snake);
            var solver = new PuzzleSolver();

            var result = solver.Solve(level);

            Assert.AreEqual(SolveStatus.Solved, result.Status);
            Assert.AreEqual(1, result.SolutionSequence.Count);
            Assert.AreEqual("s1", result.SolutionSequence[0]);
        }

        [Test]
        public void Solve_TwoSnakes_BlockedThenFree_ReturnsSolvedInOrder()
        {
            // s2 blocks s1; must escape s2 first, then s1
            var s1 = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0), new GridCoord(0, 1));
            var s2 = MakeSnake("s2", GridCoord.Up,    new GridCoord(2, 0), new GridCoord(2, 1));
            var level  = MakeLevel(8, 8, s1, s2);
            var solver = new PuzzleSolver();

            var result = solver.Solve(level);

            Assert.AreEqual(SolveStatus.Solved, result.Status);
            Assert.AreEqual(2, result.SolutionSequence.Count);
            Assert.AreEqual("s2", result.SolutionSequence[0]);
            Assert.AreEqual("s1", result.SolutionSequence[1]);
        }

        [Test]
        public void Solve_InvalidLevel_ReturnsInvalidLevel()
        {
            // Level with an out-of-bounds cell
            var snake  = MakeSnake("s1", GridCoord.Right, new GridCoord(10, 10));
            var level  = MakeLevel(8, 8, snake);
            var solver = new PuzzleSolver();

            var result = solver.Solve(level);

            Assert.AreEqual(SolveStatus.InvalidLevel, result.Status);
        }

        [Test]
        public void Solve_NullSnakes_ReturnsInvalidLevel()
        {
            var level = MakeLevel(8, 8);
            level.Snakes = null;

            var solver = new PuzzleSolver();
            var result = solver.Solve(level);

            Assert.AreEqual(SolveStatus.InvalidLevel, result.Status);
        }

        [Test]
        public void Solve_SolutionReplay_ProducesCompleteState()
        {
            var s1 = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0), new GridCoord(0, 1));
            var s2 = MakeSnake("s2", GridCoord.Up,    new GridCoord(2, 0), new GridCoord(2, 1));
            var level  = MakeLevel(8, 8, s1, s2);
            var solver = new PuzzleSolver();

            var result = solver.Solve(level);
            Assert.AreEqual(SolveStatus.Solved, result.Status);

            // Replay the solution and verify the board reaches completion
            var state = new PuzzleState(level);
            foreach (var snakeId in result.SolutionSequence)
            {
                var moveResult = PuzzleRules.EvaluateMove(snakeId, state);
                Assert.IsTrue(moveResult.IsValid, $"Replayed move '{snakeId}' should be valid.");
                state.CommitEscape(snakeId);
            }

            Assert.IsTrue(state.IsComplete, "Board should be complete after replaying the solution.");
        }

        [Test]
        public void Solve_StatesExplored_IsPositive()
        {
            var snake  = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0));
            var level  = MakeLevel(8, 8, snake);
            var solver = new PuzzleSolver();

            var result = solver.Solve(level);
            Assert.Greater(result.StatesExplored, 0);
        }
    }
}
