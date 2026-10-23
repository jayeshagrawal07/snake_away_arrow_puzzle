using System.Collections.Generic;
using NUnit.Framework;
using SnakeEscape.Domain;
using UnityEngine;

namespace SnakeEscape.Tests
{
    /// <summary>Tests for PuzzleRules: escape route, move evaluation, win detection, and validation.</summary>
    [TestFixture]
    public class PuzzleRulesTests
    {
        // ── Test Helpers ──────────────────────────────────────────────

        private static LevelDefinition MakeLevel(int cols, int rows, params SnakeDefinition[] snakes)
        {
            var level  = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = "test_level";
            level.Cols = cols;
            level.Rows = rows;
            level.Snakes = new List<SnakeDefinition>(snakes);
            return level;
        }

        private static SnakeDefinition MakeSnake(string id, GridCoord exitDir, params GridCoord[] cells)
        {
            return new SnakeDefinition
            {
                Id            = id,
                ExitDirection = exitDir,
                BodyCells     = new List<GridCoord>(cells)
            };
        }

        // ── ComputeEscapeRoute ─────────────────────────────────────────

        [Test]
        public void ComputeEscapeRoute_ClearPath_ReturnsAllCellsToExit()
        {
            // Snake at col=0, facing Right across 8-col board
            var snake = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0));
            var level = MakeLevel(8, 8, snake);

            var route = PuzzleRules.ComputeEscapeRoute(snake, level);

