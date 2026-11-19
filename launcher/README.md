# Tandem Launcher

A launcher, game setup tool and mod manager for native PC builds of *Pokémon Scarlet* and *Pokémon Violet*,
made by static recompilation from your own copies of the games.

> **No game files are included, and none are downloaded.** You set the games up from your own dumps: the base game,
> its 4.0.0 update and any DLC you own, plus the `prod.keys` from your own Switch. Everything made from them stays on
> your PC.

## What it does

- **Play**: one window for both games. Pick Scarlet or Violet; each has its own game files, saves, settings and mods.
- **Set up game**: reads your dumps, checks they're version 4.0.0, translates the game's ARM64 code to C and compiles
  it for your PC (10-20 minutes, once). **Recompile only** builds the code again after an engine update.
- **Mods**: switch mods on and off, set their priority, remove them, install `.zip` / `.7z` / `.rar` by dropping them.
- **Discover mods**: browse and install mods from [GameBanana](https://gamebanana.com/games/17220). Only files
  GameBanana's virus scan has cleared are installed, only from gamebanana.com, and only their game-data part (no
  program files). Mods that ship loose files are placed by matching their paths against the game's own pack index.
  Emulator code patches (exefs / IPS) are refused: the frame rate and resolution are built in.
- **Textures**: HD texture packs for the engine's texture replacement, texture mods, and dumping the game's textures
  to make your own pack.
- **Settings**: frame rate (30 / 60), internal resolution (1080p to 8K), fullscreen, HDR, shadows, lighting.

## Requirements

- Windows 10 / 11 x64, a Vulkan 1.3 GPU, an AVX2 CPU, .NET 8 Desktop Runtime.
- The engine is the rest of this repository. `Build Tandem.bat` (repository root) installs the build tools if they're
  missing (MSYS2 with clang and make, Python 3.12, the .NET 8 SDK), builds the engine and the launcher with
  `make tandem`, and puts both in `build\Tandem`. The install remembers this source folder (`tandem.json`): setting a game
  up compiles it from here.

## Install layout

```
<install>\
  Tandem Launcher.exe
  PokeV-Recomp.exe + its DLLs      the engine (keeps this name: the game libraries load it by name)
  games\scarlet\, games\violet\    each game's files, compiled code, saves, settings, mods, textures
```

## Building

Everything at once, from the repository root: `Build Tandem.bat` (or `make tandem [TANDEM=<folder>]` in an MSYS2
shell). The launcher alone: `dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o
publish` in this folder.

## Made with Claude Code

Tandem Launcher was made with [Claude Code](https://claude.com/claude-code), Anthropic's AI coding tool, as new code
for this project (it doesn't copy the upstream start screen; it works with the engine's file layout, settings and mod
format, which it reads and writes as the engine expects). It may not be kept up to date.

## Credits

The full list is in [CREDITS.md](../CREDITS.md). The engine Tandem starts is the work of others, used under the MIT license: **LAS Recomp** by Peiikai (recompiler,
runtime, renderers, shader translator), **ZA Recomp** by Hexveil404 and contributors (Switch services, Pokémon engine
support) and **PokeV-Recomp** by fabioschmidt01, Atterax, didslima and contributors (the Violet port). Mods on GameBanana
belong to their authors. Archive extraction: [SharpCompress](https://github.com/adamhathcock/sharpcompress) (MIT).

## Legal

A fan project, not affiliated with or endorsed by Nintendo, Game Freak, Creatures or The Pokémon Company. Pokémon,
Scarlet and Violet are trademarks of their owners. This repository holds no game code, assets, keys or files made from
them; never add any.
