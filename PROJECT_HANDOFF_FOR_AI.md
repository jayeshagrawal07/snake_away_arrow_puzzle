# Snake Escape — Project Handoff for AI Tools

**Purpose:** Give an AI coding assistant a factual, concise map of the Unity project so it can continue work without replacing existing work or confusing the prototype with the original game.

**Project inspected:** October 24, 2026  
**Unity editor version:** `6000.6.1f1`  
**Primary language:** C#

## Project summary

Snake Escape is a 2D grid puzzle game. The player taps a snake to send it out in the direction its head faces. A snake cannot leave if another active snake occupies any cell along its straight exit route.

There are **two separate gameplay implementations** in this repository:

1. **Existing game / product baseline:** `Assets/snake chain game.unity`, using `Assets/Legacy/GameManager.cs` and `Assets/Legacy/SnakeController.cs`, along with its existing scene UI and assets.
2. **Newer modular implementation / prototype:** code under `Assets/Game/`. Its unused prototype scene was removed during cleanup and can be regenerated with the Editor setup menu.

The newer implementation has not replaced or been integrated into the original scene. Treat the original scene and its user-facing UI as the product to preserve. Reuse newer code selectively where it fits; do not assume that a feature in the prototype is available in the original game.

## Current build and entry scene

`ProjectSettings/EditorBuildSettings.asset` enables:

```text
Assets/snake chain game.unity
```

Its GUID matches `Assets/snake chain game.unity.meta`. `Assets/Scenes/SampleScene.unity`, which was previously referenced by build settings, does not exist.

The original scene contains a `GameManager` object and an EventSystem. `Tools > Snake Escape > Setup Gameplay Scene` can generate a separate modular prototype scene; it is not the configured build entry scene and should only be used intentionally because it replaces the currently open Editor scene.

## Repository structure

```text
Assets/
  Legacy/
    GameManager.cs                Legacy game flow, UI, levels, persistence, menus
    SnakeController.cs            Legacy snake rendering, interaction, animation
  snake chain game.unity          Existing game scene and current build entry
  [images and sprite assets]      Existing game artwork and UI assets
  Game/
    Scripts/
      Domain/                     Grid, snake/level definitions, occupancy, rules, solver
      Application/                Session, move, undo, level, progression services
      Presentation/               Modular board, snake, input, controller, HUD views
    Data/Levels/                  Level data location; no authored .asset campaign levels found
    Prefabs/                      Snake/UI folders; no authored prefab files found
    Audio/                        Music/SFX folders; no audio files found
  Tests/EditMode/
    Domain/                       Domain and solver tests
    Application/                  Application lifecycle and undo tests
  Editor/LevelEditor/              Sample-level generator and scene setup menu commands
Documentation/                    Design, architecture, authoring, testing, release docs
Packages/manifest.json            Unity package dependencies
ProjectSettings/                  Unity configuration and build-scene list
```

Unity-generated folders such as `Library/`, `Temp/`, and `Logs/` are not source-of-truth project content.

## What is already implemented

### Existing game implementation

- `Assets/Legacy/GameManager.cs` is a large legacy `MonoBehaviour` (about 147 KB) that currently owns much of the game flow: loading/generating levels, score/coins and `PlayerPrefs` persistence, menus and modals, pause, mistakes/lives, hints/guides, and completion handling.
- `Assets/Legacy/SnakeController.cs` (about 17 KB) implements the legacy snake visuals and interaction, path-distance hit checking, blocked-move feedback, and escape animation.
- The existing scene contains a configured GameManager with UI references and is the configured build entry scene.
- Existing image assets include UI icons and snake-game artwork.

These files are tightly coupled to each other and to the existing scene. Refactor them incrementally; do not delete or replace them before equivalent behavior is integrated and verified in the original scene.

### Existing-scene behavior map (static code trace)

This map was derived from the scene and scripts; it is not a Unity PlayMode verification.

