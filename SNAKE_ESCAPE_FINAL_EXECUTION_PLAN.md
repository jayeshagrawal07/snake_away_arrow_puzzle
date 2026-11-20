# Snake Escape — Final Execution Plan

**Purpose:** The single working plan for continuing development from the repository's current state to a tested Android release, prioritizing the existing game and improving its systems incrementally.

**Basis:** Reconciles `SNAKE_ESCAPE_PRODUCTION_PLAN.md` (target specification), `PROJECT_HANDOFF_FOR_AI.md` (repository inventory), and the user's direction recorded October 26, 2026.

**Project:** Snake Escape, 2D portrait mobile puzzle game  
**Engine:** Existing Unity editor version `6000.6.1f1` (confirm installed editor and project compatibility before changing versions)  
**Language:** C#  
**Primary platform:** Android  
**Secondary platform:** iOS preparation after Android gameplay is stable

---

## 1. Final decision and guiding rule

**Keep `Assets/snake chain game.unity`, its legacy systems, and its procedural level generator as the product baseline. Prefer reusing, fixing, and cleaning up those systems over building a parallel implementation. Improve and verify one behavior at a time. Do not replace the original with the modular prototype, regenerate the original scene, or perform a wholesale rewrite.**

The repository has two distinct implementations:

1. **Legacy/product implementation:** `Assets/snake chain game.unity`, `Assets/Legacy/GameManager.cs`, `Assets/Legacy/SnakeController.cs`, existing artwork and serialized UI.
2. **Modular implementation/prototype:** `Assets/Game/`, including domain rules, solver, application services, presentation components, tests, and a separate gameplay scene.

The modular code is optional reusable material, not proof that those systems power the current product or a reason to duplicate existing functionality. Use it only when a concrete need is identified; compare its coordinate, path, collision, and state-transition assumptions with the original level format and gameplay behavior, and discuss significant architecture changes first.

### Non-negotiable safety rules

- Inspect `git status`, diffs, relevant scripts, scenes, prefabs, serialized references, and project settings before edits.
- Preserve existing user work and original scene/UI. Never overwrite or delete code merely to make the architecture look cleaner.
- Prefer extending the existing legacy generator and systems. Make one small, reviewable change at a time. State the behavior, expected files, and acceptance checks before each milestone.
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
- The Unity EditMode suite has passed 77/77 tests, including authored campaign geometry/solver checks, legacy generator audits, save migration/round-trip/checksum fallback, and sound-setting persistence. These automated tests are not scene or device tests.

### Known issues to resolve or explicitly track

1. The legacy implementation is tightly coupled; refactor only around specific maintenance or behavior needs while preserving its existing gameplay.
2. Three authored sample `.asset` levels exist for the separate modular prototype; they are not the product campaign.
3. The modular `Resources/Levels` loader was tested in the Editor; built-player validation is deferred unless the modular pipeline is selected for a concrete need.
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

### Existing level-generation decision

**Product decision:** Continue using the existing procedural generator in `Assets/Legacy/GameManager.cs`. Preserve and improve it when a concrete issue is identified. The three assets under `Assets/Resources/Levels/` are a separate modular prototype experiment, not a replacement or required product pipeline. Do not spend time integrating or validating that optional pipeline as a release path unless a specific need is established and discussed with the user.

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

### Phase 2 — Verify shared rules where they are used

**Tasks**
- Identify a specific need in the existing game before reusing or extracting shared rule logic.
- For shared logic already used by the legacy game, add or retain focused EditMode coverage for route boundaries, blockers, escaped snakes, invalid input, and side-effect-free rejected moves.
- Compare shared-rule outcomes with existing legacy behavior before changing who owns gameplay state or movement timing.
- Leave unrelated modular-only features alone unless they address a demonstrated product need.

**Gate:** Any shared logic used in the original game is deterministic, tested, and behavior-compatible. Do not integrate modular services just because they compile.

### Phase 3 — Incremental improvements in the original scene

**Tasks**
- Select one concrete issue or improvement in the legacy game per change.
- Prefer extending or correcting the existing owner over routing the behavior through a new application layer.
- Preserve the original scene, input feel, visuals, serialized UI, and established gameplay decisions.
- Add focused regression tests and manually verify affected behavior in the original scene.
- Extract or replace a legacy responsibility only when a demonstrated need justifies it and behavior parity can be verified.

**Gate:** The original scene still loads and the targeted improvement works without regressions to the surrounding legacy gameplay.

### Phase 4 — Maintain lifecycle and state reliability

**Tasks**
- Preserve the existing lifecycle, save/progression, and UI behavior while correcting specific reliability issues.
- Define and test lifecycle states only where this helps fix a real transition or state bug.
- Prevent stale animations, callbacks, or overlays from affecting a restarted level or a later transition.
- Keep established hints, guides, score/coins, lives/mistakes, and menus working; do not wire them to a replacement path without an agreed need.
- Maintain safe save migration and malformed-data fallback without losing normal progress.

**Gate:** The original game survives repeated moves, blocked taps, restart during movement, pause/resume, repeated completion, and app relaunch without softlocks or unexpected progress loss.

### Phase 5 — Maintain and improve existing level generation

**Tasks**
- Keep the existing legacy procedural level generator as the product source of levels.
- When level generation is in scope, inspect existing generation, progression, and edge cases before proposing changes.
- Fix demonstrated generator bugs or clean up existing code without changing established behavior unintentionally.
- Add targeted tests or deterministic checks for changes to the existing generator and verify generated levels in the original scene.
- Consider authored assets, a solver pipeline, or editor-authoring tools only if a specific limitation in the existing approach is established and the user agrees with that direction.

**Gate:** Existing levels remain correctly generated and playable through the original game's progression, and any generator changes are covered by relevant checks and verified in context.

