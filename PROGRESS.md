# 📊 Snake Escape — Project Progress & Task Tracker

> Specification reference: [`SNAKE_ESCAPE_PRODUCTION_PLAN.md`](./SNAKE_ESCAPE_PRODUCTION_PLAN.md)  
> Last updated: **October 26, 2026**

---

## Current Direction: Improve the Existing Game

The existing scene, `Assets/snake chain game.unity`, is the product starting point. It contains the legacy `GameManager`-driven game and its existing UI. The newer `Assets/Game/` architecture remains separate and has not replaced or been integrated into the existing scene. Its unused prototype scene was removed during cleanup; the Editor setup menu can recreate it. Do not delete or regenerate the existing scene/UI as part of cleanup.

The former overall-completion percentage and milestone checkmarks have been removed because they mixed progress on the separate implementation with progress on the actual game.

| Area | Current evidence-based status |
|:---|:---|
| Existing game | Scene and legacy gameplay scripts are present; preserve them as the baseline. |
| New puzzle engine | Domain, application, and presentation code exists separately; migrate selectively where it improves the existing game. |
| Existing-scene integration | Not complete. The original scene still uses the legacy scripts. |
| Build launch scene | Configured to `Assets/snake chain game.unity`. |
| Campaign, authoring, release readiness | Not verified as complete; modular level data remains separate from the original game. |

## Migration Plan

1. **Preserve and baseline** — Keep the existing scene and UI, identify the current gameplay flows, and establish what must continue working.
2. **Integrate one behavior at a time** — Start with snake selection/move validation and the already-reported undo behavior. Keep existing visuals and input where possible; route rules through the reusable domain logic only where compatible.
3. **Verify each integration** — Test normal moves, blocked moves, undo (including a snake restored to a cell another route crosses), restart, and level completion in the existing scene.
4. **Continue by subsystem** — Migrate progression/save behavior and other responsibilities only after the move flow is stable.
5. **Clean up after parity** — Remove duplicate or obsolete code only after the existing scene uses the replacement and its behavior is verified.

No whole-scene rewrite or broad cleanup is planned.

---

## Existing Implementation Inventory (Not Yet Migrated)

The checklist below describes components found in the newer `Assets/Game/` implementation. It is an inventory of existing work, not evidence that those components are running in `Assets/snake chain game.unity` or that the corresponding production milestone is complete.

### Milestone 1: Project Foundation
- [x] Standardized folder architecture established under `Assets/Game/`, `Assets/Tests/`, and `Documentation/`.
- [x] Configured Assembly Definitions for modular isolation:
  - `SnakeEscape.Domain.asmdef`
  - `SnakeEscape.Application.asmdef`
  - `SnakeEscape.Presentation.asmdef`
  - `SnakeEscape.Editor.asmdef`
  - `SnakeEscape.Tests.EditMode.asmdef`
- [x] Created project documentation:
  - [`README.md`](./README.md)
  - [`Documentation/GAME_DESIGN.md`](./Documentation/GAME_DESIGN.md)
  - [`Documentation/ARCHITECTURE.md`](./Documentation/ARCHITECTURE.md)
  - [`Documentation/LEVEL_AUTHORING.md`](./Documentation/LEVEL_AUTHORING.md)
  - [`Documentation/TESTING.md`](./Documentation/TESTING.md)
  - [`Documentation/BUILD_AND_RELEASE.md`](./Documentation/BUILD_AND_RELEASE.md)
  - [`Documentation/CHANGELOG.md`](./Documentation/CHANGELOG.md)

### Milestone 2: Pure Puzzle Engine
- [x] **Grid System**: [`GridCoord.cs`](./Assets/Game/Scripts/Domain/GridCoord.cs) — Immutable coordinates, cardinal directions (`Up`, `Down`, `Left`, `Right`), adjacency tests.
- [x] **Data Definitions**:
  - [`SnakeDefinition.cs`](./Assets/Game/Scripts/Domain/SnakeDefinition.cs) — Snake body coordinates, head, tail, exit direction, validation.
  - [`LevelDefinition.cs`](./Assets/Game/Scripts/Domain/LevelDefinition.cs) — `ScriptableObject` containing level metadata, dimensions, and snake list.
