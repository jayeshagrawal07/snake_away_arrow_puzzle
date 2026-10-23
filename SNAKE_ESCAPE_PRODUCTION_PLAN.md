### Snake Escape

Production-Level Game Development Plan Master Specification for Claude Code / Google Antigravity

Project type: 2D mobile puzzle game Engine: Unity 6 **LTS** or another currently supported Unity **LTS** release Language: C# Primary platform: Android Secondary platform: iOS Orientation: Portrait Art direction: Minimal flat-color, clean vector-inspired visuals Genre: Casual logic / escape puzzle Development approach: Test-driven, data-driven, modular, production-oriented

## Instructions for the AI Coding Agent

You are the lead Unity gameplay engineer and technical architect responsible for implementing Snake Escape.

Your job is to build a complete, maintainable, tested, production-quality game, not merely a prototype or a collection of scripts.

Use this document as the primary project specification.

Mandatory development rules Inspect the existing repository before changing files. If the repository is empty, establish the Unity project structure and document the required Unity editor setup. Never overwrite existing user work without understanding its purpose. Implement the game incrementally in small, testable milestones. Before implementing each milestone, provide a concise plan and identify the files affected. Keep game rules independent of rendering, animation, UI, and platform-specific functionality. Use data-driven level definitions rather than hardcoding the campaign into gameplay scripts. Create automated tests for gameplay rules, puzzle validation, save data, and important edge cases. Do not claim that code compiles or tests pass unless those checks were actually performed. If Unity Editor execution is unavailable, clearly distinguish static inspection from actual Unity compilation and runtime testing. Never introduce paid assets, third-party dependencies, advertising SDKs, or analytics services without documenting and approving the dependency. Use version control and make changes in reviewable increments. Maintain documentation as the implementation evolves. Resolve compiler errors and test failures before starting unrelated features. Do not generate the entire game in one uncontrolled step. Follow the milestones in this specification. Required agent deliverables

The finished project must include:

A playable Unity game. A reusable puzzle engine. A deterministic snake movement and collision system. A level editor or level-authoring workflow. An automated puzzle solver and level validator. A complete initial level campaign. Main menu, level selection, gameplay **HUD**, pause, settings, and completion screens. Persistent player progress. Audio and visual feedback. Automated tests and a documented manual QA process. Android build configuration and an iOS preparation checklist. Build, testing, architecture, and release documentation.

If a requirement cannot be completed in the current environment, document the limitation and provide a concrete next action. Do not silently omit it.

## Product Vision

Snake Escape is a mobile logic puzzle in which players remove curved snakes from a crowded board by tapping them in the correct order.

Each snake occupies a fixed path. Its head has a designated exit route. When the player taps the snake, the game checks whether the escape route is clear.

If the route is blocked by another snake, the move is rejected and the game communicates what blocked it.

If the route is clear, the snake slithers away, with the head moving first and the body following along its established path. The board updates, and the player can choose another snake.

The level is complete when all snakes have escaped.

Core design principles Easy to understand. Satisfying to interact with. Challenging through logic rather than reflexes. Visually minimal and readable. Deterministic and fair. Fast to load and responsive to touch. Expandable through new levels and mechanics. Fully playable without an internet connection. Unique identity

The snake theme must be more than an arrow reskin.

Develop a distinctive visual and animation language around:

Smooth curved bodies. Expressive but minimal heads. Head-first movement. Subtle slithering animation. Clear body-following behavior. Calm visual feedback. A polished, cohesive color palette.

Do not copy another game's branding, artwork, level layouts, interface assets, or proprietary content.

## Core Gameplay Rules

These rules define the initial release and must remain consistent throughout the project.

3.1 Snake representation

Each snake has:

A unique identifier. An ordered body path. A head position and orientation. A designated forward escape route. A visual style and color. A collision footprint. A gameplay state.

The logical path is the authoritative source of truth. Rendered curves are derived from that path.

The snake's body cannot bend into a new shape during normal gameplay.

3.2 Player interaction

The player taps a snake.

The game checks:

Whether the selected snake exists and is still active. Whether the snake is already moving. Whether the game is accepting input. Whether its exit route is clear. Whether the move conflicts with another active or reserved movement.

If the move is invalid, show brief feedback without corrupting the board state.

