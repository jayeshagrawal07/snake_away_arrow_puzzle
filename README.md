# 🐍 Snake Escape

A 2D mobile logic puzzle game built with Unity. Players remove curved snakes from a crowded grid board by tapping them in the correct topological order.

## Project Overview

- **Engine**: Unity 6 / LTS (URP 2D)
- **Language**: C# (.NET Standard 2.1)
- **Platform**: Android (Primary), iOS (Secondary)
- **Architecture**: Existing legacy game plus a separate 4-layer implementation under migration
- **Genre**: Casual Logic / Escape Puzzle

## Current Status

- The existing game scene is `Assets/snake chain game.unity` and remains the project starting point.
- The legacy gameplay implementation (`Assets/Legacy/GameManager.cs` and `Assets/Legacy/SnakeController.cs`) is still attached to that scene through their preserved Unity asset GUIDs.
- A newer domain/application/presentation implementation exists separately under `Assets/Game/`; it is not yet integrated into the existing scene.
- The modular prototype scene was removed because it was not part of the product or build. The editor menu can recreate it when needed.
- Current focus: preserve the existing scene and migrate or improve its gameplay incrementally, validating behavior as each part changes.

## Setup Instructions

1. Open Unity Hub.
2. Add this project folder (`snake chain game`).
3. Open with Unity 6 (6000.x) or supported LTS.
4. Open `Assets/snake chain game.unity` to continue from the existing game.
5. This scene is configured as the enabled build scene. `Tools > Snake Escape > Setup Gameplay Scene` creates a separate modular prototype scene and replaces the currently open scene in the Editor; use it only when intentionally testing that prototype.

## Running Tests

1. In Unity, go to **Window > General > Test Runner**.
2. Select the **EditMode** tab.
3. Run `SnakeEscape.Tests.EditMode` to verify:
   - `GridCoordTests`
   - `OccupancyMapTests`
   - `PuzzleRulesTests`
   - `PuzzleSolverTests`
   - `ApplicationTests`

## Documentation

- [Game Design](Documentation/GAME_DESIGN.md)
- [Architecture](Documentation/ARCHITECTURE.md)
- [Level Authoring](Documentation/LEVEL_AUTHORING.md)
- [Testing Strategy](Documentation/TESTING.md)
- [Build and Release](Documentation/BUILD_AND_RELEASE.md)
- [Changelog](Documentation/CHANGELOG.md)
