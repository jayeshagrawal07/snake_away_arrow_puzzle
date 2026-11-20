# Testing Strategy — Snake Escape

## 1. EditMode Unit Tests
Located under `Assets/Tests/EditMode/`.
Cover pure domain rules and puzzle solving without launching a scene:
- `GridCoordTests`: Vector math, equality, bounds, cardinal direction checks.
- `OccupancyMapTests`: Cell reservations, occupant retrieval, vacate transactions.
- `PuzzleRulesTests`: Escape route calculation, blocked moves, win detection, level structural validation.
- `PuzzleSolverTests`: BFS state solver, dead-end detection, replay validation.
- `ApplicationTests`: `MoveService`, `UndoService`, and `GameSession` state lifecycle.
- `PlayerSaveServiceTests`: defaults, schema-v1/schema-v2 migration to schema v3, invalid-save/checksum fallback, clamping, explicit reset, and progress/resource/settings/eraser-use round-trip across service instances.
- `LevelServiceTests`: campaign sorting and ID lookup.
- `LevelServiceResourceTests`: loading an authored asset from the canonical Resources path.
- `LegacyLevelGeneratorTests`: verifies seeded generation does not consume the game's global Unity random state and audits repeatability, path geometry, overlap, and carve-order solvability at levels 2, 5, 6, 7, and 13.

## 2. PlayMode Tests (Milestone 8)
Located under `Assets/Tests/PlayMode/`.
Will cover:
- Snake rendering and animation completion triggers.
- Input blocking during active movement.
- Scene resets and camera framing across aspect ratios.
- Original legacy UI safe-area placement and portrait/landscape resizing require visual validation in Unity Game view and on representative devices; EditMode tests do not verify rendered layout.

## 3. Automated Level Batch Validation
`CampaignValidationTests.AuthoredCampaign_AllResourcesHaveValidGeometry_AndAreSolvable` loads the authored `.asset` levels from `Assets/Resources/Levels/` and checks that every snake has serialized body cells, each level passes `PuzzleRules.ValidateLevel`, and `PuzzleSolver.Solve` finds a complete solution. The three current sample assets passed as part of the 72/72 EditMode run. The editor generator replaces those three named sample assets when run again.

`LevelServiceResourceTests` separately creates a temporary level and verifies `LevelService` loads it from the canonical Resources path. A built-player resource-loading check remains part of release validation.

The save-service regression tests verify that a Level 14 snapshot and its resources survive service recreation, and that a versioned save with a bad checksum safely falls back to legacy progress. Tests use an in-memory store and do not read or modify the developer's actual saved progress.

The existing legacy game generates short sound-effect clips at runtime for buttons, blocked moves, snake escapes, and level completion. Blocked moves and successful escapes use distinct synthesized hisses shaped from filtered broadband noise for a breathier, raspier sound without a musical pitch; UI buttons and level completion retain their tonal cues. These procedural effects are an approximation, have not been auditioned in Unity or on a device, and may need a licensed recording for greater realism. The sound setting persists independently of haptics. Background music is not implemented.

The legacy eraser uses the existing coin wallet and simulated rewarded-ad overlay; no real ad network is integrated. Its one-free-use marker is saved for each campaign level or challenge run. The existing ruler is the only grid control; after a guide is activated it toggles grid visibility, and when no free guides remain the player can get one for 100 coins or via the simulated ad. The user confirmed the updated Hint popup, HUD count-chip alignment, and current gameplay/UI changes work in the original scene in Play mode. The full Unity EditMode suite passed **78/78** on October 31, 2026 (0 failed, 0 skipped); the latest run took 68.6 seconds. These results do not replace device testing.

The legacy generator regression tests verify the seeded-random scope used by the existing `GameManager` leaves the caller's Unity random sequence unchanged, and that representative progression-boundary levels generate repeatable, valid, non-overlapping paths solvable under the existing rules. The full Unity EditMode suite passed 78/78 on October 31, 2026, after the latest UI/gameplay changes. These tests do not replace a scene playtest or assess perceived difficulty.

## Android development-build smoke test
On November 1, 2026, an Android Development APK was built from the enabled original scene using Unity 6000.6.1f1. It includes the startup flow that opens the existing Snake Puzzle main menu on launch and loads the saved level for Continue Game. `aapt` confirmed package ID `com.lynos.snake.arrow.puzzle.away.jam`, display label `Snake Puzzle`, minimum SDK 26, and target SDK 36; `apksigner` verified the APK with signature scheme v2. The APK is at `C:\Users\Jayesh\AppData\Local\Temp\SnakeEscape_Latest_Development.apk` (46,299,370 bytes). This is a local development build, not a release-signed candidate.

No Android device was connected, so the APK has not been installed or run on a phone. Unity emitted a warning that Active Input Handling is set to Both, which is unsupported on Android and may cause input/performance issues. This setting currently serves the legacy `Input` calls in `GameManager` and the original scene's `InputSystemUIInputModule`; verify on-device and resolve the input configuration before release.
