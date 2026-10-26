# Testing Strategy — Snake Escape

## 1. EditMode Unit Tests
Located under `Assets/Tests/EditMode/`.
Cover pure domain rules and puzzle solving without launching a scene:
- `GridCoordTests`: Vector math, equality, bounds, cardinal direction checks.
- `OccupancyMapTests`: Cell reservations, occupant retrieval, vacate transactions.
- `PuzzleRulesTests`: Escape route calculation, blocked moves, win detection, level structural validation.
- `PuzzleSolverTests`: BFS state solver, dead-end detection, replay validation.
- `ApplicationTests`: `MoveService`, `UndoService`, and `GameSession` state lifecycle.
- `PlayerSaveServiceTests`: defaults, legacy migration, invalid-save fallback, clamping, and explicit reset.
- `LevelServiceTests`: campaign sorting and ID lookup.
- `LevelServiceResourceTests`: loading an authored asset from the canonical Resources path.
- `LegacyLevelGeneratorTests`: verifies procedural level generation does not consume the game's global Unity random state.

## 2. PlayMode Tests (Milestone 8)
Located under `Assets/Tests/PlayMode/`.
Will cover:
- Snake rendering and animation completion triggers.
- Input blocking during active movement.
- Scene resets and camera framing across aspect ratios.

## 3. Automated Level Batch Validation
`CampaignValidationTests.AuthoredCampaign_AllResourcesHaveValidGeometry_AndAreSolvable` loads the authored `.asset` levels from `Assets/Resources/Levels/` and checks that every snake has serialized body cells, each level passes `PuzzleRules.ValidateLevel`, and `PuzzleSolver.Solve` finds a complete solution. The three current sample assets passed as part of the 72/72 EditMode run. The editor generator replaces those three named sample assets when run again.

`LevelServiceResourceTests` separately creates a temporary level and verifies `LevelService` loads it from the canonical Resources path. A built-player resource-loading check remains part of release validation.

The legacy generator regression test verifies the seeded-random scope used by the existing `GameManager` generator leaves the caller's Unity random sequence unchanged. It passed as part of the 73/73 Unity EditMode run.
