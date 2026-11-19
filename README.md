<div align="center">

# Scarlet-Violet-Recomp

**Native PC builds of *Pokémon Scarlet* and *Pokémon Violet* (4.0.0), made by static recompilation from your own
copies of the games, with Tandem Launcher: game setup, mods, textures and settings in one place.**

[Report an issue](https://github.com/rikvandam2008-wq/Scarlet-Violet-Recomp/issues)

</div>

---

> [!IMPORTANT]
> **You need your own copies of the games.** This repository holds no game code, assets, fonts or keys, and nothing
> made from them; nothing is ever downloaded for you. You set the games up from your own dumps (base game, 4.0.0 update,
> DLC) and the `prod.keys` from your own Switch, and the code is compiled on your PC.

> [!WARNING]
> **Work in progress.** The engine underneath is an early port: expect graphical glitches, frame drops and crashes.
> Please report them with the game's `logs\play.log`.

## What it is

The games' ARM64 code is translated ahead of time into C and compiled for x86-64 on your PC. The Switch's operating
system services, file system, audio and NVN graphics are reimplemented on top of Vulkan. **No emulator runs while you
play**: the game's code runs natively on your CPU and GPU.

Scarlet support is new in this project: Scarlet 4.0.0 turned out to be Violet 4.0.0's build except for its version flag,
so every patch and address the Violet port uses applies to it unchanged (`tools/sv_port.py` checks this).

## Quick start (Windows)

1. Get this repository (green **Code** button > Download ZIP, or `git clone`).
2. Double-click **`Build Tandem.bat`**. It installs the build tools if they're missing (MSYS2 with clang and make,
   Python 3.12, the .NET 8 SDK), builds the engine and the launcher into `build\Tandem`, and opens the launcher.
3. In Tandem, pick **Scarlet** or **Violet**, open **Set up game** and choose the folder with your dumps and keys. The
   first setup compiles the game's code (10-20 minutes, once).
4. Press **Play**.

To build somewhere else: `"Build Tandem.bat" -To D:\Games\Tandem` (an existing install is updated in place; its games,
saves and mods are kept).

## Supported versions

| Game | Title ID | Version | `main` build ID |
|---|---|---|---|
| Pokémon Scarlet | `0100A3D008C5C000` | 4.0.0 | `421C5411B487EB4D` |
| Pokémon Violet | `01008F6008C5E000` | 4.0.0 | `709BFD6611529864` |

DLC you own is read from its dumps too. Compressed dumps (`.nsz` / `.xcz`) have to be decompressed first.

## Tandem Launcher

- **Play**: one window for both games, each with its own game files, saves, settings and mods (`games\scarlet`,
  `games\violet`).
- **Set up game**: import from your dumps, version check, recompile; **Recompile only** after an engine update.
- **Mods**: switch on / off, priority, remove, install `.zip` / `.7z` / `.rar` by dropping them on the window.
- **Discover mods**: browse and install mods from [GameBanana](https://gamebanana.com/games/17220). Only files
  GameBanana's virus scan has cleared, only from gamebanana.com, only their game-data part. Emulator code patches
  (exefs / IPS / pchtxt) are refused: 60 fps and higher resolutions are built in.
- **Textures**: HD texture packs, texture mods, and dumping the game's textures to make your own pack.
- **Settings**: 30 / 60 fps, internal resolution 1080p to 8K, fullscreen, HDR, shadows, ambient occlusion, indirect
  light. Everything else is in the game's own menu: **F1** while playing.

More in [launcher/README.md](launcher/README.md).

## Requirements

| | |
|---|---|
| **OS** | Windows 10 / 11, 64-bit |
| **CPU** | x86-64 with AVX2 |
| **GPU** | Vulkan 1.3 |
| **RAM** | 16 GB recommended |
| **Disk** | about 15 GB per game, plus about 5 GB for the build tools and the compiled code |

## Repository layout

| Path | What |
|---|---|
| `launcher/` | Tandem Launcher (C# / WPF, .NET 8) |
| `tools/` | the recompiler (ARM64 to C), the game-file importer (`tools/zaimport`), build scripts |
| `runtime/` | the engine: HLE Switch services, the NVN implementation on Vulkan, the Maxwell shader translator |
| `Build Tandem.bat`, `tools/tandem/` | the one-step build; `make tandem` does the same from an MSYS2 shell |
| `docs/` | upstream documentation, including the original [PokeV-Recomp README](docs/PokeV-Recomp-README.md) |

Never commit or attach game files, extracted data, generated code (`generated-*`), keys, fonts, saves or textures.

## Credits

This project stands on the work of others, used under the MIT license:

- **[Peiikai](https://github.com/peiikai)**: [LAS Recomp](https://github.com/peiikai/LASRecomp), the static recompiler,
  runtime, renderers and shader translator everything here is built on.
- **[Hexveil404](https://github.com/Hexveil404)** and contributors: [ZA Recomp](https://github.com/Hexveil404/PokeZ-ARecomp),
  the Switch OS services and Pokémon engine support.
- **[fabioschmidt01](https://github.com/fabioschmidt01)**, [Atterax](https://github.com/Atterax),
  [didslima](https://github.com/didslima) and contributors: [PokeV-Recomp](https://github.com/fabioschmidt01/PokeV-Recomp),
  the Pokémon Violet port.
- **Ryujinx** as a behavioural reference, **envytools** and **Switchbrew** for documentation, and the libraries listed in
  [docs/LASRecomp-README.md](docs/LASRecomp-README.md). Mods on GameBanana belong to their authors.

## License

[MIT](LICENSE). Third-party components in `third_party/` keep their own licenses.

## Legal

A fan project. It is not affiliated with, endorsed or sponsored by Nintendo, Game Freak, Creatures or The Pokémon
Company. *Pokémon*, *Pokémon Scarlet* and *Pokémon Violet* are trademarks of their respective owners. No game files,
code, assets, fonts or keys are included or distributed; you must provide your own legally obtained copies.