- [x] **Occupancy & Collisions**: [`OccupancyMap.cs`](./Assets/Game/Scripts/Domain/OccupancyMap.cs) — Transactional grid cell occupancy tracking.
- [x] **Move Outcome Contracts**: [`MoveResult.cs`](./Assets/Game/Scripts/Domain/MoveResult.cs) — Status enum (`Valid`, `BlockedBySnake`, `SnakeAlreadyEscaped`, etc.) and payload.
- [x] **State Machine & Rules**:
  - [`PuzzleState.cs`](./Assets/Game/Scripts/Domain/PuzzleState.cs) — Active snakes, commit escape, undo escape, cloning.
  - [`PuzzleRules.cs`](./Assets/Game/Scripts/Domain/PuzzleRules.cs) — Deterministic escape route calculation, obstacle evaluation, win condition, level geometry validator.
- [x] **Automated Solver**: [`PuzzleSolver.cs`](./Assets/Game/Scripts/Domain/PuzzleSolver.cs) — Deterministic BFS search with state memoization (up to 200k states).
- [x] **Application Services**:
  - [`MoveService.cs`](./Assets/Game/Scripts/Application/MoveService.cs) — Single entry point for moves with input locking during animations.
  - [`UndoService.cs`](./Assets/Game/Scripts/Application/UndoService.cs) — State snapshot stack for undo.
  - [`GameSession.cs`](./Assets/Game/Scripts/Application/GameSession.cs) — Session lifecycle (`Ready`, `Moving`, `Paused`, `LevelComplete`).
  - [`LevelService.cs`](./Assets/Game/Scripts/Application/LevelService.cs) — Campaign asset loader.
  - [`ProgressionService.cs`](./Assets/Game/Scripts/Application/ProgressionService.cs) — Unlocks & completion persistence.
- [x] **Unit Testing**:
  - Full EditMode test suite covering grid math, occupancy, rules, move validation, BFS solver, and session lifecycle.
  - Verified 0 compilation errors.

### Milestone 3: First Playable Level (Clean Presentation Layer)
- [x] **Board Presentation**:
  - [`BoardLayout.cs`](./Assets/Game/Scripts/Presentation/Board/BoardLayout.cs) — Grid-to-world conversion, centering, dimensions.
  - [`GridRenderer.cs`](./Assets/Game/Scripts/Presentation/Board/GridRenderer.cs) — Procedural round dot grid background.
  - [`BoardView.cs`](./Assets/Game/Scripts/Presentation/Board/BoardView.cs) — Snake spawning, dictionary tracking, layout lifecycle.
- [x] **Snake Presentation**:
  - [`SnakeRenderer.cs`](./Assets/Game/Scripts/Presentation/Snake/SnakeRenderer.cs) — Smooth corner LineRenderer with width taper, anti-aliased texture, and procedural viper head with tongue.
  - [`SnakeAnimator.cs`](./Assets/Game/Scripts/Presentation/Snake/SnakeAnimator.cs) — Slither motion along escape trajectory & bump/impact feedback on blocked route.
  - [`SnakeView.cs`](./Assets/Game/Scripts/Presentation/Snake/SnakeView.cs) — Composite component for distance-to-segment tap targets.
- [x] **Input & Orchestration**:
  - [`SnakeInputHandler.cs`](./Assets/Game/Scripts/Presentation/Snake/SnakeInputHandler.cs) — Touch & click detection, nearest snake resolver.
  - [`GameController.cs`](./Assets/Game/Scripts/Presentation/GameController.cs) — Master presentation runner connecting `GameSession` with views, handling undo/restart, and built-in sample level generator.
  - [`GameplayHUD.cs`](./Assets/Game/Scripts/Presentation/UI/GameplayHUD.cs) & [`LevelCompleteUI.cs`](./Assets/Game/Scripts/Presentation/UI/LevelCompleteUI.cs) — Decoupled UI views.
- [x] **Authoring & Tooling**:
  - [`LevelAssetCreator.cs`](./Assets/Editor/LevelEditor/LevelAssetCreator.cs) — Editor tool (`Tools > Snake Escape > Generate Sample Levels`) to create solver-validated `LevelDefinition` ScriptableObject assets.
  - [`CampaignValidationTests.cs`](./Assets/Tests/EditMode/Domain/CampaignValidationTests.cs) — Automated test proving level 1 solver verification.

### Milestone 4: Reliable Gameplay Systems
- [x] **Production Visual Alignment (Matched Image 2)**:
  - Corrected snake head scale to sleek `1.45x` proportion (eliminated the bloated 5.2x debug balloon head).
  - Adopted deep crisp navy-black (`#0B0E14`) color styling.
  - Multi-sample Bezier interpolation for smooth, silky snake curves.
  - Dot grid rendered selectively under snakes and boundaries rather than entire empty canvas.
  - Pure crisp white camera background (`#FFFFFF`).