If the move is valid, reserve the movement, start the escape animation, and update the board when the move completes.

3.3 Collision rules

The first release uses deterministic, grid-based escape-route validation.

A snake cannot escape if another active snake occupies any grid cell along its required route.

The selected snake must not block itself.

Cells occupied by other snakes must be checked against a well-defined route representation.

The system must account for:

Cells immediately outside the board. Multiple snakes sharing a route corridor. Snakes with long bodies. Snakes near board edges. Movement reservations. Simultaneous input during animation. Escaped snakes. Invalid or overlapping level data.

Do not use Unity physics as the authoritative puzzle solver. Physics and colliders are for visual interaction; logical occupancy determines puzzle legality.

Important geometry rule

The rendered snake body is curved, but the initial puzzle logic is grid-based.

The level format must define how curved paths map to grid cells. The rendering system must not visually imply a collision that the logical system ignores.

If a future mechanic allows movement through continuous curved spaces, introduce a separate swept-body collision model and validate it against the existing rules before enabling it.

3.4 Escape behavior

The head travels forward toward the designated exit.

The body follows the established path and gradually disappears beyond the board boundary.

The animation must not change the snake's logical route or create an invalid board state.

Only one escape animation should run at a time in the initial release. This reduces ambiguity and makes state transitions easier to test.

3.5 Level completion

A level is complete when every snake has reached the escaped state.

Completion should:

Disable additional gameplay input. Trigger a short celebration. Save progress. Display the completion screen. Show the next-level action.

The completion screen must not appear before the last escape animation finishes.

## Technical Architecture

Use a modular architecture with clear ownership.

4.1 Architecture layers Domain layer

Pure C# gameplay logic, independent of Unity scene objects.

Responsibilities:

Snake definitions. Grid coordinates. Occupancy representation. Escape validation. Puzzle state. Move execution rules. Win detection. Puzzle solving. Undo state snapshots. Application layer

Coordinates gameplay operations.

Responsibilities:

Start and restart levels. Receive player commands. Validate and execute moves. Coordinate animation completion. Manage game state transitions. Trigger save and progression updates. Presentation layer

Unity-specific rendering and interaction.

Responsibilities:

Draw the board. Render snake paths. Animate escapes. Process touch and pointer input. Display **HUD** and menus. Play audio and visual feedback. Data layer

Responsibilities:

Level definitions. Campaign configuration. Player save data. Settings. Level completion records. Content versioning. Editor and tooling layer

Responsibilities:

Author levels. Preview snakes and routes. Validate board geometry. Run the puzzle solver. Preview valid move sequences. Batch-validate the campaign. 4.2 Suggested project structure

Organize the project by feature and responsibility.

Assets/
    Game/
    Scenes/
    Bootstrap.unity
    MainMenu.unity
    Gameplay.unity

    Scripts/
    Domain/
    GridCoord.cs
    SnakeDefinition.cs
    PuzzleState.cs
    PuzzleRules.cs
    OccupancyMap.cs
    MoveResult.cs
    PuzzleSolver.cs

    Application/
    GameSession.cs
    LevelService.cs
    MoveService.cs
    UndoService.cs
    ProgressionService.cs

    Presentation/
    Board/
    BoardView.cs
    GridRenderer.cs
    BoardLayout.cs

    Snake/
    SnakeView.cs
    SnakeRenderer.cs
    SnakeAnimator.cs
    SnakeInputHandler.cs

    UI/
    MainMenuUI.cs
    GameplayHUD.cs
    LevelSelectUI.cs
    PauseMenuUI.cs
    SettingsUI.cs
    LevelCompleteUI.cs

    Audio/
    AudioService.cs
    HapticsService.cs

    Infrastructure/
    SaveService.cs
    SettingsService.cs
    LoggingService.cs

    Data/
    Campaigns/
    Levels/
    Themes/

    Prefabs/
    Snake/
    UI/

    Art/
    Sprites/
    Materials/
    Fonts/

    Audio/
    Music/
    **SFX**/

    Tests/
    EditMode/
    PlayMode/

    Editor/
    LevelEditor/
    Validation/

Documentation/
    GAME_DESIGN.md
    **ARCHITECTURE**.md
    LEVEL_AUTHORING.md
    **TESTING**.md
    BUILD_AND_RELEASE.md
    **CHANGELOG**.md

