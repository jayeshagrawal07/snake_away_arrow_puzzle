# 📊 Snake Escape — Project Progress & Task Tracker

> Specification reference: [`SNAKE_ESCAPE_PRODUCTION_PLAN.md`](./SNAKE_ESCAPE_PRODUCTION_PLAN.md)  
> Last updated: **November 20, 2026**

---

## Current Direction: Improve the Existing Game

The existing scene, `Assets/snake chain game.unity`, is the product starting point. It contains the legacy `GameManager`-driven game and its existing UI. The newer `Assets/Game/` architecture remains separate and has not replaced or been integrated into the existing scene. Its unused prototype scene was removed during cleanup; the Editor setup menu can recreate it. Do not delete or regenerate the existing scene/UI as part of cleanup.

The former overall-completion percentage and milestone checkmarks have been removed because they mixed progress on the separate implementation with progress on the actual game.

| Area | Current evidence-based status |
|:---|:---|
| Existing game | Scene and legacy gameplay scripts are present; preserve them as the baseline. |
| New puzzle engine | Domain, application, and presentation code exists separately; use only when it addresses a demonstrated need in the existing game. |
| Existing-scene integration | Not complete. The original scene still uses the legacy scripts. |
| Build launch scene | Configured to `Assets/snake chain game.unity`. |
| Campaign, authoring, release readiness | Not verified as complete; modular level data remains separate from the original game. |

**Architectural decision (October 26, 2026):** Continue using the legacy game and its existing procedural level generator. Reuse and incrementally improve existing code before considering a parallel replacement. The modular authored-level samples are a separate prototype experiment, not the next product integration task. Defer their player-build validation unless a concrete need arises and is discussed.

## Latest Existing-Scene UI and Rendering Update

- Rebuilt the active legacy home dashboard and added a scrollable Challenges screen in `Assets/Legacy/GameManager.cs`; the existing scene and procedural level/save systems remain the runtime path.
- Added Rush, Zero Hearts, One-Heart Survival, and level-30 Move Master challenge entries, plus the Coming Soon multiplayer notice. The existing challenge wallet/reward flow remains in use.
- Updated the gameplay HUD layout, including an integrated snake-count progress bar, clock icon without the unsupported timer emoji, and circular booster controls. The Undo control is currently a visible placeholder notice; the legacy game still has no undo implementation.
- Wired `Assets/Glossy Glowing Light Bulb Icon.png` as the gameplay Hint sprite and configured it as a bilinear, no-mipmap, uncompressed UI sprite.
- Reduced the configured legacy slither speed to `11f`, added launch acceleration, refined the line taper, and enabled 4x MSAA in the project quality settings and runtime camera setup.
- Configured the active Canvas for 1080x1920 Scale With Screen Size, Match 0.5, and non-pixel-perfect rendering.
- Verification: `dotnet build Assembly-CSharp.csproj --no-restore` succeeded on November 20, 2026 with 0 warnings and 0 errors. This verifies C# compilation only; no post-change Unity PlayMode or Android device validation was run.

## Migration Plan