- [x] **Production HUD Layout (Matched Image 2)**:
  - Top row: Circular blue Pause button, Level title, Circular blue Settings button.
  - Row 2: Snake counter badge, 3 Red Hearts container (`♥♥♥`), Difficulty pill ("Easy", "Medium", "Hard").
  - Bottom row: Circular Hint lightbulb button with `+` badge, Circular Ruler guide button with `+` badge, and clean Undo pill.
- [x] **Long-Spine Interlocking Maze Generator**:
  - [`LongSpineMazeGenerator.cs`](./Assets/Game/Scripts/Domain/LongSpineMazeGenerator.cs) — Reverse-topological carving creating rich 8-snake to 65-snake mazes (e.g. Level 10) that are guaranteed solvable.
- [x] **Auto-Constructing UI Views**:
  - [`GameplayHUD.cs`](./Assets/Game/Scripts/Presentation/UI/GameplayHUD.cs) — Dynamic top bar (Level title, snake count badge) and bottom bar (Undo & Restart buttons).
  - [`LevelCompleteUI.cs`](./Assets/Game/Scripts/Presentation/UI/LevelCompleteUI.cs) — Centered completion card with "Next Level ▶" and "Play Again" buttons.
- [x] **Interactive Undo & Restart**:
  - Instant snapshot restore via `UndoService` respawning escaped snakes in original poses.
  - Full board reset via `RestartLevel`.
- [x] **Input Guards**:
  - Blocked repeated rapid taps while moves are in flight via `Session.CurrentState == Ready`.
- [x] **Multi-Level Progression**:
  - Seamless campaign loop across Level 1, Level 2, and Level 3 on completion.
- [x] **Scene Setup Automation**:
  - Added `Tools > Snake Escape > Setup Gameplay Scene` menu item to generate the modular prototype scene on demand.

---

## Current Sprint: Existing-Scene Audit and Incremental Integration