**README**.md

The agent may adjust names and grouping when necessary, but must preserve the separation between domain logic, application orchestration, presentation, and data.

## Core System Requirements

5.1 Board system

Implement a board that supports configurable dimensions.

Initial target:

Standard board: 8 × 8. Larger boards: configurable for later levels. Coordinate origin and orientation defined consistently. Automatic layout for different screen sizes. Safe-area support. Clear board boundaries. Optional subtle cell grid.

The board must adapt to portrait mobile aspect ratios without distorting snake paths.

Board coordinates must be independent of world-space coordinates.

Do not assume that one grid cell equals one Unity world unit throughout the entire codebase.

5.2 Snake path system

Support ordered path points representing the body and escape route.

Requirements:

Validate path continuity. Validate allowed turns. Validate board boundaries. Validate self-overlap. Validate overlaps with other snakes. Determine head orientation. Determine the exit route. Convert grid paths into smooth visual curves. Preserve logical occupancy during animation.

Use a single canonical path representation. Do not maintain unrelated copies of the same path in multiple systems.

5.3 Occupancy system

Maintain a reliable mapping between board cells and active snake IDs.

Requirements:

Rebuild occupancy deterministically from puzzle state. Update occupancy after successful moves. Exclude escaped snakes. Detect invalid overlaps during level loading. Handle movement reservations. Prevent two snakes from claiming the same logical cell. Support fast escape checks.

The board state must be updated transactionally. A rejected move must not partially modify occupancy.

5.4 Move service

All player moves must pass through one application-level entry point.

The move service must:

Validate the current session state. Resolve the selected snake. Evaluate its escape route. Return a structured move result. Reject invalid moves without modifying the puzzle. Reserve the move when accepted. Notify the presentation layer to animate. Commit the completed move. Update progression and save state. Evaluate level completion.

Avoid duplicating validation logic in click handlers, animation scripts, and UI controllers.

5.5 Animation system

Implement a presentation system that follows the authoritative path.

Requirements:

Smooth path rendering. Rounded caps and corners. Stable head orientation. Head-first escape. Body-following motion. Reliable completion callbacks. Cancellation and reset behavior. Correct handling of scene transitions. No logical duplication during animation. No input race conditions.

Animations must be cancellable or safely reset when the player restarts a level or leaves gameplay.

Do not let animation timing determine whether a move is logically valid.

5.6 Game state management

Define explicit states, including:

Initializing. Ready. Moving. Paused. Level complete. Loading another level. Error or invalid level.

Define allowed transitions and test them.

Input must be disabled when the game is paused, loading, or transitioning.

## Level Editor and Puzzle Solver

These are high-priority production tools.

Build them early, before producing a large campaign.

6.1 Level editor

Create a Unity Editor tool that allows a designer to:

Set board dimensions. Draw snake paths using grid coordinates. Set the head position. Set the exit route. Select a snake's visual style. Preview the rendered board. Detect overlaps and invalid paths. Save level assets. Duplicate levels. Run validation. Run the solver. Preview a solution sequence. Export a validation report.

The editor must display useful errors and warnings rather than merely returning a pass/fail result.

6.2 Automated puzzle solver

Implement a deterministic search over puzzle states.

For the initial game, a state can be represented by the set of snakes that remain on the board, provided snake paths remain fixed and removing a snake does not change the geometry of other snakes.

For each state:

Identify all snakes that can legally escape. Generate successor states by removing one eligible snake. Search until all snakes have escaped or the search space is exhausted. Cache previously visited states. Return a valid solution sequence when one exists.

Use depth-first search or breadth-first search initially, with memoization.

If levels become large, evaluate stronger search strategies and state compression.

The solver must distinguish:

Solvable. Unsolvable. Invalid level data. Search limit reached.

A search limit must never be reported as proof that a level is unsolvable.

6.3 Level validation

Every published level must pass:

Coordinate validation. Board boundary validation. Path continuity validation. Self-overlap validation. Snake-to-snake overlap validation. Escape-route validation. Solver validation. Solution replay validation. Visual preview inspection. Manual playtesting.

Store validation results in a report that identifies the level and the exact problem.

## Level Design and Campaign Production

7.1 Initial campaign