1. **Startup and persistence:** `GameManager.Awake` initializes the singleton, loads level/hint/grid-guide/coin/challenge/haptics values from `PlayerPrefs`, and optionally resets fresh-install values. `Start` configures the camera, styles/creates runtime UI, then calls `LoadLevel(currentLevel)`.
2. **Level representation:** The original game does not load the modular `LevelDefinition` assets. Level 1 is built by `BuildTutorialLevel1`; other campaign levels are generated procedurally by `GenerateAddictiveLongSpineMaze`. The board dimensions and spacing are manager fields. Each `SnakeController` receives an ordered `List<Vector2Int>` grid path and derived world-space points via `InitSnake`. The first grid point is the head; the head-to-next-point difference determines the forward direction.
3. **Selection and input:** `GameManager.Update` handles touch/mouse input, ignores clicks over UI, and selects the nearest non-moving, non-cleared snake by distance to its rendered path. `SnakeController.TriggerTap` starts that snake's coroutine unless it or the game is paused.
4. **Route legality:** `GameManager.EvaluateEscapeMove` adapts the legacy head, direction, dimensions, and active-snake occupancy lookup to `EscapeRouteGeometry.EvaluateMove`. It returns a `MoveResult` (valid route or first blocking snake/cell) plus the count of free cells. The compatibility `CheckPathClearAndDistance` method remains for hint logic and derives its boolean from the result.
5. **Blocked move:** `SnakeController.ExecuteTapRoutine` consumes `MoveResult.IsValid`; on a blocked result it still uses the free-cell count for the existing advance, mistake penalty, impact/shake, and return animation. `OnMistake` decrements lives and updates hearts; at zero it schedules the game-over modal/reload flow. The blocker detail is available in the result but is not yet shown as new UI feedback. This remains a penalty in the current legacy game, unlike the no-punishment standard-mode target in the production specification.
6. **Successful move and completion:** The snake is marked cleared, then `GameManager.OnSnakeCleared` removes it from `activeSnakes`, updates the counter, awards coins on completion, and saves the next level. The snake coroutine continues animating off-board after this callback. The manager starts a three-second celebration before showing the win modal. Whether this timing always keeps the win modal behind the final escape animation needs a PlayMode/manual check.
7. **Restart and next level:** The pause modal's restart action calls `LoadLevel(currentLevel)` (or restarts challenge gameplay); `LoadLevel` clears active snake GameObjects and rebuilds the selected level. The win modal routes to `NextLevel`, which increments the level and calls `LoadLevel`.
8. **Pause and settings:** The runtime-created pause button calls `TogglePause`; the pause modal offers continue, restart, and main-menu actions. Settings buttons open the runtime-built settings modal.
9. **Hint and grid guide:** Runtime HUD button listeners call `GiveHint` and `UseGridGuide`. The hint highlights an available snake and consumes a saved hint. The grid guide is a separate assist with its own counter/save state.
10. **Undo:** No legacy undo control, undo stack, or undo method was found in `GameManager.cs` or `SnakeController.cs`. Undo exists only in the modular application code unless another scene/object outside this trace provides it.

**Initial behavioral risks to verify, not confirmed runtime bugs:** simultaneous escapes may be possible because the manager has no visible global moving-state gate; completion bookkeeping starts before the last snake's exit animation ends; blocked moves consume lives although the target specification says standard mode should not punish them. Preserve current behavior until product decisions and baseline tests are agreed.

**Completion-screen edge fix:** The win modal uses a dedicated square-edged sunburst sprite stretched to the full Canvas; popup cards continue using the rounded sunburst sprite. This prevents transparent rounded corners from exposing the layer behind the completion screen.

**Restart-during-escape cleanup:** `GameManager` now tracks all snakes in the level separately from `activeSnakes`, because an escaping snake leaves the active list before its animation ends. Level/challenge resets deactivate and destroy all tracked snakes. Resets also advance a level-generation token, clear active confetti, and prevent an old completion coroutine from showing a stale win screen. The user confirmed the restart/celebration behavior works in Play mode.

**Delayed game-over protection:** The delayed defeat-modal coroutine captures the current level-generation token and checks it after its wait. A restart or challenge transition invalidates the old coroutine, preventing the previous level's game-over modal from appearing on the new run. The user confirmed the game-over/restart case works in Play mode.

**Simulated-ad lifecycle:** Simulated ad playback captures the level-generation token. A level/challenge reset hides the ad overlay and invalidates the running coroutine, so it cannot show stale ad UI or invoke an old reward callback after reset. The user confirmed the reset-during-ad flow works in Play mode.