- [x] Fix completion-screen background edges in the original UI by using square-edged full-screen artwork while preserving rounded popup-card artwork.
- [x] User confirmed the completion-screen edges look correct in Unity Game view.
- [ ] Verify the completion screen on a device or a different aspect ratio.
- [x] Confirm the existing scene contains the legacy game manager and EventSystem.
- [x] Configure the existing scene as the enabled build scene.
- [x] Clarify in project documentation that the newer gameplay scene is separate and not the migration target.
- [x] Map the original scene's startup, level representation, input, route check, blocked/successful moves, completion, restart, pause, hint/guide, and persistence flows by static inspection.
- [x] Compare legacy and modular grid/path/exit rules; document direction and move-timing mismatches before integration.
- [x] User reported the original scene's clear escape, blocked-move penalty/return, and concurrent escape behavior worked after the first shared-route extraction (before the latest edits).
- [x] Share cardinal in-bounds route geometry between modular rules and legacy route checking without changing legacy penalties, animations, or concurrent-move behavior.
- [x] Extract first-blocker route traversal/count for both modular geometry and legacy route checking; keep legacy snake occupancy lookup and move timing in `GameManager`.
- [x] Adapt the original scene's legacy occupancy check to shared `MoveResult` results without changing the life penalty, blocked animation, or concurrent escape scheduling.
- [x] Fix restart/challenge cleanup so snakes already animating out are also deactivated and destroyed.
- [x] Invalidate old completion/confetti effects when a new level or challenge starts, preventing stale celebration UI after restart.
- [x] User confirmed the restart/celebration lifecycle checks are working in Play mode; this is manual user-reported validation.
- [x] Guard delayed game-over UI with the level-generation token so an old defeat coroutine cannot show over a restarted level or challenge.
- [x] Cancel simulated-ad overlays and suppress stale reward callbacks when the level or challenge resets during an ad.
- [x] User confirmed the delayed-game-over/restart and restart-during-simulated-ad flows work in Play mode.
- [x] Clamp negative saved level/resource/currency counters during load and save; saturate coin rewards instead of allowing integer overflow.
- [x] Build `Assembly-CSharp` and `SnakeEscape.Tests.EditMode` after the save-value hardening (0 warnings/errors).
- [x] Add schema-v1 `PlayerSaveService` with a checksummed JSON snapshot, legacy-key mirrors, migration fallback, and an explicit reset path.
- [x] Preserve legacy progress when `SnakePuzzle_FreshStart_V8` is missing but existing progress keys are present; use defaults only when no legacy save fields exist.
- [x] Add and run PlayerSaveService EditMode tests for first-run defaults, legacy migration, negative-value normalization, `int.MaxValue` coins, corrupt snapshot fallback, and explicit reset (6/6 passed).
- [x] User confirmed original-scene save persistence works after Play-mode relaunch.
- [x] Choose `Assets/Resources/Levels/` as the canonical authored-level folder; sample generation and runtime loading now share this Resources path.
- [x] Add Editor resource-loading coverage plus campaign sorting and ID-lookup tests.
- [x] Fix `GridCoord` serialization so generated level assets retain snake-body coordinates; regenerate the three sample levels in `Assets/Resources/Levels/`.
- [x] Validate every authored sample level’s saved geometry and solver result in Unity EditMode; full suite passed 72/72.
- [ ] Verify that the same authored level loads in a built player; the current resource test covers Editor loading only.
- [x] Compile original `Assembly-CSharp` and modular EditMode test assemblies after the route/result changes (0 warnings/errors; built sequentially).
- [x] User asked to assume original-scene clear, blocked, and concurrent escape behavior remains working; no PlayMode check was run after the latest integration.
- [x] Unity batch-mode EditMode run passed all 27 `PuzzleRulesTests` (0 failed, 0 skipped), including blocker-count, legacy parity, and valid/blocked result payload tests.
- [x] Add a legacy path-conversion parity fixture for all cardinal directions and first-blocker counting.
- [x] Compile the updated EditMode test assembly after adding the parity fixture (0 warnings/errors); subsequent Unity batch-mode execution confirms the focused tests passed.
- [x] Run blocker-count, parity-fixture, and result-payload tests in Unity batch-mode Test Runner.
- [x] Build the delayed-game-over guard and EditMode test assembly (both builds completed with 0 warnings/errors).
- [x] Unity batch-mode EditMode regression run passed all 27 `PuzzleRulesTests` (0 failed, 0 skipped).
- [x] Correct the undo regression fixture to record the pre-move state for each move being undone; assert occupancy follows the escaped-snake invariant.
- [x] Update the generated-level solver test to verify deterministic complete solution coverage rather than the obsolete two-snake tutorial order.
- [x] Run the full Unity batch-mode EditMode suite after save-service and test updates; all 68 tests passed (0 failed, 0 skipped).
- [ ] Manually test save migration, invalid snapshot fallback, and app relaunch in the original scene.
- [x] User confirmed the restart/celebration, game-over/restart, and restart-during-simulated-ad lifecycle checks are working in Play mode.
- [x] Record source/build/EditMode-test evidence and the remaining PlayMode limitation before continuing lifecycle work.

---

## 📌 Backlog & Parking Lot ("Do Later" TODOs)

*Add any features or side-tasks here so we never lose track of them while staying focused on current milestone.*

### Audio & Haptics (M7)
- [ ] Sound effects: tap, blocked bump, slither escape, level win celebration, button click.
- [ ] Ambient background music (calm, minimal loop).
- [ ] Platform haptics implementation (Android `Vibrator` / iOS `UIFeedbackGenerator`).

### Monetization & Ads (Optional / Post-M6)
- [ ] Replace prototype ad simulation overlay with real SDK (Unity Ads or Google AdMob).
- [ ] Rewarded ads for hints / extra grid guides.
- [ ] In-App Purchase: Remove Ads option.

### Progression & Persistence Enhancements (M6)
- [ ] Replace PlayerPrefs with atomic JSON file persistence (`SaveService.cs`).
- [ ] Cloud save synchronization (Google Play Games / Apple Game Center).

### Advanced Mechanics & Game Modes (Post-Launch)
- [ ] Challenge Mode (levels with move limits or countdown timer).
- [ ] Obstacles / Rocks on the grid.
- [ ] Two-headed snakes or looping snakes.
- [ ] Snake skin cosmetics & themes (color palettes).
- [ ] Multiplayer untangling race (placeholder modal exists in prototype).

### Technical Debt / Cleanup
- [ ] Refactor responsibilities out of legacy `GameManager.cs` incrementally, only after replacement behavior is integrated and verified in the existing scene.
- [ ] Convert procedural button generation to modular UI prefabs.
