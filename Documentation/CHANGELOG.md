# Changelog — Snake Escape

## [Unreleased]
### Added
- **Milestone 1: Project Foundation**
  - Standardized directory layout under `Assets/Game/`, `Assets/Tests/`, and `Documentation/`.
  - Added asmdefs for `SnakeEscape.Domain` and `SnakeEscape.Application`.
  - Added core documentation set (`README`, `GAME_DESIGN`, `ARCHITECTURE`, etc.).
- **Milestone 2: Pure Puzzle Engine**
  - Created `GridCoord` struct with immutable cardinal math.
  - Created `SnakeDefinition` and `LevelDefinition` ScriptableObjects.
  - Implemented `OccupancyMap` for deterministic grid occupancy.
  - Implemented `PuzzleRules` for escape route calculation, move validation, and structural checking.
  - Implemented `PuzzleSolver` deterministic BFS solver.
  - Implemented `MoveService`, `UndoService`, `GameSession`, `LevelService`, and `ProgressionService`.
  - Added full NUnit EditMode test suite covering domain rules and solver mechanics.

