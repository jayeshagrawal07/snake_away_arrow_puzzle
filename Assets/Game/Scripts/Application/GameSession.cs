using System;
using SnakeEscape.Domain;
using UnityEngine;

namespace SnakeEscape.Application
{
    /// <summary>
    /// Coordinates the lifecycle of one puzzle session: initialization, moves, undo, restart, and completion.
    /// Owns the PuzzleState and orchestrates MoveService and UndoService.
    /// </summary>
    public class GameSession
    {
        public enum SessionState
        {
            Uninitialized,
            Ready,
            Moving,         // Animation in progress
            Paused,
            LevelComplete,
            Error
        }

        // ── Events ────────────────────────────────────────────────────
        public event Action<SessionState, SessionState> OnStateChanged; // (from, to)
        public event Action<LevelDefinition>            OnLevelLoaded;
        public event Action                             OnLevelRestarted;
        public event Action                             OnLevelComplete;

        // ── Services ──────────────────────────────────────────────────
        public MoveService  MoveService  { get; } = new MoveService();
        public UndoService  UndoService  { get; } = new UndoService();

        // ── State ─────────────────────────────────────────────────────
        public SessionState     CurrentState { get; private set; } = SessionState.Uninitialized;
        public LevelDefinition  CurrentLevel { get; private set; }
        public PuzzleState      PuzzleState  { get; private set; }

        // ── Setup ─────────────────────────────────────────────────────

        public void LoadLevel(LevelDefinition level)
        {
            CurrentLevel = level ?? throw new ArgumentNullException(nameof(level));

            var errors = PuzzleRules.ValidateLevel(level);
            if (errors.Count > 0)
            {
                Debug.LogError($"[GameSession] Level '{level.LevelId}' failed validation:\n" + string.Join("\n", errors));
                Transition(SessionState.Error);
                return;
            }

            PuzzleState = new PuzzleState(level);
            MoveService.Initialize(PuzzleState);
            UndoService.Initialize(MoveService);

            WireEvents();
            Transition(SessionState.Ready);
            OnLevelLoaded?.Invoke(level);
        }

        public void RestartLevel()
        {
            if (CurrentLevel == null) return;
            MoveService.Reset();
            UndoService.Reset();
            PuzzleState = new PuzzleState(CurrentLevel);
            MoveService.Initialize(PuzzleState);
            Transition(SessionState.Ready);
            OnLevelRestarted?.Invoke();
        }

        // ── Input ─────────────────────────────────────────────────────

        public MoveResult RequestMove(string snakeId)
        {
            if (CurrentState != SessionState.Ready)
                return MoveResult.Busy($"Session is in state {CurrentState}, not Ready.");

            return MoveService.RequestMove(snakeId);
        }

        public void CommitMove(string snakeId)
        {
            MoveService.CommitMove(snakeId);
        }

        public bool RequestUndo() => UndoService.Undo();

        // ── Pause ─────────────────────────────────────────────────────

        public void Pause()
        {
            if (CurrentState == SessionState.Ready)
                Transition(SessionState.Paused);
        }

        public void Resume()
        {
            if (CurrentState == SessionState.Paused)
                Transition(SessionState.Ready);
        }

        // ── Internal ─────────────────────────────────────────────────

        private void WireEvents()
        {
            MoveService.OnMoveAccepted += (_, __) => Transition(SessionState.Moving);
            MoveService.OnMoveCommitted += _ =>
            {
                if (CurrentState != SessionState.LevelComplete)
                    Transition(SessionState.Ready);
            };
            MoveService.OnLevelComplete += () =>
            {
                Transition(SessionState.LevelComplete);
                OnLevelComplete?.Invoke();
            };
        }

        private void Transition(SessionState next)
        {
            var prev = CurrentState;
            CurrentState = next;
            if (prev != next)
                OnStateChanged?.Invoke(prev, next);
        }
    }
}