Target 30 carefully tested levels for the first public release, with the architecture supporting additional content later.

Suggested distribution:

Levels 1–5: basic interaction and simple exits. Levels 6–10: one blocker at a time. Levels 11–15: multiple dependency relationships. Levels 16–20: longer snakes and denser layouts. Levels 21–25: more complex move sequences. Levels 26–30: advanced combinations of previously introduced ideas.

These are design targets, not automatic difficulty guarantees. Adjust them after playtesting.

7.2 Difficulty design

Measure difficulty using several signals:

Number of snakes. Number of blocked snakes at the start. Length of the shortest solution. Number of valid solution sequences. Number of dependencies between snakes. Board density. Number of plausible but incorrect moves. Completion time during playtesting.

Do not rely on snake count alone to estimate difficulty.

7.3 Level metadata

Each level should include:

Stable level ID. Campaign and chapter. Board dimensions. Snake definitions. Difficulty category. Intended learning objective. Expected solution sequence, when applicable. Author and revision information. Validation status. Content version. 7.4 Level acceptance

Before publication, each level must:

Have valid geometry. Have at least one verified solution. Be replayable from a fresh state. Produce consistent results across repeated runs. Be visually readable on supported devices. Have no unintended alternative interpretations of the rules. Pass manual testing. ## UI and UX Specification 8.1 Main menu

Include:

Game logo. Play button. Level selection. Settings. Optional cosmetics or theme selection. Privacy and legal links where applicable.

The primary action should be obvious and accessible.

8.2 Gameplay **HUD**

Include:

Level number. Level progress. Restart. Undo, if enabled. Hint, if enabled. Pause or settings access.

Avoid cluttering the board with unnecessary buttons.

8.3 Blocked move feedback

When a player taps a blocked snake:

Give a subtle visual response. Identify the blocking snake or blocking route when practical. Optionally play a quiet sound or haptic response. Do not punish the player in standard mode. Do not change the logical board state.

Avoid excessive shaking, aggressive red flashes, or repeated disruptive sounds.

8.4 Undo

Undo should restore the last committed successful move.

It must restore:

The removed snake. The prior occupancy state. Progress counters. Any relevant move history. A consistent visual state.

Do not allow undo during an active animation unless the animation can be safely cancelled and the entire state restored.

8.5 Restart

Restart must restore the exact initial state of the current level.

It must reset:

Active snakes. Occupancy. Escape progress. Undo history. Game state. Animation state. Temporary visual effects. 8.6 Completion screen

Include:

Completion message. Level identifier. Progress feedback. Next Level button. Replay button. Optional star rating.

Avoid delaying the next action with long mandatory animations.

8.7 Accessibility

Provide:

Readable text and scalable UI. Adequate contrast. Large touch targets. Reduced-motion support where practical. Sound and haptic toggles. Distinguishable snake heads and paths. Feedback that does not depend on color alone. ## Art and Animation Production 9.1 Visual direction

Use a minimal flat-color style.

Recommended palette:

Background: warm cream. Grid: subtle gray. Text: dark navy. Snakes: green, blue, coral, yellow, purple, and teal. Feedback: restrained accent colors.

Avoid noisy textures and unnecessary visual effects.

9.2 Snake design

Each snake needs:

Smooth body geometry. Rounded ends. A visually distinct head. A clear forward direction. Consistent body width. Optional subtle eyes or facial details. Reliable visual separation from neighboring snakes.

Do not make head orientation ambiguous.

9.3 Animation list

Create reusable animation behaviors for:

Snake selection. Blocked escape. Successful escape. Level completion. Button interaction. Menu transitions. Undo and restart transitions.

Keep animation durations short and consistent.

9.4 Asset pipeline

Document:

Source asset locations. Import settings. Sprite filtering and compression. Material conventions. Naming conventions. License and attribution information.

Prefer original assets or assets with clearly documented usage rights.

## Audio and Haptics

Create a restrained audio system.

Suggested sounds:

Snake tap or selection. Blocked move. Successful escape. Level completion. Button interaction. Optional background music.

Requirements:

Centralized volume controls. Independent music and effects settings. No overlapping sound spam. Safe behavior when audio is muted. Haptic feedback only where supported. A settings option to disable haptics.

All audio must be optional and must not interfere with puzzle readability.

## Save Data and Progression

