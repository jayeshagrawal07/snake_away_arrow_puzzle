# Snake Escape — Final Execution Plan

**Purpose:** The single working plan for continuing development from the repository's current state to a tested Android release, while preserving the existing game and integrating the modular implementation safely.

**Basis:** Reconciles `SNAKE_ESCAPE_PRODUCTION_PLAN.md` (target specification) with `PROJECT_HANDOFF_FOR_AI.md` (repository inventory inspected 24 October 2026).

**Project:** Snake Escape, 2D portrait mobile puzzle game  
**Engine:** Existing Unity editor version `6000.6.1f1` (confirm installed editor and project compatibility before changing versions)  
**Language:** C#  
**Primary platform:** Android  
**Secondary platform:** iOS preparation after Android gameplay is stable

---

## 1. Final decision and guiding rule

**Keep `Assets/snake chain game.unity` as the product baseline. Integrate and verify improvements in that scene one behavior at a time. Do not replace it with the separately generated modular prototype scene, regenerate the original scene, or perform a wholesale rewrite.**

The repository has two distinct implementations:

1. **Legacy/product implementation:** `Assets/snake chain game.unity`, `Assets/Legacy/GameManager.cs`, `Assets/Legacy/SnakeController.cs`, existing artwork and serialized UI.
2. **Modular implementation/prototype:** `Assets/Game/`, including domain rules, solver, application services, presentation components, tests, and a separate gameplay scene.

The modular code is reusable material, not proof that those systems power the current product. Before integrating any module, compare its coordinate, path, collision, and state-transition assumptions with the original level format and gameplay behavior.

### Non-negotiable safety rules

- Inspect `git status`, diffs, relevant scripts, scenes, prefabs, serialized references, and project settings before edits.
- Preserve existing user work and original scene/UI. Never overwrite or delete code merely to make the architecture look cleaner.
- Make one small, reviewable change at a time. State the behavior, expected files, and acceptance checks before each milestone.
- Keep gameplay rules independent of Unity rendering and animation where practical.
- Never claim a build, test, scene playtest, device test, or store-readiness check passed unless it was actually run.
- If Unity Editor execution is unavailable, label results as static inspection or C# compilation only.
- No advertising, analytics, purchase SDK, paid asset, or new third-party dependency without documenting its purpose, cost/licensing, privacy implications, and obtaining approval.
- Fix regressions and release-blocking failures before starting unrelated work.

---

## 2. Current-state baseline

### Existing work identified in the handoff

- Original scene is the configured build entry: `Assets/snake chain game.unity`.
- Legacy `GameManager.cs` handles substantial game flow, levels, score/coins, `PlayerPrefs`, menus/modals, pause, hints/guides, mistakes/lives, and completion.
- Legacy `SnakeController.cs` handles snake visuals, path-distance hit detection, blocked feedback, and escape animation.
- Modular domain code includes grid coordinates, snake/level definitions, occupancy, puzzle rules/state, move results, a bounded deterministic breadth-first solver, and a procedural `LongSpineMazeGenerator`.
- Modular application code includes session, move, undo, level, and progression services.
- Modular presentation code includes a separate controller, board and snake views, input, HUD, and completion UI.
- EditMode test source files exist for grid coordinates, occupancy, puzzle rules, solver, campaign validation, and application behavior.
- The Unity EditMode suite has passed 72/72 tests, including authored campaign geometry/solver validation. Neither scene has been play-tested as part of this validation.

### Known issues to resolve or explicitly track

1. The legacy implementation is tightly coupled and has no known integration with modular `GameSession` / `PuzzleRules`.
2. Three authored sample `.asset` levels are generated and validated; the modular controller still generates puzzles procedurally.
3. The generator and `LevelService` now share `Assets/Resources/Levels/`; loading in a built player remains unverified.
4. Some modular HUD events (including pause/settings/hint/guide-related actions) are not wired through the modular controller.
5. Modular `ProgressionService` is not used by the modular controller's current level loop.
6. The configured original scene must be tested directly; modular prototype behavior cannot be used as evidence for original-scene behavior.
7. Current Git working-tree state must be rechecked before every implementation batch.

Treat this section as an initial inventory, not a guarantee that the repository has not changed since inspection.

---

## 3. Product scope and gameplay rules

### First-release core loop

The player taps a snake to make it escape in the direction its head faces. A move is permitted only if the defined exit route is clear of other active snakes. A blocked move gives clear, restrained feedback without corrupting the board. A successful snake moves head-first and its body follows its established path. The level is complete only after every snake has escaped and the final escape animation has finished.

