# Configuration

Every setting lives in `BepInEx\config\com.ivmakk.nomaddrive.restorationtoolstweaks.cfg`, written the first time you run the game with the mod installed. Edit it with any text editor while the game is closed. Changes apply at the next game start. A value out of its range applies as the nearest limit.

## Brush

| Setting | Default | Values | What it does |
|---|---|---|---|
| `StepSize` | `0.25` | `0.05` to `1` | How much one wheel notch changes the brush size of the Polisher, Grinder, and Cleaning Sponge. Does not change the Spray Paint steps. |
| `MaxScale` | `5` | `1` to `10` | The largest brush size, as a multiple of the game's size. Also sets how far the Spray Paint goes above its own largest size. A brush radius never goes above 2 m, the game's limit. |
| `DrainMode` | `Work` | `Work`, `Area`, `Time` | `Work`: a tool drains only for surface it changes, not for surface that is already done or for a tool in the air. `Area`: a bigger brush drains faster, by its area. `Time`: the drain each second stays as in the game at every brush size. In co-op, only the host's setting counts. |
| `DrainRate` | `0.5` | `0` to `2` | Multiplies the drain of the battery, the Cleaning Sponge, and the Spray Paint in every mode. `1` is the game's drain, `0.5` is half, `0` is no drain. In co-op, only the host's setting counts. |
| `ShowPreview` | `true` | `true`, `false` | Shows a ring on the surface at the brush size while a Polisher, Grinder, or Cleaning Sponge is snapped. |

## General

| Setting | Default | Values | What it does |
|---|---|---|---|
| `InvertWheel` | `false` | `true`, `false` | Turn the wheel down, not up, to make the brush bigger. On a gamepad, it swaps LB and RB. |
| `Verbose` | `false` | `true`, `false` | Writes extra lines to `BepInEx\LogOutput.log` for troubleshooting. Keep it off in normal play. |
