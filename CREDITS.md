# Credits

Scarlet-Violet-Recomp is built on other people's work. Almost all of the engine - the recompiler, the Switch OS and GPU
reimplementation, the renderer and the Pokémon-specific fixes - was written by the projects and people below, long
before this repository existed. This project added Scarlet support, a data folder per game, Tandem Launcher and the
one-step build on top (see [How this project was made](#how-this-project-was-made)).

Where something here came from is listed as accurately as it can be. If you're an author and something of yours is
missing or wrong, please open an issue.

## The projects this code comes from

This repository is a fork. Its history contains every upstream commit, with its original author.

### LAS Recomp - the foundation
**[Peiikai](https://github.com/peiikai)** - [github.com/peiikai/LASRecomp](https://github.com/peiikai/LASRecomp) (MIT)

The static recompiler (`tools/gen.py`, `lifter.py`, `analyze.py` and the passes), the runtime architecture (kernel,
IPC, HLE services), the NVN-on-Vulkan renderer and the Maxwell shader translator, made for *The Legend of Zelda: Link's
Awakening*. Contributors to LAS Recomp: [@dvilelaf](https://github.com/dvilelaf), [@Tensorlong](https://github.com/Tensorlong),
[@mansarip](https://github.com/mansarip), [@we11adam](https://github.com/we11adam), [@Maganra](https://github.com/Maganra),
[@hjqcan](https://github.com/hjqcan), [@gitarre1234rr5](https://github.com/gitarre1234rr5), [@rkammoun](https://github.com/rkammoun)
(what each did: [docs/LASRecomp-README.md](docs/LASRecomp-README.md#credits)).

### ZA Recomp - Pokémon engine support
**[Hexveil404](https://github.com/Hexveil404)** - [github.com/Hexveil404/PokeZ-ARecomp](https://github.com/Hexveil404/PokeZ-ARecomp) (MIT)

The *Pokémon Legends: Z-A* port of LAS Recomp: the Switch services and the Game Freak (Trinity) engine support these
games need, the game-file importer (`tools/zaimport`), the pack-index handling that lets mods replace packed files
(`runtime/services/fs.c`), DLAA, and much more. Contributors: [@Primus92](https://github.com/Primus92),
[@Atterax](https://github.com/Atterax), [@wimw85](https://github.com/wimw85), [@lucabravi](https://github.com/lucabravi),
[@gabrielsem16-bot](https://github.com/gabrielsem16-bot), [@gdsistemas-web](https://github.com/gdsistemas-web) and
everyone who reported issues and tested builds.

### PokeV-Recomp - the Violet port
**[fabioschmidt01](https://github.com/fabioschmidt01)** - [github.com/fabioschmidt01/PokeV-Recomp](https://github.com/fabioschmidt01/PokeV-Recomp) (MIT)

The *Pokémon Violet* port this repository is forked from: Violet 4.0.0 booting and playable, its 60 fps frame clock,
ultrawide camera, Intel Arc tuning, XeSS, and the patches in `tools/sv_patches.py` that Scarlet now uses unchanged
(credited there to fabioschmidt01 & Antigravity). Contributors: **[Atterax](https://github.com/Atterax)** (the hair
shader fix, NPC eyebrows, stick dead zone, banded sky, city crash, sandstorm / fog triangles, NPC triangles, UI animation
speed, DLAA, ambient occlusion and indirect light, NPC count and detail options) and
**[didslima](https://github.com/didslima)** (Linux data folder, 32:9 ultrawide). Details: the
[original README](docs/PokeV-Recomp-README.md) and `HISTORICO_CORRECOES_E_DESENVOLVIMENTO.md`.

### Also credited by the projects above
- **[LAS-Randomizer](https://github.com/Owen-Splat/LAS-Randomizer)** by Owen_Splat, Glan and ProfessorLaw (MIT,
  vendored in `third_party/LAS-Randomizer`; Link's Awakening only).
- **[totk-pc-recomp](https://github.com/valentderah/totk-pc-recomp)** by valentderah (MIT) - the DLAA integration
  (`runtime/nvn/vulkan/dlaa.c`) follows its `runtime/vk_dlaa.c`.
- **GameBanana mods re-implemented in LAS Recomp's options** (no files used): 360 Controls by Owen_Splat (blur removal
  by theboy181), XBOX UI by fellow_demo, PS4 UI Buttons by messiahgov, No Hero Mode Indicator by Boo/Calla.
- **[Dusklight](https://github.com/TwilitRealm/dusklight)** by the TwilitRealm team - design inspiration for LAS
  Recomp's start screen.

## Third-party code in this repository (`third_party/`, each under its own license)

| Component | Author | License | Used for |
|---|---|---|---|
| [Dear ImGui](https://github.com/ocornut/imgui) | Omar Cornut and contributors | MIT | the engine's menus and start screen |
| [AMD FidelityFX FSR 2](https://github.com/GPUOpen-Effects/FidelityFX-FSR2) (and FSR 1, ported to GLSL) | AMD | MIT | upscaling / temporal anti-aliasing |
| [SMAA](https://github.com/iryoku/smaa) | Jorge Jimenez, Jose I. Echevarria, Belen Masia, Fernando Navarro, Diego Gutierrez | MIT | anti-aliasing |
| [ooz](https://github.com/powzix/ooz) | Powzix | GPL 3.0 or later | Oodle (Kraken etc.) decompression |
| Ryujinx-derived code (`third_party/ryujinx`) | [Ryujinx Team and contributors](https://github.com/Ryujinx/Ryujinx) | MIT | Switch behaviour reference |
| [Intel XeSS SDK](https://github.com/intel/xess) | Intel | Intel Simplified Software License | the XeSS upscaler |
| [Baloo 2](https://github.com/EkType/Baloo2) | Ek Type | SIL OFL 1.1 | the engine start screen's font |

Ported into the engine's own code: [XeGTAO](https://github.com/GameTechDev/XeGTAO) by Intel (MIT) - ambient occlusion
(`runtime/nvn/vulkan/`).

## Libraries and tools used at build or run time (not stored here)

- [SDL 3](https://libsdl.org) (zlib), [FFmpeg](https://ffmpeg.org) (LGPL), [Vulkan Memory Allocator](https://github.com/GPUOpen-LibrariesAndSDKs/VulkanMemoryAllocator) (MIT),
  [shaderc](https://github.com/google/shaderc) and SPIRV-Tools (Apache 2.0), the Vulkan loader and headers (Apache 2.0) -
  through [MSYS2](https://www.msys2.org).
- [LLVM / Clang / LLD](https://llvm.org) (Apache 2.0 with LLVM exceptions) - compiles the engine and the games' code.
- [Python](https://python.org) (PSF), [Capstone](https://www.capstone-engine.org) (BSD), [Unicorn](https://www.unicorn-engine.org)
  (GPL 2.0; bindings BSD), [python-lz4](https://github.com/python-lz4/python-lz4) (BSD) - the code generator.
- [LibHac](https://github.com/Thealexbarney/LibHac) by Alex Barney (BSD 3-clause) - reads the player's dumps in the importer.
- [.NET](https://dotnet.microsoft.com) and WPF (MIT) and [SharpCompress](https://github.com/adamhathcock/sharpcompress)
  by Adam Hathcock (MIT) - Tandem Launcher and its archive extraction.
- [Zig](https://ziglang.org) (MIT) - the upstream start screen's player-side compiler.
- NVIDIA DLSS (`nvngx_dlss.dll`, NVIDIA RTX SDK license) - optional, not included.

## Documentation and references

- [Switchbrew](https://switchbrew.org) - Switch system documentation (services, file formats such as RomFS and NSO).
- [envytools](https://github.com/envytools/envytools) - NVIDIA GPU documentation behind the shader translator.
- [Ryujinx](https://github.com/Ryujinx/Ryujinx) - a behavioural reference for the Switch kernel and NVN.
- [GameBanana](https://gamebanana.com)'s public API (apiv11) - Tandem's mod browser. Mods there belong to their authors.

## How this project was made

**Everything this repository added on top of PokeV-Recomp was made with [Claude Code](https://claude.com/claude-code)**,
Anthropic's AI coding tool, directed by RikvDam. That covers:

- Pokémon Scarlet 4.0.0 support (identifying that its build matches Violet's, `tools/sv_port.py`, the build ID entries);
- a data folder per game and the Scarlet / Violet switch (`runtime/paths.c`, `runtime/game_identity.h`, the start screen);
- the importer's `--title` option and tolerant package reading;
- Tandem Launcher (`launcher/`): game setup and recompiling from the player's dumps, the mod manager and GameBanana
  browser, textures and settings;
- the one-step build (`Build Tandem.bat`, `tools/tandem/build.ps1`, `make tandem`) and this documentation.

What Claude Code worked from, so nothing is passed off as new: the PokeV-Recomp source (the base of this fork, and the
model for how its start screen, mods, settings and texture packs work), the ZA Recomp source (read to understand the
same architecture), the engine's own code (RomFS reading, the pack index hash, `tools/zaimport`'s package handling),
public documentation (Switchbrew, GameBanana's API, Microsoft's .NET / WPF docs) and the libraries above. The
[SwitchRECOMP](https://github.com/swizardlv/switchrecomp) README was read while researching options; none of its code
is used. Tandem Launcher's code was written new for this project, without copying the upstream start screen.

The engine itself - nearly all of the code in `runtime/` and `tools/` - is the human-written work credited above, not
something Claude Code produced.
