# Changelog

All notable changes to this mod are documented in this file. The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.1] - 2026-11-02

### Removed

- Support for the game before its update of 1 November 2026. On an older game the drain settings turn off.

### Fixed

- The mod works again after the game update of 1 November 2026. Before this fix it turned itself off, so the brush size, the ring, the extra Spray Paint sizes, and the drain settings did nothing.
- In work drain, the Polisher, Grinder, and Spray Paint now drain on a loose part that is not on a vehicle. Before, they did not drain there at all.
- When a later game update breaks only the drain settings, the brush size, the ring, and the extra Spray Paint sizes keep working, and the tools drain as in the game.

## [1.0.0] - 2026-10-29

### Added

- Mouse wheel (LB and RB on a gamepad) brush size for the Polisher, Grinder, and Cleaning Sponge while the tool is snapped, from the game's size up to 5x (up to 10x in the settings).
- Ring on the surface that shows the brush size of the Polisher, Grinder, and Cleaning Sponge.
- Extra Spray Paint brush sizes above the game's largest one.
- Work drain: the Polisher and Grinder battery and the Spray Paint drain only for surface the tool changes, at half the game's cost by default.
- Settings for the wheel step, the largest size, the drain mode, the drain rate, the ring, and the wheel direction.

[Unreleased]: https://github.com/ivmakk-nd/nd-restoration-tools-tweaks/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/ivmakk-nd/nd-restoration-tools-tweaks/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/ivmakk-nd/nd-restoration-tools-tweaks/releases/tag/v1.0.0