### Rule decisions

- Logical occupancy and path data—not Unity physics—are authoritative for move legality.
- Use one canonical path representation. Rendered curves derive from the logical path.
- The initial rules use deterministic grid-based route validation. The mapping between curved artwork and occupied grid cells must be explicit and visually consistent.
- A selected snake must not block itself. Escaped snakes no longer occupy cells.
- Rejected moves must not mutate puzzle state or occupancy.
- Reserve an accepted move before animation starts. Only one escape animation runs at a time in the initial release.
- Block input while moving, paused, loading, or transitioning.
- Restart restores the exact initial level state. Undo, if retained in the product, restores the last committed move and is unavailable during movement unless cancellation and restoration are fully safe.
- Completion saves progress and displays the next-level action only after the final escape completes.
- The core game must remain playable offline.

**Agreed legacy behavior for the first route-geometry integration:** blocked moves continue to consume a life, and another snake may start moving while a prior escape animation is in progress. These are user-approved compatibility decisions for this migration slice, overriding the corresponding production-plan defaults unless the user later requests a behavior change. Do not use the modular `MoveService` single-move lock as a drop-in replacement.

### First-release features

**Required:** existing-scene gameplay, deterministic move validation, blocked-move feedback, escape animation, restart, current core menu/HUD, level completion, reliable local progress persistence, level validation, automated tests, Android build and device QA, documentation.

**Keep if already present and stable:** current hints/guides, score/coins, lives/mistakes, existing menu/modals and other established product behavior. Map these before refactoring; do not silently remove them.

**Required production tooling before large-scale campaign production:** deterministic solver, level validation report, repeatable level asset loading, and a usable authoring workflow. The visual drag/draw level editor is a target; first make validation and asset loading reliable, then build the editor.

**Later/optional:** rewarded ads for optional hints, remove-ads purchase, cosmetics, analytics, cloud saves, extra content packs, iOS release. Monetization must not delay proving and stabilizing the core game.

---

## 4. Target architecture

Keep the legacy scene and presentation during migration, but move toward these ownership boundaries:

