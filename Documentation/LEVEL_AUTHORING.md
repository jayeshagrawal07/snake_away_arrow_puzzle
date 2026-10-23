# Level Authoring Guide — Snake Escape

## Creating Levels
Levels are stored as `LevelDefinition` ScriptableObjects under `Assets/Game/Data/Levels/`.

### Parameters
- `LevelId`: Unique alphanumeric identifier (e.g. `Level_001`).
- `Cols` & `Rows`: Board dimensions (standard: 8x8).
- `Snakes`: Collection of `SnakeDefinition` items.
  - `Id`: Local snake identifier (e.g. `snake_0`).
  - `BodyCells`: Ordered coordinates from Head (index 0) to Tail. Every step must be cardinally adjacent.
  - `ExitDirection`: Cardinal unit direction (`(1,0)`, `(-1,0)`, `(0,1)`, `(0,-1)`).
  - `BodyColor`: Display tint.

## Validation & Solving
Every level asset can be validated programmatically:
1. `PuzzleRules.ValidateLevel(level)`: Ensures path continuity, bounds compliance, and no duplicate/overlapping cells.
2. `PuzzleSolver.Solve(level)`: Deterministic BFS search that guarantees a solution sequence exists before release.
