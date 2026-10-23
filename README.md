# 🐍 Snake Escape

A 2D mobile logic puzzle game built with Unity. Players remove curved snakes from a crowded grid board by tapping them in the correct topological order.

## Project Overview

- **Engine**: Unity 6 / LTS (URP 2D)
- **Language**: C# (.NET Standard 2.1)
- **Platform**: Android (Primary), iOS (Secondary)
- **Architecture**: 4-Layer Clean Architecture (Domain, Application, Presentation, Data)
- **Genre**: Casual Logic / Escape Puzzle

## Current Status

- **Milestone 1 (Project Foundation & Directory Structure)**: Complete
- **Milestone 2 (Pure Puzzle Engine)**: Complete
  - Pure domain models (`GridCoord`, `SnakeDefinition`, `LevelDefinition`, `PuzzleState`, `OccupancyMap`)
  - Rule engine (`PuzzleRules`) with escape route projection, occupancy collision checks, win validation
  - Deterministic BFS puzzle solver with memoization (`PuzzleSolver`)
  - Application orchestration (`GameSession`, `MoveService`, `UndoService`, `LevelService`, `ProgressionService`)
  - Automated EditMode unit test suite for Domain & Application logic
- **Next**: Milestone 3 (Clean Presentation layer & Board/Snake views)

## Setup Instructions

1. Open Unity Hub.
2. Add this project folder (`snake chain game`).
3. Open with Unity 6 (6000.x) or supported LTS.
4. Open the main scene: `Assets/snake chain game.unity`.

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