- **Domain (pure C#):** grid coordinates, level/snake definitions, occupancy, puzzle state, move legality, win detection, solver, validation, undo snapshots.
- **Application:** start/restart level, accept move commands, reserve/commit moves, coordinate animation completion, manage state transitions, update progress/save.
- **Presentation (Unity):** original scene/UI, board and snake rendering, touch selection, animation, audio/haptics, menus and HUD.
- **Data/infrastructure:** level assets, campaign metadata, versioned local saves, settings and logging.
- **Editor/tools:** level authoring, preview, validation, solver execution and campaign batch reports.

Do not force a large refactor to achieve this diagram. Establish seams around individual behaviors while retaining existing serialized scene references and user-facing presentation.

### Level data and loading decision

**Decision:** Use `Assets/Resources/Levels/` as the canonical authored-level location. The editor generator writes there, and `LevelService` loads from `Resources/Levels`, so Unity includes these assets in player builds. Do not duplicate level assets to hide path mismatches. EditMode coverage verifies Editor loading; add and run a built-player check proving the same authored level loads there.

---

## 5. Execution roadmap and acceptance gates

Complete these phases in order. Each phase ends with a report of files changed, tests actually run, outcomes, known limitations, and the next proposed phase. Do not advance past a failed gate without an explicit decision.

### Phase 0 — Re-audit and establish a safe baseline

**Tasks**
- Read `README.md`, `PROGRESS.md`, this plan, and relevant architecture/testing docs.
- Inspect `git status`, staged and unstaged diffs, Unity version, package manifest, build settings, original scene, serialized references, legacy scripts, and modular scripts.
- Open the configured original scene in Unity and record whether it loads and which current flows work.
- Preserve the current worktree exactly as found. Review staged, unstaged, and untracked changes to identify existing work; do not commit, stash, reset, clean, or otherwise alter it without the user's explicit approval.
- After the user confirms how to handle existing changes, establish a separate checkpoint/branch if appropriate. A branch alone does not preserve or clarify uncommitted changes.
- Record baseline behavior and known failures.

**Gate:** No code changes until the working-tree state and original-scene baseline are understood. Report Unity Editor availability and what could not be verified.

### Phase 1 — Characterize the original game

**Tasks**
- Trace the original scene's startup, level loading, snake selection, blocked move, successful escape, completion, restart, undo (if present), hint/guide, pause, settings, and save/progression flows.
- Identify the actual level representation and how its paths map to occupied cells/escape routes.
- Map UI button events and serialized references to their owning scripts.
- Write focused tests around rules that can be extracted without changing presentation. Add a concise behavior map to documentation.

**Gate:** Existing core flows are documented, and critical regressions can be detected. No visual redesign or large rewrite in this phase.

### Phase 2 — Verify and harden the modular domain engine

**Tasks**
- Audit `GridCoord`, definitions, `OccupancyMap`, `PuzzleState`, `PuzzleRules`, `MoveResult`, and `PuzzleSolver` against the actual legacy rules.
- Add/repair EditMode tests for route boundaries, long snakes, edge exits, self-blocking exclusion, overlap rejection, escaped snakes, repeated input/reservations, invalid data, no-mutation-on-rejection, win detection, and solver search-limit semantics.
- Ensure solver outcomes distinguish solvable, proven unsolvable, invalid data, and search limit reached.
- Compare modular and legacy outcomes on representative level fixtures before adopting shared logic.

**Gate:** Domain behavior is deterministic and tested, and compatibility gaps are documented. Do not integrate incompatible path rules just because the code compiles.

### Phase 3 — First integration slice in the original scene

**Tasks**
- Select one bounded behavior—preferably route validation for a single snake move—as the first integration slice.
- Route that behavior through one application-level entry point while preserving the original scene, input feel, visuals, and serialized UI.
- Keep animation separate from legality. Reserve a valid move, animate, then commit state exactly once.
- Keep the legacy implementation available until parity is verified; use an adapter/bridge if it is safer than replacing a subsystem.
- Add regression tests and manually test valid and blocked moves in the original scene.

**Gate:** Original scene still loads; valid and blocked moves behave correctly; rejected moves do not alter state; no duplicate occupancy or input race is observed. If not, revert or fix this slice before proceeding.

### Phase 4 — Integrate lifecycle and state reliability

**Tasks**
- Integrate restart, undo (if included), pause/resume, level transitions, completion, and save/progression incrementally.
- Define and test explicit states: initializing, ready, moving, paused, level complete, loading, and error/invalid level.
- Prevent input during transitions and ensure restart/scene changes cancel or safely reset animation and temporary effects.
- Preserve established features such as hints, guides, score/coins, lives/mistakes, and menus. Wire each feature to the new path only after its behavior is mapped and tested.
- Add save versioning and safe handling for missing or malformed data without losing normal progress.

**Gate:** The original game survives repeated moves, blocked taps, restart during movement, pause/resume, repeated completion, and app relaunch without softlocks or unexpected progress loss.

### Phase 5 — Resolve level pipeline; build validation and solver workflow

**Tasks**
- Choose one canonical level asset location/loading mechanism and resolve the generator/`LevelService` mismatch.
- Add stable level IDs, board dimensions, snake paths, difficulty/learning metadata, content version, and validation status as appropriate.
- Implement level geometry checks, boundary/path continuity checks, self- and inter-snake overlap checks, escape-route checks, and solution replay.
- Run deterministic solver and batch validation against all authored levels. Save actionable reports with level ID and precise errors.
- Ensure search-limit results are never mislabeled as proof of unsolvability.
- Add a simple reliable authoring workflow first; then implement the visual Unity Editor path-drawing/preview tool, duplication, solver preview, and report export.

**Gate:** An authored level loads in the player build, invalid levels produce useful errors, solver results are reproducible, and a level can be validated and replayed from a fresh state.

**Current status:** The generator and `LevelService` now share `Assets/Resources/Levels/`; three serialized sample levels pass geometry, solver, and Editor Resources-loading checks in the 72/72 EditMode suite. Built-player loading and integration with the modular controller remain open.

### Phase 6 — Campaign production and progression

**Tasks**
- Produce the target initial campaign of 30 levels only after the pipeline is reliable.
- Suggested progression: 1–5 basic interaction; 6–10 one blocker; 11–15 dependency chains; 16–20 longer snakes/denser layouts; 21–25 longer reasoning sequences; 26–30 advanced combinations.
- Measure difficulty using more than snake count: initial blocked count, shortest solution length, solution alternatives, dependency depth, density, plausible wrong moves, and playtest completion time.
- Validate every level, replay its solution, inspect its visual readability, and manually playtest it.
- Wire level selection and progression to the existing product UI rather than substituting the prototype HUD without approval.

**Gate:** All published levels are valid, solvable, visually readable, replayable from a fresh state, and playable from beginning to end. No campaign level is marked verified solely because it was generated.

### Phase 7 — UX, art, audio, accessibility

**Tasks**
- Polish the existing visual identity rather than replacing it without approval.
- Ensure heads and forward directions are unambiguous; paths and collision behavior agree visually.
- Refine selection, blocked feedback, successful escape, completion, button interactions, and transitions.
- Check portrait layouts, safe areas, different aspect ratios, readable/scalable text, touch target sizes, contrast, reduced motion where practical, and feedback not dependent on color alone.
- Centralize sound effects/music and settings; add optional haptics only where supported.

**Gate:** UI works on representative screen sizes; feedback is clear and restrained; audio/haptics can be disabled; no UX change breaks gameplay rules.

### Phase 8 — QA, performance and Android release candidate

**Tasks**
- Run available EditMode and PlayMode suites in Unity Test Runner; record exact results.
- Run campaign batch validation and solution replay.
- Test the original build entry scene and actual Android builds on representative devices.
- Test rapid repeated taps, slow devices, small/large screens, aspect ratios, app background/resume, muted audio, restart during transitions, save corruption recovery, repeated completion, and low-memory behavior where feasible.
- Profile before adding complex optimization. Fix critical bugs, incorrect collision, softlocks, crashes, progression loss, and unusable controls.
- Configure portrait orientation, application ID, icon/splash, target API requirements, signing outside version control, privacy disclosures, store assets, release notes, and rollback/hotfix steps.

**Gate:** A reproducible Android release candidate has been built and tested; no known release-blocking bugs remain; test evidence and known limitations are documented.

### Phase 9 — Monetization and optional platform expansion

**Tasks, only after Phase 8's core gate**
- Evaluate rewarded ads for optional hints and optionally a remove-ads purchase.
- Before adding an SDK, document provider, current platform requirements, consent/disclosures, data collected, privacy-policy changes, test mode, failure/offline behavior, and recurring cost. Obtain explicit approval first.
- Ensure ads never block the core puzzle or make the game unusable offline; avoid accidental taps and test ad failure/return-to-game flows.
- Prepare iOS only after Android core behavior is stable, including Apple signing/provisioning and device validation. Do not claim iOS readiness without an actual build and test.

**Gate:** Monetization/platform changes pass their own compliance, integration, failure-mode, and device tests. If these requirements are not met, defer them rather than delaying or risking the core release.

---

## 6. Test and release policy

### Automated tests

- **Domain/EditMode:** coordinate conversion, occupancy rebuild/update, route clear/blocked, boundary cells, long bodies, overlaps, invalid paths/dimensions, escaped snakes, no mutation on rejected moves, win condition, undo restoration, solver correctness and search limits.
- **Application/EditMode:** legal state transitions, move reservation/commit, input gating, restart, undo, progression, save version/malformed data handling.
- **PlayMode:** original scene startup, selection, rendering, blocked feedback, escape animation, input blocking, restart during animation, completion UI, scene transitions, save restoration, resolution/safe-area changes.
- **Campaign:** batch geometry validation, solver result, solution replay, deterministic outcomes, metadata checks.

### Manual QA checklist

- [ ] Original configured scene opens and starts.
- [ ] Legal move clears the correct route and commits exactly once.
- [ ] Blocked move explains/indicates the blocker and changes no logical state.
- [ ] Rapid taps cannot cause simultaneous moves or duplicate removals.
- [ ] Restart and pause/resume are safe during transitions.
- [ ] Undo, if enabled, restores the complete prior state.
- [ ] Final animation finishes before completion screen appears.
- [ ] Progress persists after app restart; malformed save recovery is safe.
- [ ] Every campaign level passes automated validation and manual review.
- [ ] Portrait UI is usable on multiple screen sizes/aspect ratios.
- [ ] Android release build launches and is tested on real device(s).
- [ ] Privacy/store disclosures and third-party licenses are reviewed.

A successful `.csproj` build is not a substitute for Unity Test Runner results, scene playtesting, or device testing.

---

## 7. Documentation to keep current

Maintain and update, rather than recreate unnecessarily:

- `README.md`: overview, exact Unity version, setup, run/build/test instructions, current status.
- `PROGRESS.md`: phase status, changes, actual checks run, blockers, next task.
- `Documentation/GAME_DESIGN.md`: rules, controls, progression, visual direction, accessibility and scope.
- `Documentation/ARCHITECTURE.md`: layer ownership, state transitions, dependencies, migration decisions.
- `Documentation/LEVEL_AUTHORING.md`: data format, paths/exits, validation, solver and solution review.
- `Documentation/TESTING.md`: test setup, commands, release checks, known limitations.
- `Documentation/BUILD_AND_RELEASE.md`: build configuration, signing, privacy/store steps, rollback/hotfix process.
- `Documentation/CHANGELOG.md`: user-visible and technical changes by increment.

Documentation must distinguish **implemented**, **compiled**, **automated-tested**, **Unity-play-tested**, **device-tested**, and **release-verified**. These are different levels of evidence.

---

## 8. Required format for every AI coding-agent task

Before editing, the agent must report:
1. Current Git status and relevant existing changes.
2. The single behavior or issue being addressed.
3. Expected files to change and why.
4. Compatibility/regression risks and tests to run.

After editing, report:
1. Files changed and summary of behavior.
2. Commands/tests actually executed and their exact results.
3. Whether Unity compilation, Unity Test Runner, scene playtest, and device test were actually performed.
4. Remaining limitations, known bugs, and next recommended task.
5. Any new dependency, asset/license, privacy, or cost implication.

Wait for approval before major architectural changes, deleting legacy responsibilities, replacing the original UI/scene, or adding monetization/analytics dependencies.

---

## 9. Immediate next task

**Phase 0/1 audit and the first route/result integration slice are complete.** The original scene remains the product baseline. The user asked us to assume that clear escapes, blocked-move return/life penalty, and concurrent escapes continue working; this is an accepted working assumption, not a PlayMode verification after the latest code change.

`EscapeRouteGeometry` now supplies the route projection, first-blocker count, and valid/blocked `MoveResult` used by the legacy manager and `SnakeController`. Legacy occupancy filtering, life handling, blocked animation, and concurrent-move scheduling remain in their existing owners. Serialized builds of `Assembly-CSharp.csproj` and `SnakeEscape.Tests.EditMode.csproj` succeeded with 0 warnings/errors. Unity batch-mode EditMode testing passed all 27 `PuzzleRulesTests` (0 failed, 0 skipped), including route parity and move-result payload tests. The user confirmed the completion-screen edge fix looks correct in Game view.

The Phase 4 static trace identified a restart-during-escape cleanup gap: escaped snakes are removed from `activeSnakes` before their animation completes, while level restart previously destroyed only `activeSnakes`. The manager now tracks every level snake for reset cleanup; level-generation tokens also invalidate old victory/confetti routines on restart or challenge transition. The user confirmed restart/celebration behavior in Play mode. A further trace found the delayed game-over coroutine could display an old modal after restart; it now checks the level-generation token after its delay. The user confirmed that game-over followed by restart and restart during a simulated ad work as expected.

The delayed-game-over guard compiled successfully in `Assembly-CSharp.csproj`; the EditMode test assembly also built with 0 warnings/errors. Unity batch-mode testing passed all 27 `PuzzleRulesTests` (0 failed, 0 skipped). The user confirmed that both game-over-followed-by-restart and restart-during-simulated-ad flows work in Play mode. This is user-reported manual validation, not automated regression coverage.

The pause/in-flight-effects audit also found that simulated-ad coroutines use unscaled time and can outlive a level reset. They now capture the level-generation token, hide their overlay on reset, and skip stale reward callbacks. The user confirmed the flow works in Play mode.

The first save-hardening slice clamps negative saved level/resource/currency counters and saturates coin rewards at `int.MaxValue`. `PlayerSaveService` adds a checksummed schema-v1 JSON snapshot while retaining the existing `SnakePuzzle_*` keys as mirrors. It migrates valid legacy values when the snapshot is missing or invalid, preserves legacy values if the fresh-start marker is absent, and only initializes defaults when no legacy progress fields exist (unless the explicit reset flag is set). All 6 focused save-service EditMode tests passed. Original-scene PlayMode persistence/relaunch checks remain pending.

A full Unity batch-mode EditMode run after these changes compiled the project and passed all 68 tests (0 failed, 0 skipped). Two earlier test failures were resolved by correcting the undo fixture's missing snapshot and updating a stale two-snake sample-level expectation to validate the current generated level deterministically. The earlier focused 27/27 `PuzzleRulesTests` run predates this save change.

**Next:** Validate authored campaign levels with the solver and level validator, then confirm the Resources asset loads in a built player. The user confirmed original-scene save persistence works after Play-mode relaunch. Authored-level loading remains modular and is not integrated into the original product scene.

Preserve all existing worktree changes.

**Final priority:** Preserve the product → characterize behavior → verify domain rules → integrate one behavior at a time → stabilize lifecycle/save → fix level pipeline and validator → produce verified campaign → polish → QA/release → optional monetization.