1. **Preserve and baseline** — Keep the existing scene, UI, procedural level generator, and gameplay flows; establish what must continue working.
2. **Improve the existing implementation** — Reuse and incrementally fix or clean up the legacy systems. Avoid parallel replacements unless a concrete limitation requires one.
3. **Verify each change** — Test normal moves, blocked moves, undo (including a snake restored to a cell another route crosses), restart, level generation/progression, and completion in the existing scene.
4. **Extract or replace only when justified** — Use modular code only when it provides a needed improvement and its behavior is verified against the existing game.
5. **Clean up after parity** — Remove duplicate or obsolete code only after its replacement is integrated and verified.

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
- [x] Confirm the game should continue supporting both portrait and landscape orientations.
- [x] Update the existing legacy CanvasScaler to match screen height in landscape and retain the existing portrait scaling behavior.
- [x] Apply dynamic safe-area insets to edge-anchored UI under full-screen panels and refresh layout when the screen size, orientation, safe area, or canvas scale changes.
- [x] Compile the legacy gameplay assembly after the responsive UI change (0 warnings/errors).
- [x] Rerun the full Unity EditMode suite after the responsive UI change; 76/76 passed (0 failed, 0 skipped). This is logic/compilation coverage, not visual layout validation.
- [x] User reports visually verifying the original scene in portrait and landscape. Specific devices/aspect ratios and observed results were not recorded.
- [x] Add centralized procedural sound effects to the existing legacy game for UI buttons, blocked moves, successful escapes, and level completion.
- [x] Replace blocked-move and successful-escape tones with distinct breathy, raspier hiss effects synthesized from filtered broadband noise; retain the UI and level-complete tones.
- [ ] Audition the updated hiss effects in Unity and on a device; consider a properly licensed recording if greater realism is needed.
- [x] Add a persisted Sound Effects ON/OFF setting with migration from version-1 saves; existing saves default to sound enabled without losing progress.
- [x] Compile the legacy gameplay assembly after audio integration (0 warnings/errors).
- [x] Verify save settings migration and round-trip in focused EditMode tests (9/9 passed); run the complete EditMode suite (77/77 passed).
- [x] User confirmed the original synthesized sound effects and sound setting work in the original scene. The new hiss effects are not yet auditioned; background music remains unimplemented.
- [x] Reduce legacy snake movement speed and refine the body taper at the tail.
- [x] Replace the level's 50% completion popup with a live completion progress bar.
- [x] Keep the existing ruler icon as the grid control; it toggles visibility after use and offers a grid guide for 100 coins or a simulated ad when no free guides remain.
- [x] Add a target-selecting eraser: one free use per campaign level or challenge run, then 100 coins or the existing simulated rewarded-ad flow. The free-use marker is persisted without changing the saved level or wallet.
- [x] Refresh modal cards to a blue/white palette, remove the duplicate home Settings button, rename the home card to Challenge, and simplify Play with Friends to a Coming Soon message.
- [x] Reuse the existing persisted coin wallet and level/challenge rewards; eraser use is the new coin sink.
- [x] Upgrade the save snapshot to schema v3 with migration coverage for v1/v2 and round-trip coverage for eraser use.
- [x] Compile `Assembly-CSharp` and `SnakeEscape.Tests.EditMode` after these changes (0 warnings/errors).
- [x] Run the full Unity EditMode suite after the latest HUD and popup changes: 78/78 passed, 0 failed, 0 skipped (Unity Test Runner, EditMode, October 31, 2026).
- [x] Fix the live completion progress bar by assigning its fill sprite and slowing the fill animation; the full EditMode suite remained 78/78.
- [x] User confirmed the updated Hint popup, HUD count-chip alignment, and current gameplay/UI changes work in the original scene in Play mode. This is manual user-reported validation, not device QA.
- [x] Configure Android publisher/display name (`Snake Puzzle`) and normalized package ID (`com.lynos.snake.arrow.puzzle.away.jam`) with user confirmation.
- [x] Build an Android Development APK with the progress-bar fix and verify its package ID, display label, SDK range, and v2 signature. This is not a release-signed build.
- [x] Show the existing Snake Puzzle welcome/main menu when the app launches; load the saved level behind it so Continue Game resumes the player's progress.
- [x] Build a fresh Android Development APK including the startup-menu change; verify app metadata and v2 signature (November 1, 2026).
- [ ] Install and test the APK on an Android device when one is available; none was connected during this run.
- [ ] Resolve or device-validate Unity's Android warning that Active Input Handling is set to Both; the original scene currently uses the new Input System UI module and legacy `Input` calls.
- [x] Remove the duplicate text grid button and show the eraser as a circular icon button beside the hint and existing ruler.
- [x] Match the eraser HUD button to the hint/ruler circular style and add a blue count badge (`1` before the free use, `+` after).
- [x] Fix the eraser popup layout: its shared icon-button helper used bottom anchoring even inside the centered popup card, which made the free option overlap Cancel. The popup free option now uses centered anchoring and has separated label/action spacing.
- [x] In both guide/eraser popups, display the tool artwork as a large standalone icon rather than a round HUD button. The eraser artwork is enlarged to a similar visible scale as the ruler preview and has no count badge or “Free Eraser Used” text; the HUD eraser keeps its availability badge.
- [x] Match the eraser popup card height, dim overlay, button order, button dimensions/colors/font sizes, and Close placement to the Grid Guide popup; enlarge the eraser artwork to a similar visible scale as the ruler icon.
- [x] Make the Hint popup consistent with Grid Guide: use standalone hint artwork without the HUD plate/count chip and match title/action button styling. Use shared badge geometry and centered text for the Hint, ruler, and eraser HUD chips.
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
- [x] Review the existing legacy procedural level generator; keep its seeded layouts deterministic while avoiding side effects on unrelated Unity randomness.
- [x] Isolate deterministic legacy level generation from Unity's global random stream so generating a level does not shift random behavior elsewhere.
- [x] Add the legacy random-state regression test; both gameplay and EditMode test assemblies compile with 0 warnings/errors.
- [x] Run the full Unity EditMode suite with the legacy generator regression test; 73/73 passed (0 failed, 0 skipped).
- [x] Extract legacy snake-path carving from scene spawning so levels can be audited without loading a scene or touching saved progress.
- [x] Audit seeded levels 2, 5, 6, 7, and 13 for repeatability, bounds, continuity, overlap, domain validation, and carve-order solvability; all checks passed.
- [x] Build the legacy gameplay and EditMode test assemblies; the sequential EditMode test build passed with 0 warnings/errors (the first parallel attempt encountered a shared intermediate-file lock).
- [x] Run the complete Unity batch-mode EditMode suite after the generator audit; 74/74 passed (0 failed, 0 skipped).
- [x] Add save/progression regressions for Level 14 snapshot round-trip and corrupted versioned-save checksum fallback; focused Unity EditMode tests passed 8/8.
- [x] Run the full Unity batch-mode EditMode suite after save/progression QA; 76/76 passed (0 failed, 0 skipped).
- [ ] Playtest levels 5–7 in the original scene to judge the large board/count change at Level 6 and the lower occupied-cell ratio at Level 7; do not alter generator policy without a demonstrated gameplay issue.
- [ ] Built-player validation of the separate modular authored-level pipeline is deferred; revisit only if that pipeline is selected for a concrete product need.
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
- [x] Add procedural UI-button and level-complete tones plus breathy broadband-noise hiss effects for blocked moves and successful escapes. The original effects were user-confirmed; the updated hiss still needs auditioning.
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
- [x] Add legacy challenge modes for timed, mistake-free, one-heart, and Move Master runs.
- [ ] Obstacles / Rocks on the grid.
- [ ] Two-headed snakes or looping snakes.
- [ ] Snake skin cosmetics & themes (color palettes).
- [ ] Multiplayer untangling race (placeholder modal exists in prototype).

### Technical Debt / Cleanup
- [ ] Refactor responsibilities out of legacy `GameManager.cs` incrementally, only after replacement behavior is integrated and verified in the existing scene.
- [ ] Convert procedural button generation to modular UI prefabs.