Implement local persistence for the first release.

Persist:

Unlocked levels. Completed levels. Best results. Settings. Cosmetic selections, if implemented. Save schema version.

Requirements:

Safe handling of missing save data. Safe handling of malformed data. Versioned save migrations. Atomic or recoverable writes where practical. No progression loss during normal app interruptions. No dependence on a network connection.

Do not store sensitive personal information.

If cloud saves are introduced later, design synchronization and conflict resolution as a separate feature.

## Monetization and Analytics

Monetization is optional and should not delay proving the core gameplay.

Potential options:

Rewarded ads for optional hints. An optional remove-ads purchase. Cosmetic themes. Additional level packs.

Do not implement monetization until the core game, onboarding, and progression are stable.

If advertising or analytics SDKs are added:

Document each dependency. Verify current platform requirements. Follow applicable privacy laws and store policies. Provide required consent and disclosures. Avoid collecting unnecessary personal data. Test purchases and ad behavior in test environments. Keep the core puzzle playable offline.

Analytics may measure:

Level starts. Level completions. Completion time. Hint usage. Restart frequency. Difficulty-related abandonment. Crash and performance information.

Use these measurements to improve level design, not to create manipulative friction.

## Testing Strategy

Testing is mandatory throughout development.

13.1 Edit Mode tests

Test pure gameplay logic:

Grid coordinate conversions. Occupancy creation and updates. Valid escape routes. Blocked escape routes. Self-overlap detection. Invalid paths. Invalid board dimensions. Win conditions. Undo state restoration. Solver correctness. 13.2 Play Mode tests

Test Unity integration:

Snake rendering. Head interaction. Escape animation. Input blocking during movement. Restart during movement. Scene transitions. Completion UI. Save restoration. Resolution changes. 13.3 Automated level tests

Run the validator against the entire campaign.

Fail the validation pipeline if a published level contains invalid geometry or is proven unsolvable.

Store solver statistics to help diagnose unexpectedly difficult or expensive levels.

13.4 Manual QA

Test on actual target devices.

Include:

Small and large screens. Different aspect ratios. Slow devices. App backgrounding and resuming. Audio disabled. Rapid repeated taps. Restart during transitions. Repeated level completion. Save corruption recovery. Low-memory situations. 13.5 Release-blocking bugs

Do not ship with:

Unsolvable published levels. Softlocks. Lost progression under normal conditions. Incorrect collision results. Crashes during common gameplay. UI controls that cannot be used on supported devices. Broken purchase behavior, if purchases are enabled. ## Performance and Reliability

Set measurable targets and validate them on representative hardware.

Initial targets:

Responsive touch input. Smooth gameplay on supported devices. Minimal allocations during repeated move validation. No per-frame reconstruction of the entire board. Efficient snake rendering. Bounded memory usage. Fast loading of ordinary levels. No unnecessary network calls.

Prefer simple, efficient rendering for the initial campaign.

Profile before introducing object pooling or complicated optimization.

The board and solver must remain correct regardless of frame rate or animation timing.

## Platform and Build Requirements

Android

Prepare:

Android build configuration. Portrait orientation. Application identifier. App icon and splash assets. Appropriate target **API** configuration. Signing configuration. Release build. Device testing. Store listing assets. Privacy disclosures.

Keep signing credentials outside version control.

iOS

Prepare:

iOS build configuration. Bundle identifier. Required Apple developer configuration. Signing and provisioning. App icon and launch assets. Device testing. Privacy disclosures. Store listing assets.

Validate current Unity and store requirements at release time.

Do not claim an iOS build has been tested unless it has actually been built and tested.

## Development Milestones

The agent must implement these milestones in order.

Milestone 1 — Repository audit and project foundation

Tasks:

Inspect repository and Unity version. Establish folder structure. Configure project settings. Set up version control guidance. Document dependencies. Create architecture documentation.

Acceptance criteria:

The project structure is documented. No unexplained dependencies exist. The required editor version is known. The initial scene can be opened in Unity. Milestone 2 — Pure puzzle engine

Tasks:

Implement grid coordinates. Define snake and level data. Implement occupancy. Implement route validation. Implement puzzle state transitions. Implement win detection.

Acceptance criteria:

