# Architecture Document — Snake Escape

## 1. Architectural Layers

The project follows a 4-layer architecture with strict separation of concerns and unidirectional dependencies:

```
[Presentation Layer]
  (Unity Scene, BoardView, SnakeView, SnakeRenderer, SnakeAnimator, HUD)
        │
        ▼
[Application Layer]
  (GameSession, MoveService, UndoService, LevelService, ProgressionService)
        │
        ▼
[Domain Layer]
  (GridCoord, SnakeDefinition, LevelDefinition, OccupancyMap, PuzzleRules, PuzzleState, PuzzleSolver)
        ▲
        │
   [Data Layer]
  (ScriptableObjects, Level assets, PlayerPrefs/JSON)
```

### Layer Rules
1. **Domain Layer (`SnakeEscape.Domain`)**: Pure C# logic. Zero dependencies on Unity scene objects or UI. Fully testable in headless NUnit EditMode tests.
2. **Application Layer (`SnakeEscape.Application`)**: Coordinates puzzle lifecycle, move execution state (`Ready`, `Moving`, `LevelComplete`), and history/undo snapshots.
3. **Presentation Layer (`SnakeEscape.Presentation`)**: Handles rendering (`LineRenderer`, procedural heads, materials), input raycasts, tweens/animations, and UI view controllers.
4. **Data Layer (`SnakeEscape.Data`)**: Stores serializable level configurations and save game state.

## 2. Assembly Definitions (asmdef)
- `SnakeEscape.Domain`
- `SnakeEscape.Application` (references `SnakeEscape.Domain`)
- `SnakeEscape.Tests.EditMode` (references Domain & Application)
- Presentation & Infrastructure will be configured in subsequent milestones.