            // Should be cols 1–7 (in-bounds) + col 8 (exit cell outside board) = 8 entries
            Assert.AreEqual(8, route.Count);
            Assert.AreEqual(new GridCoord(1, 0), route[0]);
            Assert.AreEqual(new GridCoord(8, 0), route[route.Count - 1]); // Out-of-bounds exit
        }

        [Test]
        public void ComputeEscapeRoute_HeadAtEdgeFacingOut_ReturnsJustExitCell()
        {
            // Head at col=7 (rightmost), facing Right → route is immediately outside
            var snake = MakeSnake("s1", GridCoord.Right, new GridCoord(7, 0));
            var level = MakeLevel(8, 8, snake);

            var route = PuzzleRules.ComputeEscapeRoute(snake, level);

            // No in-bounds cells in front → only the exit cell
            Assert.AreEqual(1, route.Count);
            Assert.AreEqual(new GridCoord(8, 0), route[0]);
        }

        // ── EvaluateMove ───────────────────────────────────────────────

        [Test]
        public void EvaluateMove_ClearRoute_ReturnsValid()
        {
            var snake = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0));
            var level = MakeLevel(8, 8, snake);
            var state = new PuzzleState(level);

            var result = PuzzleRules.EvaluateMove("s1", state);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(MoveStatus.Valid, result.Status);
            Assert.IsNotNull(result.EscapeRoute);
            Assert.Greater(result.EscapeRoute.Count, 0);
        }

        [Test]
        public void EvaluateMove_BlockedByOtherSnake_ReturnsBlocked()
        {
            // s1 at (0,0), facing Right
            // s2 at (2,0), blocking s1's route
            var s1 = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0), new GridCoord(0, 1));
            var s2 = MakeSnake("s2", GridCoord.Up,    new GridCoord(2, 0), new GridCoord(2, 1));
            var level = MakeLevel(8, 8, s1, s2);
            var state = new PuzzleState(level);

            var result = PuzzleRules.EvaluateMove("s1", state);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(MoveStatus.BlockedBySnake, result.Status);
            Assert.AreEqual("s2", result.BlockingSnakeId);
        }

        [Test]
        public void EvaluateMove_AlreadyEscaped_ReturnsAlreadyEscaped()
        {
            var snake = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0));
            var level = MakeLevel(8, 8, snake);
            var state = new PuzzleState(level);
            state.CommitEscape("s1");

            var result = PuzzleRules.EvaluateMove("s1", state);

            Assert.AreEqual(MoveStatus.SnakeAlreadyEscaped, result.Status);
        }

        [Test]
        public void EvaluateMove_NonExistentSnake_ReturnsNotFound()
        {
            var snake = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0));
            var level = MakeLevel(8, 8, snake);
            var state = new PuzzleState(level);

            var result = PuzzleRules.EvaluateMove("ghost", state);

            Assert.AreEqual(MoveStatus.SnakeNotFound, result.Status);
        }

        [Test]
        public void EvaluateMove_AfterBlockerEscapes_BecomesValid()
        {
            var s1 = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0), new GridCoord(0, 1));
            var s2 = MakeSnake("s2", GridCoord.Up,    new GridCoord(2, 0), new GridCoord(2, 1));
            var level = MakeLevel(8, 8, s1, s2);
            var state = new PuzzleState(level);

            // s1 blocked initially
            Assert.AreEqual(MoveStatus.BlockedBySnake, PuzzleRules.EvaluateMove("s1", state).Status);

            // Remove blocker
            state.CommitEscape("s2");

            // s1 should now be clear
            Assert.AreEqual(MoveStatus.Valid, PuzzleRules.EvaluateMove("s1", state).Status);
        }

        // ── IsLevelComplete ────────────────────────────────────────────

        [Test]
        public void IsLevelComplete_NoEscapes_ReturnsFalse()
        {
            var snake = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0));
            var level = MakeLevel(8, 8, snake);
            var state = new PuzzleState(level);
            Assert.IsFalse(PuzzleRules.IsLevelComplete(state));
        }

        [Test]
        public void IsLevelComplete_AllEscaped_ReturnsTrue()
        {
            var snake = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0));
            var level = MakeLevel(8, 8, snake);
            var state = new PuzzleState(level);
            state.CommitEscape("s1");
            Assert.IsTrue(PuzzleRules.IsLevelComplete(state));
        }

        // ── ValidateLevel ──────────────────────────────────────────────

        [Test]
        public void ValidateLevel_ValidLevel_ReturnsNoErrors()
        {
            var snake = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0), new GridCoord(1, 0));
            var level = MakeLevel(8, 8, snake);
            var errors = PuzzleRules.ValidateLevel(level);
            Assert.AreEqual(0, errors.Count, "Errors: " + string.Join(", ", errors));
        }

        [Test]
        public void ValidateLevel_OutOfBoundsCell_ReturnsError()
        {
            var snake = MakeSnake("s1", GridCoord.Right, new GridCoord(10, 10));
            var level = MakeLevel(8, 8, snake);
            var errors = PuzzleRules.ValidateLevel(level);
            Assert.Greater(errors.Count, 0);
        }

        [Test]
        public void ValidateLevel_OverlappingSnakes_ReturnsError()
        {
            var s1 = MakeSnake("s1", GridCoord.Right, new GridCoord(2, 2), new GridCoord(3, 2));
            var s2 = MakeSnake("s2", GridCoord.Up,    new GridCoord(2, 2), new GridCoord(2, 3));
            var level = MakeLevel(8, 8, s1, s2);
            var errors = PuzzleRules.ValidateLevel(level);
            Assert.Greater(errors.Count, 0);
        }

        [Test]
        public void ValidateLevel_InvalidBoardSize_ReturnsError()
        {
            var snake = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0));
            var level = MakeLevel(1, 1, snake);
            var errors = PuzzleRules.ValidateLevel(level);
            Assert.Greater(errors.Count, 0);
        }

        [Test]
        public void ValidateLevel_NullLevel_ReturnsError()
        {
            var errors = PuzzleRules.ValidateLevel(null);
            Assert.Greater(errors.Count, 0);
        }

        [Test]
        public void ValidateLevel_DiscontiguousPath_ReturnsError()
        {
            // Gap between cells: (0,0) and (2,0) are NOT adjacent
            var snake = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0), new GridCoord(2, 0));
            var level = MakeLevel(8, 8, snake);
            var errors = PuzzleRules.ValidateLevel(level);
            Assert.Greater(errors.Count, 0);
        }

        // ── FindAllValidMoves ──────────────────────────────────────────

        [Test]
        public void FindAllValidMoves_TwoSnakesOneClear_ReturnsOnlyFreeSnake()
        {
            // s1 blocked by s2, s2 is free
            var s1 = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0), new GridCoord(0, 1));
            var s2 = MakeSnake("s2", GridCoord.Up,    new GridCoord(2, 0), new GridCoord(2, 1));
            var level = MakeLevel(8, 8, s1, s2);
            var state = new PuzzleState(level);

            var valid = PuzzleRules.FindAllValidMoves(state);
            Assert.Contains("s2", valid);
            Assert.IsFalse(valid.Contains("s1"));
        }
    }
}

