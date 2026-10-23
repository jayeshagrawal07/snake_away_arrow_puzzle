# Game Design Document — Snake Escape

## 1. Overview
Snake Escape is a mobile logic puzzle in which players untangle and remove snakes from a grid board. Each snake occupies a fixed set of connected grid cells, and its head points toward a designated cardinal exit direction. When tapped, if its forward path off the board is unobstructed by any other snake, it slithers away and leaves the board.

## 2. Core Mechanics
- **Grid Coordinates**: Discrete columns and rows (e.g. 8x8 standard board).
- **Snake Path**: Ordered list of adjacent cells `[Head, Body1, Body2, ... Tail]`.
- **Exit Direction**: A cardinal unit vector (`Up`, `Down`, `Left`, `Right`).
- **Escape Route**: All cells in front of the head extending straight along the exit direction until off-board.
- **Collision Check**: If any cell on the escape route contains another active snake, the move is blocked.
- **Victory Condition**: All snakes on the board have escaped.

## 3. Controls & UX Feedback
- **Tap Snake**: Requests an escape.
- **Valid Move**: Snake begins slithering along its escape trajectory.
- **Blocked Move**: Subtle visual feedback (gentle shake, indicator of blocking snake). No destructive penalties in standard puzzle mode.
- **Undo**: Restores the last escaped snake back to the board.
- **Restart**: Restores the initial puzzle layout.