**Save-value hardening and migration:** `PlayerSaveService` stores a schema-v1, checksummed JSON snapshot under `SnakePuzzle_Save_v1` and continues mirroring the existing `SnakePuzzle_*` integer keys. If the snapshot is absent or invalid, it migrates available legacy values, clamps negative values, and uses defaults only when no legacy progress keys exist. The explicit inspector reset still resets to defaults. `GameManager` now routes progress reads/writes through the service, including the next-level completion save. All 6 focused save-service EditMode tests passed, covering defaults, migration, negative values, `int.MaxValue`, corrupt snapshots, and explicit reset. The full suite now passes 68/68 EditMode tests. Two earlier test failures were resolved by correcting a missing undo snapshot in the fixture and replacing an obsolete two-snake tutorial expectation with deterministic solution validation for the current generated level. Original-scene PlayMode persistence/relaunch checks are still pending.

The first full Unity batch-mode EditMode run after cleanup executed 62 tests with 60 passing and 2 failing. After correcting the stale fixtures, the latest full run passed all 68 tests. The earlier focused 27/27 `PuzzleRulesTests` result predates save hardening.

### Legacy-to-modular rule compatibility (static audit)

The original scene's baseline was manually checked by the user and reported as working. The details below compare source code; no new automated parity test or PlayMode test was run for this audit.

**Compatible core representation:** Both implementations use discrete integer grid cells, ordered paths with the head at index 0, cardinal movement toward a board edge, and other active snakes' occupied cells as blockers. The original generator passes ordered `Vector2Int` paths to `SnakeController.InitSnake`; the modular model stores equivalent ordered `GridCoord` paths. The coordinate axes also match: positive X/right and positive Y/up.

**Adaptation needed:** The legacy forward/exit direction is derived from the first two path cells (`head - nextBodyCell`), with `Up` as the one-cell-path fallback. `SnakeDefinition` stores `ExitDirection` separately from `BodyCells`. Any bridge must derive the direction from the legacy path and validate that it is a cardinal unit vector; it must not guess from the snake color, rendered head, or world-space curve.

**Important timing difference:** Legacy `OnSnakeCleared` removes a snake from `activeSnakes` when its escape animation starts. Other moves may therefore treat its cells as free while it is still animating. Modular `MoveService` reserves one move at a time and commits occupancy removal after animation completion. Adopting the modular behavior would intentionally alter move timing/concurrency; decide and test this before integration rather than assuming parity.

**Existing product-rule difference:** A blocked legacy move consumes a life. The production plan's standard-mode target says blocked moves should not punish the player. Do not change the shipped behavior as an incidental consequence of route-rule extraction; decide the intended standard-mode rule explicitly.

**Other integration boundaries:** Legacy board dimensions and world conversion are owned by `GameManager`; challenge levels can use different dimensions. The adapter uses current dimensions and keeps world-space animation separate from logical coordinates. The legacy move now receives the shared structured result while the animation remains owned by the current presentation.

**Agreed behavior for the first integration slice:** Preserve the legacy life penalty for blocked moves and preserve the ability to start another snake while an earlier escape animation is in progress. The shared route-geometry/result adapter reuses the same grid dimensions, head, derived direction, and blocker lookup; it does not replace legacy move scheduling, life handling, or animation. The user asked us to assume basic clear/blocked/concurrent escape behavior works. They also confirmed the restart/celebration, game-over/restart, and restart-during-simulated-ad PlayMode checks work; these are user-reported manual checks.

**Implemented slices (source/compile/test verified):** `EscapeRouteGeometry.ComputeInBoundsRoute` supplies the common cardinal grid ray. `CountClearCellsBeforeBlock` supplies first-blocker traversal/count, and `EscapeRouteGeometry.EvaluateMove` creates the shared valid/blocked `MoveResult`. Legacy occupancy lookup remains in `GameManager`, preserving selected/cleared-snake exclusions and concurrent move scheduling. An earlier Unity batch-mode run passed all 27 focused `PuzzleRulesTests` (0 failed, 0 skipped), including route parity and clear/blocked result payloads. `Assembly-CSharp` and `SnakeEscape.Tests.EditMode` builds succeeded with 0 warnings/errors after serialized builds. The latest full Unity batch-mode EditMode run passed all 68 tests; earlier failures were corrected in the test fixtures. Restart cleanup, stale-celebration invalidation, delayed game-over invalidation, and simulated-ad cancellation are user-confirmed in Play mode.

### Newer modular implementation

**Domain** (`Assets/Game/Scripts/Domain/`):

