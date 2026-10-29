# Restoration Tools Tweaks

Make the brush of the Polisher, Grinder, Cleaning Sponge, and Spray Paint bigger with the mouse wheel.

The restoration tools work on a small area at a time, so a whole vehicle takes many passes. This mod lets you make the brush up to 5 times wider, shows its size on the surface, and makes the tools pay only for the surface they actually change.

## Features

- **Brush size on the mouse wheel**: snap the Polisher, Grinder, or Cleaning Sponge to a surface and turn the wheel (LB and RB on a gamepad) to resize the brush, up to 5x. With no tool snapped, the wheel changes the hotbar as usual. The size resets when you put the tool away.
- **Brush preview**: while the tool is snapped, a ring on the surface shows the brush size, in the same style as the Spray Paint ring.
- **Bigger Spray Paint brush**: the Spray Paint keeps the game's Brush Size control and gets extra sizes above its largest one, up to 4x with the default settings (8x with a max size of 10).
- **Pay for the work, not the time**: the Polisher, Grinder, and Spray Paint drain only for surface they change, at half the game's cost by default. Finished surface and a tool in the air cost nothing. The Cleaning Sponge drains by brush area instead.
- **Limits**: a brush radius never goes above 2 m, the game's own limit. With a max size above 5, some tools stop before it: the Polisher at about 8.9x, the Cleaning Sponge at about 6.7x.

## Multiplayer

Not tested in co-op. The bigger brush should work for each player who installs the mod. The drain rules and the extra Spray Paint sizes need the mod on the host.

## Save safety

The mod stores nothing in your save, so you can remove it at any time.

## Requirements

- Nomad Drive 1.0 or later.
- [Nomad Drive BepInEx Pack](https://www.nexusmods.com/nomaddrive/mods/7).

## Installation

1. Install the [BepInEx pack](https://www.nexusmods.com/nomaddrive/mods/7).
2. Extract this mod's zip into the game folder (the folder with the game .exe). The DLL lands in BepInEx\plugins. Full path example:
   `C:\Program Files (x86)\Steam\steamapps\common\Nomad Drive\BepInEx\plugins\RestorationToolsTweaks.dll`

## Uninstall

Delete `RestorationToolsTweaks.dll` from the `BepInEx\plugins` folder.

## Configuration

To change mod settings, edit `BepInEx\config\com.ivmakk.nomaddrive.restorationtoolstweaks.cfg` while the game is closed (see [CONFIG.md](CONFIG.md) for every setting). Changes apply at the next game start.

## Build

The build needs the game and the BepInEx pack installed, and the .NET SDK 10.

```
dotnet build src/RestorationToolsTweaks.csproj -c Release
dotnet test tests/RestorationToolsTweaks.Tests
```

The build looks for the game in `C:\Program Files (x86)\Steam\steamapps\common\Nomad Drive`. For another folder, pass `-p:GameDir=<game folder>` or set a `GameDir` environment variable. Add `-p:Package=true` to make the install zip in `dist/`.

## License

Licensed under the GNU General Public License v3.0. Copyright (C) 2026 ivmakk.

Do not reupload this mod or a changed copy without credit and a link to this repository.
