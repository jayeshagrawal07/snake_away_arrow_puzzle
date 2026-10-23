using System;
using System.Collections.Generic;
using SnakeEscape.Domain;

namespace SnakeEscape.Application
{
    /// <summary>
    /// Manages an undo stack of PuzzleState snapshots.
    /// Each successful CommitMove should trigger a snapshot push via RecordSnapshot().
    /// Undo() pops the most recent snapshot and applies it.
    /// </summary>
    public class UndoService
    {
        public event Action OnUndoPerformed;
        public event Action OnUndoStackChanged;

        private readonly Stack<PuzzleState> _snapshots = new Stack<PuzzleState>();
        private MoveService _moveService;

        public void Initialize(MoveService moveService)
        {
            _moveService = moveService ?? throw new ArgumentNullException(nameof(moveService));
            _snapshots.Clear();
        }

        public void Reset() => _snapshots.Clear();

        /// <summary>
        /// Call this BEFORE each move is committed to save the pre-move state.
        /// </summary>
        public void RecordSnapshot(PuzzleState state)
        {
            _snapshots.Push(state.Clone());
            OnUndoStackChanged?.Invoke();
        }

        /// <summary>
        /// Restores the most recent snapshot. Only valid when not busy.
        /// Returns true if an undo was performed.
        /// </summary>
        public bool Undo()
        {
            if (_moveService.IsBusy)
            {
                UnityEngine.Debug.LogWarning("[UndoService] Cannot undo while a move animation is in progress.");
                return false;
            }

            if (_snapshots.Count == 0) return false;

            var snapshot = _snapshots.Pop();
            _moveService.Initialize(snapshot);
            _moveService.Reset();

            OnUndoPerformed?.Invoke();
            OnUndoStackChanged?.Invoke();
            return true;
        }

        public bool CanUndo => _snapshots.Count > 0 && !_moveService.IsBusy;
        public int  UndoCount => _snapshots.Count;
    }
}