Core rules can be tested without rendering. Invalid moves do not mutate state. Automated tests cover core edge cases. Milestone 3 — First playable level

Tasks:

Render a grid. Render two or more snakes. Add tap input. Implement escape validation. Animate successful escapes. Show level completion.

Acceptance criteria:

A valid sequence clears the board. Blocked moves work correctly. No invalid board states are produced. Milestone 4 — Reliable gameplay systems

Tasks:

Implement restart. Implement undo. Implement pause and resume. Prevent conflicting inputs. Handle scene transitions. Add state recovery.

Acceptance criteria:

State remains correct across all tested interactions. Tests cover interrupted and repeated actions. Milestone 5 — Level authoring and solver

Tasks:

Build the level editor. Add geometry validation. Implement the solver. Add solution preview. Batch-validate level assets.

Acceptance criteria:

Designers can create levels without editing gameplay code. Invalid levels are identified. Solver results are deterministic and verifiable. Milestone 6 — Campaign and progression

Tasks:

Produce the initial 30-level campaign. Implement level selection. Save completed and unlocked levels. Add difficulty progression. Conduct playtesting.

Acceptance criteria:

All shipped levels pass automated validation. The campaign is playable from beginning to end. Milestone 7 — UI, art, and audio polish

Tasks:

Finalize the visual style. Implement menus and **HUD**. Add animation polish. Add sound and haptics. Implement accessibility settings.

Acceptance criteria:

The game feels visually consistent. UI works across target aspect ratios. Feedback is understandable and unobtrusive. Milestone 8 — QA and optimization

Tasks:

Run Edit Mode and Play Mode tests. Profile representative devices. Test save recovery. Resolve release-blocking bugs. Verify platform requirements.

Acceptance criteria:

No known critical bugs remain. Performance meets the agreed targets. A release candidate is available. Milestone 9 — Release preparation

Tasks:

Prepare store assets. Configure release builds. Validate privacy disclosures. Conduct final regression testing. Document known issues. Create a rollback and hotfix plan.

Acceptance criteria:

Builds are reproducible. Store materials are ready. Release evidence and test results are documented. ## Required Documentation

Maintain these documents throughout the project.

**README**.md

Must include:

Game overview. Required Unity version. Setup instructions. How to open and run the project. How to run tests. Current development status. GAME_DESIGN.md

Must include:

Game rules. Controls. Level progression. Visual direction. Accessibility. Feature scope. **ARCHITECTURE**.md

Must include:

System boundaries. Data flow. State transitions. Dependencies. Major technical decisions. LEVEL_AUTHORING.md

Must include:

How to create a level. How to define snake paths. How to define exits. How to validate a level. How to run the solver. How to review a solution. **TESTING**.md

Must include:

Test setup. Test categories. How to execute tests. Required release checks. Known limitations. BUILD_AND_RELEASE.md

Must include:

Build configuration. Platform prerequisites. Signing guidance. Release steps. Store requirements. Rollback and hotfix procedures. ## Definition of Production Ready

The project is not production-ready merely because the game runs in the Unity Editor.

It is production-ready when:

Core gameplay is deterministic and tested. All published levels are valid and solvable. The level editor supports repeatable content production. The solver can verify the campaign. Save data is reliable and versioned. UI and animations handle state transitions correctly. The game works on supported devices. Performance has been profiled. Platform and privacy requirements have been checked. Release builds have been tested. Documentation is complete. Known issues and limitations are documented. ## Final Execution Instructions

Begin by auditing the repository and reporting:

Current project state. Existing systems that can be reused. Missing project prerequisites. Proposed implementation sequence. Risks and unresolved design decisions.

Then implement Milestone 1.

For every milestone:

Summarize the intended changes. Implement only the scope of that milestone. Run available validation and tests. Report the files changed. Report actual test results. Document remaining limitations. Wait for approval before making major architectural changes.

Do not skip ahead to monetization, cosmetics, or large-scale content production while core gameplay is unstable.

If a requested feature conflicts with the established rules, explain the conflict and propose a consistent solution before implementation.

Final objective: deliver a maintainable, polished Unity mobile puzzle game with reliable snake escape mechanics, a scalable level-production pipeline, verified campaign content, and a documented release process.

The priority order is:

Correct gameplay → reusable architecture → level editor and solver → campaign content → polished UX → QA → release.