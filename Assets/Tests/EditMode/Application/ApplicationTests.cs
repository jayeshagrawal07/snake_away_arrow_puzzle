using System.Collections.Generic;
using NUnit.Framework;
using SnakeEscape.Application;
using SnakeEscape.Domain;
using UnityEngine;

namespace SnakeEscape.Tests
{
    [TestFixture]
    public class ApplicationTests
    {
        private static LevelDefinition MakeLevel(int cols, int rows, params SnakeDefinition[] snakes)
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = "app_test";
            level.Cols = cols;
            level.Rows = rows;
            level.Snakes = new List<SnakeDefinition>(snakes);
            return level;
        }

        private static SnakeDefinition MakeSnake(string id, GridCoord exitDir, params GridCoord[] cells) =>
            new SnakeDefinition { Id = id, ExitDirection = exitDir, BodyCells = new List<GridCoord>(cells) };

        [Test]
        public void MoveService_RequestAndCommit_FlowCompletes()
        {
            var s1 = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0));
            var level = MakeLevel(8, 8, s1);
            var state = new PuzzleState(level);

            var moveService = new MoveService();
            moveService.Initialize(state);

            bool acceptedFired = false;
            moveService.OnMoveAccepted += (id, res) => acceptedFired = true;

            var result = moveService.RequestMove("s1");
            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(acceptedFired);
            Assert.IsTrue(moveService.IsBusy);

            // While busy, another move is rejected
            var busyRes = moveService.RequestMove("s1");
            Assert.AreEqual(MoveStatus.GameBusy, busyRes.Status);

            // Commit
            bool committedFired = false;
            bool winFired = false;
            moveService.OnMoveCommitted += id => committedFired = true;
            moveService.OnLevelComplete += () => winFired = true;

            moveService.CommitMove("s1");

            Assert.IsTrue(committedFired);
            Assert.IsTrue(winFired);
            Assert.IsFalse(moveService.IsBusy);
            Assert.IsTrue(state.IsComplete);
        }

        [Test]
        public void UndoService_RestoresPreviousState()
        {
            var s1 = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0));
            var level = MakeLevel(8, 8, s1);
            var state = new PuzzleState(level);

            var moveService = new MoveService();
            moveService.Initialize(state);

            var undoService = new UndoService();
            undoService.Initialize(moveService);

            // Snapshot before move
            undoService.RecordSnapshot(state);
            Assert.IsTrue(undoService.CanUndo);

            moveService.RequestMove("s1");
            moveService.CommitMove("s1");
            Assert.IsTrue(state.IsComplete);

            // Perform undo
            bool undone = undoService.Undo();
            Assert.IsTrue(undone);
            Assert.IsFalse(moveService.State.IsComplete);
            Assert.IsFalse(moveService.State.EscapedSnakeIds.Contains("s1"));
        }

        [Test]
        public void GameSession_UndoRestoresAuthoritativeStateAndSnakeOccupancy()
        {
            var passingSnake = MakeSnake("passing", GridCoord.Right, new GridCoord(0, 1));
            var escapingSnake = MakeSnake("escaping", GridCoord.Up, new GridCoord(2, 1));
            var level = MakeLevel(4, 4, passingSnake, escapingSnake);

            var session = new GameSession();
            session.LoadLevel(level);

            Assert.AreEqual(MoveStatus.BlockedBySnake, session.RequestMove("passing").Status);
            Assert.IsTrue(session.RequestMove("escaping").IsValid);
            session.UndoService.RecordSnapshot(session.PuzzleState);
            session.CommitMove("escaping");

            Assert.IsTrue(session.PuzzleState.EscapedSnakeIds.Contains("escaping"));
            session.UndoService.RecordSnapshot(session.PuzzleState);
            Assert.IsTrue(session.RequestMove("passing").IsValid);
            session.CommitMove("passing");

            Assert.IsTrue(session.RequestUndo());

            Assert.AreSame(session.MoveService.State, session.PuzzleState);
            Assert.IsFalse(session.PuzzleState.EscapedSnakeIds.Contains("passing"));
            Assert.IsTrue(session.PuzzleState.EscapedSnakeIds.Contains("escaping"));
            Assert.AreEqual("passing", session.PuzzleState.Occupancy.GetOccupant(new GridCoord(0, 1)));
            Assert.IsNull(session.PuzzleState.Occupancy.GetOccupant(new GridCoord(2, 1)));
            Assert.IsTrue(session.RequestMove("passing").IsValid);
        }

        [Test]
        public void GameSession_UndoAfterLevelCompletionReturnsToReady()
        {
            var snake = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0));
            var session = new GameSession();
            session.LoadLevel(MakeLevel(8, 8, snake));

            session.UndoService.RecordSnapshot(session.PuzzleState);
            Assert.IsTrue(session.RequestMove("s1").IsValid);
            session.CommitMove("s1");
            Assert.AreEqual(GameSession.SessionState.LevelComplete, session.CurrentState);

            Assert.IsTrue(session.RequestUndo());

            Assert.AreEqual(GameSession.SessionState.Ready, session.CurrentState);
            Assert.AreSame(session.MoveService.State, session.PuzzleState);
            Assert.IsFalse(session.PuzzleState.IsComplete);
            Assert.IsTrue(session.PuzzleState.Occupancy.IsOccupied(new GridCoord(0, 0)));
        }

        [Test]
        public void GameSession_FullLifecycle()
        {
            var s1 = MakeSnake("s1", GridCoord.Right, new GridCoord(0, 0));
            var level = MakeLevel(8, 8, s1);

            var session = new GameSession();
            session.LoadLevel(level);

            Assert.AreEqual(GameSession.SessionState.Ready, session.CurrentState);

            var res = session.RequestMove("s1");
            Assert.IsTrue(res.IsValid);
            Assert.AreEqual(GameSession.SessionState.Moving, session.CurrentState);

            session.CommitMove("s1");
            Assert.AreEqual(GameSession.SessionState.LevelComplete, session.CurrentState);

            // Restart
            session.RestartLevel();
            Assert.AreEqual(GameSession.SessionState.Ready, session.CurrentState);
            Assert.IsFalse(session.PuzzleState.IsComplete);
        }
    }
}
