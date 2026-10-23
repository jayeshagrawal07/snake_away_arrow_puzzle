# Testing Strategy — Snake Escape

## 1. EditMode Unit Tests
Located under `Assets/Tests/EditMode/`.
Cover pure domain rules and puzzle solving without launching a scene:
- `GridCoordTests`: Vector math, equality, bounds, cardinal direction checks.
- `OccupancyMapTests`: Cell reservations, occupant retrieval, vacate transactions.
- `PuzzleRulesTests`: Escape route calculation, blocked moves, win detection, level structural validation.
- `PuzzleSolverTests`: BFS state solver, dead-end detection, replay validation.
- `ApplicationTests`: `MoveService`, `UndoService`, and `GameSession` state lifecycle.

## 2. PlayMode Tests (Milestone 8)
Located under `Assets/Tests/PlayMode/`.
Will cover:
- Snake rendering and animation completion triggers.
- Input blocking during active movement.
- Scene resets and camera framing across aspect ratios.

## 3. Automated Level Batch Validation
Runs `PuzzleRules.ValidateLevel` and `PuzzleSolver.Solve` against all `.asset` levels in `Assets/Game/Data/Levels/`.

