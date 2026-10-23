using System.Collections.Generic;

namespace SnakeEscape.Domain
{
    /// <summary>
    /// Describes the outcome of a move request.
    /// Returned by PuzzleRules.EvaluateMove and consumed by the application and presentation layers.
    /// </summary>
    public enum MoveStatus
    {
        /// <summary>The move is legal. EscapeRoute is populated.</summary>
        Valid,

        /// <summary>Another snake is blocking the exit route. BlockingSnakeId is populated.</summary>
        BlockedBySnake,

        /// <summary>The target snake has already escaped this level.</summary>
        SnakeAlreadyEscaped,

        /// <summary>No snake with the given ID exists in the level.</summary>
        SnakeNotFound,

        /// <summary>The game is not currently accepting input (e.g. animation in progress).</summary>
        GameBusy,

        /// <summary>The level data is invalid; the move cannot be evaluated safely.</summary>
        InvalidLevel
    }

    public class MoveResult
    {
        public MoveStatus  Status;
        public string      SnakeId;
        public string      BlockingSnakeId;   // Set when Status == BlockedBySnake
        public GridCoord   BlockedAtCell;     // The first blocked cell
        public List<GridCoord> EscapeRoute;   // Set when Status == Valid (head→exit cells)
        public string      Message;

        public bool IsValid => Status == MoveStatus.Valid;

        // --- Static factory helpers ---

        public static MoveResult Valid(string snakeId, List<GridCoord> route) => new MoveResult
        {
            Status      = MoveStatus.Valid,
            SnakeId     = snakeId,
            EscapeRoute = route,
            Message     = $"Snake '{snakeId}' can escape."
        };

        public static MoveResult Blocked(string snakeId, string blockingId, GridCoord blockedAt) => new MoveResult
        {
            Status          = MoveStatus.BlockedBySnake,
            SnakeId         = snakeId,
            BlockingSnakeId = blockingId,
            BlockedAtCell   = blockedAt,
            Message         = $"Snake '{snakeId}' is blocked by '{blockingId}' at {blockedAt}."
        };

        public static MoveResult NotFound(string snakeId) => new MoveResult
        {
            Status  = MoveStatus.SnakeNotFound,
            SnakeId = snakeId,
            Message = $"Snake '{snakeId}' was not found in this level."
        };

        public static MoveResult AlreadyEscaped(string snakeId) => new MoveResult
        {
            Status  = MoveStatus.SnakeAlreadyEscaped,
            SnakeId = snakeId,
            Message = $"Snake '{snakeId}' has already escaped."
        };

        public static MoveResult Busy(string reason = "A move is already in progress.") => new MoveResult
        {
            Status  = MoveStatus.GameBusy,
            Message = reason
        };

        public override string ToString() => $"MoveResult[{Status}] {Message}";
    }
}
