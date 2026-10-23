using System;
using SnakeEscape.Domain;

namespace SnakeEscape.Application
{
    /// <summary>
    /// Single entry point for all player move requests.
    /// Validates moves via domain rules, manages the busy/idle state,
    /// and notifies the presentation layer via events.
    ///
    /// Flow:
    ///   1. Presentation calls RequestMove(snakeId).
    ///   2. MoveService evaluates via PuzzleRules.EvaluateMove.
    ///   3. If valid: fires OnMoveAccepted → presentation starts animation.
    ///   4. Presentation calls CommitMove(snakeId) when animation finishes.
    ///   5. MoveService commits state change, checks win condition.
    /// </summary>
    public class MoveService
    {
        // ── Events (subscribed to by presentation layer) ──────────────
        public event Action<MoveResult>       OnMoveEvaluated;   // Always fires (valid or not)
        public event Action<string, MoveResult> OnMoveAccepted;  // snakeId, result — fires when valid
        public event Action<string>           OnMoveCommitted;   // snakeId — fires after animation confirms
        public event Action                   OnLevelComplete;

        // ── State ─────────────────────────────────────────────────────
        private PuzzleState _state;
        private bool        _isBusy;
        private string      _pendingSnakeId;

        // ── Setup ─────────────────────────────────────────────────────

        public void Initialize(PuzzleState state)
        {
            _state         = state ?? throw new ArgumentNullException(nameof(state));
            _isBusy        = false;
            _pendingSnakeId = null;
        }

        public void Reset()
        {
            _isBusy         = false;
            _pendingSnakeId = null;
        }

        // ── Move Request (called by input/presentation) ───────────────

        /// <summary>
        /// Evaluates a move and, if valid, marks the service as busy waiting for CommitMove.
        /// Returns the MoveResult for immediate feedback use.
        /// </summary>
        public MoveResult RequestMove(string snakeId)
        {
            if (_state == null) return MoveResult.Busy("Game session not initialized.");
            if (_isBusy)        return MoveResult.Busy($"Cannot move '{snakeId}': animation in progress for '{_pendingSnakeId}'.");

            var result = PuzzleRules.EvaluateMove(snakeId, _state);
            OnMoveEvaluated?.Invoke(result);

            if (result.IsValid)
            {
                _isBusy         = true;
                _pendingSnakeId = snakeId;
                OnMoveAccepted?.Invoke(snakeId, result);
            }

            return result;
        }

        /// <summary>
        /// Called by the presentation layer when the escape animation has fully completed.
        /// Commits the state change and checks for level completion.
        /// </summary>
        public void CommitMove(string snakeId)
        {
            if (_state == null) return;

            _state.CommitEscape(snakeId);
            _isBusy         = false;
            _pendingSnakeId = null;

            OnMoveCommitted?.Invoke(snakeId);

            if (_state.IsComplete)
                OnLevelComplete?.Invoke();
        }

        // ── Queries ───────────────────────────────────────────────────

        public bool IsBusy           => _isBusy;
        public string PendingSnakeId => _pendingSnakeId;
        public PuzzleState State     => _state;
    }
}