- `GridCoord`, `SnakeDefinition`, `LevelDefinition`
- `OccupancyMap`, `PuzzleState`, `PuzzleRules`, `MoveResult`
- `PuzzleSolver` with deterministic breadth-first search and a bounded search limit
- `LongSpineMazeGenerator` for generated puzzle levels

**Application** (`Assets/Game/Scripts/Application/`):

- `GameSession`, `MoveService`, `UndoService`
- `LevelService`, `ProgressionService`

**Presentation** (`Assets/Game/Scripts/Presentation/`):

- `GameController`
- Board layout, rendering, and view components
- Snake rendering, animation, hit selection, and input
- Programmatically constructed gameplay HUD and completion UI

**Editor tooling** (`Assets/Editor/LevelEditor/LevelAssetCreator.cs`):

- Unity menu command to create the separate modular gameplay scene
- Unity menu command to generate three solver-checked sample level assets
- This is not the planned visual path-drawing level editor.

The newer `GameController` currently uses `LongSpineMazeGenerator` directly. The canonical authored-level path is `Assets/Resources/Levels/`; the sample editor generator and `LevelService` share this path. Three sample assets are present and their serialized snake geometry, structural validity, solver results, and Resources loading passed the Unity EditMode suite (72/72). Built-player loading remains to be checked.

## Tests and validation evidence

EditMode test source files are under `Assets/Tests/EditMode/`:

- `GridCoordTests`
- `OccupancyMapTests`
- `PuzzleRulesTests`
- `PuzzleSolverTests`
- `CampaignValidationTests`
- `ApplicationTests`
- `PlayerSaveServiceTests`
- `LevelServiceTests` and `LevelServiceResourceTests`

The Unity EditMode suite ran successfully with:

```powershell
Unity.exe -batchmode -nographics -projectPath "<project>" -runTests -testPlatform EditMode
```

The latest run passed 72/72 tests. This does **not** confirm that either scene was play-tested or that authored levels load in a built player.

## Known gaps and cautions

- The modular implementation remains separate from the existing game; its systems must not be described as already powering the existing scene. Its unused prototype scene was removed during cleanup and can be regenerated from the Editor menu when needed.
- The legacy `GameManager`/`SnakeController` path has no known integration with the modular `GameSession` or `PuzzleRules`.
- Three sample campaign `.asset` files are generated under `Assets/Resources/Levels`; the modular `GameController` still generates puzzles procedurally.
- The authored-level path is shared, but loading is not yet integrated into the modular controller or the existing product scene.
- The modular HUD exposes some button events (including pause/settings/hint/guide-related UI), but not all are wired by the modular controller.
- The modular `ProgressionService` exists, but is not used by the current modular `GameController` level loop.
- `README.md` and `PROGRESS.md` previously described the modular prototype as completed work for the product. They have been corrected to distinguish the inventory from existing-scene integration; continue to keep status claims evidence-based.
- The production plan in `SNAKE_ESCAPE_PRODUCTION_PLAN.md` remains a target specification, not proof that its milestones or release requirements are complete.
- The working tree may contain unrelated or in-progress modifications. Always inspect `git status` and diffs before editing; preserve changes you did not create.

## Current migration direction

1. Preserve `Assets/snake chain game.unity` and its existing presentation.
2. Map its current UI actions and gameplay lifecycle before changing runtime behavior.
3. Improve one behavior at a time in the original scene, retaining existing input and visuals where possible.
4. Verify normal moves, blocked moves, undo, restart, and completion in the original scene after relevant changes.
5. Reuse the modular domain logic only when its coordinate/path rules are compatible with legacy level data.
6. Remove or replace legacy responsibilities only after the integrated behavior has parity and has been verified.

Do not regenerate the original scene, replace its UI with the prototype HUD, migrate every subsystem in one pass, or delete legacy code as a cleanup shortcut.

## Instructions for an AI coding assistant

Before editing:

1. Read this handoff, `PROGRESS.md`, `README.md`, and the relevant production-plan sections.
2. Inspect the target scene, scripts, serialized references, and current `git status`.
3. State the specific behavior being changed and list the files expected to change.
4. Keep the change scoped to the existing scene unless the user explicitly requests otherwise.
5. Add or update a focused test where feasible, then report exactly which tests/builds were actually run.
6. Do not claim PlayMode or device validation unless performed.
7. Ask before making a significant design or migration decision.
