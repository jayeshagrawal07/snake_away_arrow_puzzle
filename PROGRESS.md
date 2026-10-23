# 📊 Snake Escape — Project Progress & Task Tracker

> Specification reference: [`SNAKE_ESCAPE_PRODUCTION_PLAN.md`](./SNAKE_ESCAPE_PRODUCTION_PLAN.md)  
> Last updated: **October 24, 2026**

---

## 📈 Milestone Overview

```
[████████████████████░] ~65% Complete Overall
```

| Milestone | Description | Status | Target / Notes |
|:---|:---|:---:|:---|
| **M1** | Repository Audit & Project Foundation | ✅ Complete | Directory structure, asmdefs, docs |
| **M2** | Pure Puzzle Engine (Domain & App Layer) | ✅ Complete | Pure C# logic, rules, solver, EditMode tests |
| **M3** | First Playable Level (Clean Presentation) | ✅ Complete | BoardView, SnakeView, SnakeRenderer, input |
| **M4** | Reliable Gameplay Systems | ✅ Complete | Undo, restart, pause, transition guards, scene tooling |
| **M5** | Level Authoring & Solver Tooling | 🟡 In Progress | Visual level editor window, campaign batch validator |
| **M6** | Campaign & Progression | ⏳ Pending | 30 verified levels, save/load, level select |
| **M7** | UI, Art & Audio Polish | ⏳ Pending | SFX, BGM, haptics, responsive HUD |
| **M8** | QA & Optimization | ⏳ Pending | PlayMode tests, device profiling, edge cases |
| **M9** | Release Preparation | ⏳ Pending | Android/iOS build config, store assets, privacy |

---

## ✅ Completed Work

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
  - Added `Tools > Snake Escape > Setup Gameplay Scene` menu item to create `Assets/Game/Scenes/Gameplay.unity` in one click.

---

## 🟡 Current Sprint: Milestone 3 (First Playable Level)

- [ ] **Board Presentation**:
  - Create `BoardView.cs` (calculates world-space grid positioning, responsive camera bounds).
  - Create `GridRenderer.cs` (subtle dots/lines rendering for cell alignment).
- [ ] **Snake Presentation**:
  - Create `SnakeRenderer.cs` (port clean anti-aliased tapered `LineRenderer` from prototype).
  - Create `SnakeAnimator.cs` (slither motion along escape route, head-first following).
  - Create `SnakeView.cs` (composite component handling tap detection & visual states).
- [ ] **Input & Game Wiring**:
  - Connect tap/click raycasting directly to `GameSession.RequestMove()`.
  - Handle move feedback (blocked shake vs. smooth escape slither).
  - Trigger win feedback when `GameSession.OnLevelComplete` fires.

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
- [ ] Gradually decommission and delete legacy God Object `GameManager.cs` once new modular presentation layer is active.
- [ ] Convert procedural button generation to modular UI prefabs.

