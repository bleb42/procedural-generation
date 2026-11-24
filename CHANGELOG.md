# Changelog

## [0.1.0]
- Initial extraction from a game project.
- Grid-free layout: rooms are measured (floor rectangles and doors), not snapped to a cell grid.
- Frame-budgeted coroutine generation, plus an immediate mode for the editor and tests.
- Optional minimap (mini and full views).

## [Unreleased]
- Wall visibility rewritten: a wall piece is hidden only when the visible pieces of the neighbouring room fully cover it. Fixes holes next to rooms of different sizes and closed doors hiding neighbouring walls.