**Current status:** The existing product continues using its legacy procedural generator. Its seeded generation now restores Unity's global random state, the regression test passes, and the user confirmed the change works in Play mode. Three levels in `Assets/Resources/Levels/` belong to a separate modular prototype experiment; they are not the next product task. Per the user's direction, built-player loading and integration of this optional pipeline are deferred.

### Phase 6 — Campaign quality and progression

**Tasks**
- Assess the current procedural level progression and player experience in the existing game before setting a separate authored-campaign target.
- Improve the legacy generator and progression if testing identifies concrete quality, variety, difficulty, or reliability issues.
- Preserve the existing product UI and saved progress while testing changes.
- Consider hand-authored campaigns or level-selection UI only when there's a demonstrated requirement and an agreed design.

**Gate:** The existing progression produces playable, understandable levels and retains reliable progress; any broader campaign change is justified and tested before adoption.

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

**Current status:** The original scene and legacy procedural generator remain the product baseline. Seeded path carving is separately callable for audits, while the original scene-spawning path retains the same generator policy. Both gameplay and EditMode test assemblies compiled. Save QA now verifies a Level 14 snapshot and resource round-trip across service instances and checksum-corruption fallback to legacy data; it uses an in-memory store and does not touch the user's actual saved progress. Focused save tests passed **8/8**, and the full Unity EditMode suite passed **76/76**. Audited levels 2, 5, 6, 7, and 13 were repeatable, in-bounds, continuous, non-overlapping, domain-valid, and solvable in carve order. Their occupied cells were respectively 41/42, 90/90, 228/288, 157/320, and 196/396. The user elected to assume the generator audit is correct without a manual Level 5–7 playtest. Save persistence after relaunch and the previous generator change have been user-confirmed in Play mode. These checks do not claim device or release-build validation.

**Current UI review:** The user confirmed both portrait and landscape remain supported. The legacy canvas now matches reference height in landscape while retaining its existing portrait scaling mode, and edge-anchored elements inside full-screen panels apply dynamic `Screen.safeArea` insets when resolution, orientation, safe area, or canvas scale changes. Project settings already allow portrait and landscape rotation. `Assembly-CSharp.csproj` compiled with 0 warnings/errors, and the full Unity EditMode suite passed **76/76** after this change. The user reports visually verifying the original scene in portrait and landscape; exact devices/aspect ratios and findings were not recorded. This remains user-reported visual evidence, not independently reproduced device QA.

**Audio work:** The existing legacy game now generates short runtime sound effects for UI buttons, blocked moves, successful snake escapes, and level completion. A Settings toggle disables/enables effects; the preference was introduced in schema v2 and remains persisted in v3, with migration paths preserving prior progress and preferences. No third-party audio assets or packages were added. The focused save/settings suite passed **9/9**, and the full Unity EditMode suite passed **77/77** after audio integration. The user confirmed the sounds and setting work in the original scene. Background music remains unimplemented.

**Requested legacy-game polish:** The game now has slower snake movement with a refined tapered tail, blue/white modal cards, a progress bar instead of the 50% completion popup, and a target-selecting eraser. The existing ruler remains the only grid control; after activating a guide it toggles grid visibility, and when no free guides remain it offers one guide for 100 existing coins or via the prototype simulated rewarded-ad flow. The eraser matches the hint/ruler round icon style and uses a blue HUD badge (`1` for the free use and `+` after use). Its popup now matches the Grid Guide popup card height, dim overlay, action order, action button dimensions/colors/font sizes, and Close placement; its standalone eraser artwork is enlarged to a similar visible scale as the ruler preview and has no count badge. The Hint popup now uses standalone hint artwork and matching action-button styling; Hint, ruler, and eraser HUD chips share badge geometry and centered text. Erasing is free once per campaign level or challenge run, then costs 100 existing coins or uses the simulated ad. The free eraser marker is saved in schema v3; valid v1/v2 saves migrate while preserving prior progress. The home screen has one Settings button, labels the mode “Challenge,” and opens a simple Coming Soon message for Play with Friends. The gameplay assembly compiles with 0 warnings/errors. The user confirmed the recent gameplay/UI changes work in the original scene in Play mode; this is manual user-reported validation. The Unity EditMode suite subsequently passed 78/78 on October 31, 2026 (0 failed, 0 skipped); this is not device QA.

**Next task:** The live completion progress bar uses a sprite-backed fill with a slower animation; the full Unity EditMode suite passed 78/78. The app opens its existing main menu on launch and loads the saved level when Continue Game is selected. A fresh Android Development APK built successfully on November 1, 2026 at `%TEMP%\SnakeEscape_Latest_Development.apk`. The user confirmed the publisher/display name `Snake Puzzle` and package ID `com.lynos.snake.arrow.puzzle.away.jam`; APK metadata and v2 signature were verified. No phone was connected, so startup-menu and device QA remain pending. The build emitted Unity's Android warning that Active Input Handling is set to Both; the original scene combines legacy `Input` calls with `InputSystemUIInputModule`. Verify this setup on-device and resolve it before release. Continue the remaining Phase 7 accessibility checks (touch-target usability, contrast, restrained feedback, and representative screen-size coverage) alongside Phase 8 Android QA. Do not treat this development APK as a release candidate: signing, store configuration, and device validation remain incomplete.

**Deferred:** The three `Assets/Resources/Levels/` samples and their separate loader are modular prototype work. Do not build or integrate that pipeline unless a specific product need is identified and discussed with the user.

Preserve all existing worktree changes.

**Final priority:** Preserve the existing product → characterize behavior → fix demonstrated issues in existing systems → verify each change → improve campaign quality where needed → polish → QA/release → optional monetization. Modularize or add new systems only when they solve an established need.
