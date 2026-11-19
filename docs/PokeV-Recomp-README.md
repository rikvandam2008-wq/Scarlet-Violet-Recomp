<div align="center">

<img src="docs/icon/icon_1024.png" alt="PokeV-Recomp icon" width="140">

<a href="https://recomplauncher.com/game/pokev-recomp"><img src="https://recomplauncher.com/assets/press/launcher/badge-dark.svg" alt="Get it on RecompLauncher" height="56"></a>

# PokeV-Recomp (SV Recomp)

**A native PC port of *Pokémon Violet*, built by static recompilation. Work in progress.**

**English** · [Português (Brasil)](README.pt-BR.md)

[Report an issue](../../issues) ·
[Releases](../../releases)

</div>

---

> [!NOTE]
> This is the original PokeV-Recomp README, kept for reference and credit. For Scarlet-Violet-Recomp, see the
> [main README](../README.md).

> [!WARNING]
> **Work in progress: expect bugs and crashes.**
> PokeV-Recomp is still at an early stage. Even in my own testing and my friends', there are graphical glitches,
> performance drops and crashes, and you will probably run into others. Please be patient, and help us by
> [reporting bugs and crashes](../../issues) (with your `logs/play.log` file when you can): every report helps
> improve the project.
>
> **Development will be slower for a while**, as my main focus right now is
> [PokeL-ARecomp](https://github.com/fabioschmidt01/PokeL-ARecomp).

> [!NOTE]
> **Latest updates (v0.0.23 to v0.0.28): big improvements in performance and stability, and many bug fixes.**
> - **v0.0.28, 12 community pull requests:** a crash entering cities fixed, black / stretched triangles around NPCs and in
>   the sandstorm and fog gone, NPCs keep their eyebrows up close, smooth sky, UI animations at the right speed at
>   60 fps, working ambient occlusion and indirect light, controller stick dead zone (map magnifier / fast travel),
>   NVIDIA DLAA, 32:9 ultrawide, and F1 options for how many people are shown and how far models keep full detail
>   (by [Atterax](https://github.com/Atterax) and [didslima](https://github.com/didslima), #5 to #16).
> - **Hair no longer glows purple / cyan** (found and fixed by [Atterax](https://github.com/Atterax), [#4](../../pull/4)).
>   Some safeguards in this fix are still to be added.
> - **Normal game speed below 60 fps**: no more slow motion at 40-50 fps.
> - **No more white specks** along triangle edges on Intel and AMD GPUs.
> - **Linux**: PokeV-Recomp's own data folder, and other games' files are refused instead of being compiled over
>   (by [didslima](https://github.com/didslima), [#2](../../pull/2)).
> - The supported game version is shown correctly as **4.0.0** (reported by Atterax, [#3](../../issues/3)).

**PokeV-Recomp** (SV Recomp) turns the Nintendo Switch code of *Pokémon Violet* into a native 64-bit program for
Windows (and Linux, barely tested so far).
The game's ARM64 machine code is translated ahead of time into C and compiled for x86-64 on your own PC. The Switch's
operating system services, file system, audio and NVN graphics are re-implemented on top of Vulkan.

**No emulator runs while you play.** The game's code executes natively on your CPU and GPU.

> [!IMPORTANT]
> **You need your own copy of the game.** PokeV-Recomp does not include any game code, ROM, asset, texture, font or
> encryption key, and never will. You provide the files from the game you own; the recompilation happens locally on
> your machine.

---

## Current status

This is an early, in-development port. The game boots, loads a save and is playable in the open world, but it is not
finished and has only been tested on one machine (Intel Arc B580, Xeon E5-2650 v4, Windows 10).

**Known issues**

- The frame rate is unsteady in the open world (around 40 fps on the test machine, with drops), and there are hitches
  the first time each shader is compiled.
- Only *Pokémon Violet* 4.0.0 is recognised. *Pokémon Scarlet* is not supported yet.
- **Linux is barely tested.** Releases include an AppImage built automatically from the same code. A contributor
  started it and checked the setup on Bazzite (RTX 5070 Ti); nobody has reported a full play session yet. Reports are welcome ([docs/linux.md](docs/linux.md) lists what to include).

Every fix and its validation state is logged in
[HISTORICO_CORRECOES_E_DESENVOLVIMENTO.md](HISTORICO_CORRECOES_E_DESENVOLVIMENTO.md) (in Portuguese).

---

## Features

Most of these come from the runtime this project is built on (see [Credits](#credits--acknowledgements)). They are
available in the F1 menu; not all of them have been validated with *Pokémon Violet* yet.

### Performance
- **30 or 60 fps target** (option). See the known issues above for where it stands today.
- **Internal resolution scale**, with optional dynamic resolution.
- **Multi-threaded texture unpacking**, scaled to the number of CPU threads.

### Graphics
- **Upscalers**: bilinear, AMD FSR 1, Intel XeSS; AMD FSR 2 (temporal) and SMAA anti-aliasing.
- **Lighting options**: ambient occlusion (XeGTAO), indirect light, water reflections, shadow resolution and sharpness.
- **HDR output** (option).
- **HD texture packs**: an *Original / HD* switch in the menu loads replacement textures from `textures/hd/`.
- **Mods**: `mods/<name>/romfs` layers over the game's files, each switched on or off in the start screen's Mods tab.
  A Brazilian Portuguese translation by [PKG Traduções](https://pkgtraducoes.com.br/) installs this way (not included:
  get it from them). The Mods tab links their download and credits them.
- **Save backups**: each save is also copied to `save/backups/` (the 10 newest, at most one every 5 minutes).

### Controls
- Gamepads through SDL3 (Xbox, DualShock 4, DualSense, Switch Pro Controller, Steam Deck).
- Keyboard and mouse, with a mouse-driven camera and rebindable keys.

---

## Supported version

| Game | Title ID | Version | `main` build ID | Status |
|---|---|---|---|---|
| **Pokémon Violet** | `01008F6008C5E000` | **4.0.0** (update v786432) | `709BFD6611529864` | Supported |
| **Pokémon Scarlet** | `0100A3D008C5C000` | 4.0.0 | - | Not supported yet |

---

## Requirements

| Component | Expected minimum (not tested) | Tested |
|---|---|---|
| **Operating system** | Windows 10/11 64-bit, or Linux x86-64 with glibc 2.35+ | Windows 10 |
| **Processor** | x86-64 with AVX2, 4 cores | Intel Xeon E5-2650 v4 (12 cores / 24 threads) |
| **Graphics card** | Vulkan 1.3, 4 GB of video memory | Intel Arc B580 (12 GB) |
| **Memory** | 8 GB | 16 GB |
| **Storage** | ~15 GB free | SSD |

---

## Quick start

1. Download `PokeV-Recomp-windows-x64.zip` from the [Releases](../../releases) page (Linux:
   `PokeV-Recomp-linux-x86_64.AppImage`, untested - make it executable and run it; the steps below are the same).
2. Extract it into a folder you can write to (for example `C:\Games\PokeV-Recomp`).
3. Have your own copy of the game ready, in one of these forms:
   - the game extracted (`exefs` and `romfs` folders) at version **4.0.0** - no keys needed; or
   - your dump: the base game (`.xci` or `.nsp`), the **4.0.0 update** (`.nsp`) and the `prod.keys` from your own
     console.
4. Run `PokeV-Recomp.exe`.
5. On the start screen, choose the folder with your game files and click **Set up the game**.
   The game's version is checked first: only Pokémon Violet 4.0.0 is set up (Scarlet, other versions and other
   games' files are refused, with the reason on screen). On Linux, the game files, saves and settings live in
   `~/.local/share/pokev-recomp` (see [docs/linux.md](docs/linux.md)); `prod.keys` is also found in `~/.switch`.
6. The first setup recompiles the game's code on your PC. It takes 10 to 30 minutes depending on the CPU and happens
   once (again only when an update changes how the code is generated).
7. Click **Play**.

---

## Controls

Keyboard movement is on **W / A / S / D** and the camera follows the mouse. Every key and button can be seen and
changed in the F1 menu.

| Key | Action |
|---|---|
| F1 | Settings menu |
| F9 | Dump the next frame's render passes into `build/` (diagnostics) |
| F10 | Performance overlay |
| F11 | Toggle fullscreen |
| F12 | Save a screenshot to `screenshots/` |

---

## Contributors

Thank you to everyone who reported problems and sent fixes: without them these bugs would not have been found.

- **[Atterax](https://github.com/Atterax)**: found and fixed the purple / cyan hair - the shader translator ignored
  the condition code of branches, so the lighting took the wrong path for hair ([#4](../../pull/4)); reported that the
  supported update is the game's 4.0.0, not 3.0.1 ([#3](../../issues/3)); added `LARECOMP_CLOCK` for repeatable tests.
  In v0.0.28: NPC eyebrows / eyelashes ([#5](../../pull/5)), stick dead zone ([#6](../../pull/6)), banded sky
  ([#7](../../pull/7)), crash entering a city ([#8](../../pull/8)), stretched triangles in sandstorm / fog
  ([#9](../../pull/9)), black triangles following NPCs ([#10](../../pull/10)), UI animations at 60 fps
  ([#11](../../pull/11)), NVIDIA DLAA ([#12](../../pull/12)), ambient occlusion and indirect light
  ([#13](../../pull/13)), F1 > People shown ([#15](../../pull/15)) and People / Pokémon detail ([#16](../../pull/16)).
- **[didslima](https://github.com/didslima)**: tested the Linux AppImage on Bazzite, reported that it shared Legends:
  Arceus's data folder ([#1](../../issues/1)) and fixed it: PokeV-Recomp's own data folder, only Pokémon Violet's
  files accepted, PokeV-Recomp names in the AppImage, with tests ([#2](../../pull/2)). In v0.0.28: 32:9 ultrawide
  support - Hor+ camera and the HUD, menus and title art kept at 16:9 ([#14](../../pull/14)).

Reports and pull requests are welcome: see [CONTRIBUTING.md](CONTRIBUTING.md).

---

## Credits & acknowledgements

- **Peiikai**: creator of [LAS Recomp](https://github.com/peiikai/LASRecomp) - the static recompiler, the runtime
  architecture and the Maxwell shader translator this project is built on.
- **Hexveil404**: creator of [PokeZ-ARecomp](https://github.com/Hexveil404/PokeZ-ARecomp), which brought the Switch OS
  services and the Pokémon engine support.
- **fabioschmidt01 & Antigravity**: architecture, Intel Xeon multi-threading, Intel Arc B580 tuning, Intel XeSS
  integration, and the Pokémon Violet & Arceus ports.
- **Ryujinx project**: behavioural reference for the Switch kernel and the NVN GPU.
- **[PKG Traduções](https://pkgtraducoes.com.br/)**: the Brazilian Portuguese translation mod (distributed by them).
- **Open-source libraries**: SDL3, FFmpeg, Dear ImGui, Vulkan Memory Allocator, Shaderc, XeGTAO, LibHac, Intel XeSS SDK.

PokeV-Recomp's own code is under the [MIT license](LICENSE); the components in `third_party/` keep their own licenses.

---

## Legal notice

*PokeV-Recomp* is a fan-made research and preservation project. It is **not** endorsed by, affiliated with or sponsored
by Nintendo, Game Freak, Creatures Inc. or The Pokémon Company. *Pokémon*, *Pokémon Scarlet* and *Pokémon Violet* are
trademarks of their respective owners. No Nintendo code, game binary, texture, model or font is distributed in this
repository or in its releases.
