using System.IO;
using System.Text.Json;

namespace Tandem.Core;

/// <summary>What recompiling a game needs on this PC: the engine's source (the recompiler and runtime), MSYS2's clang
/// and make, the generator's Python environment, and the .NET SDK for the game-file importer. Nothing of a game is in
/// any of these: the game is read from the player's own dumps when it's set up.</summary>
public sealed class Toolchain
{
    public const string Msys = @"C:\msys64";
    public static string Clang => Path.Combine(Msys, "ucrt64", "bin", "clang.exe");
    public static string Make => Path.Combine(Msys, "usr", "bin", "make.exe");

    public string? EngineSource { get; private set; }
    public string? Python => EngineSource == null ? null : Path.Combine(EngineSource, "tools", "venv", "Scripts", "python.exe");
    public string? ImporterProject => EngineSource == null ? null : Path.Combine(EngineSource, "tools", "zaimport");
    public string? Importer => EngineSource == null ? null : Path.Combine(EngineSource, "build", "zaimport", "zaimport.exe");
    public string? SetupScript => EngineSource == null ? null : Path.Combine(EngineSource, "tools", "setup_windows.ps1");

    public bool HasEngine => EngineSource != null;
    public bool HasCompiler => File.Exists(Clang) && File.Exists(Make);
    public bool HasPython => Python != null && File.Exists(Python);
    public bool HasDotnet => FindOnPath("dotnet.exe") != null;
    public bool Ready => HasEngine && HasCompiler && HasPython && HasDotnet;

    // ---- the engine source folder: remembered in tandem.json next to the launcher, else looked for nearby
    private static string ConfigFile => Path.Combine(Games.InstallDir, "tandem.json");
    private sealed class Config { public string? EngineSource { get; set; } }

    public static bool IsEngine(string? dir) =>
        dir != null && File.Exists(Path.Combine(dir, "Makefile")) && File.Exists(Path.Combine(dir, "tools", "gen.py")) &&
        File.Exists(Path.Combine(dir, "tools", "sv_patches.py")) && Directory.Exists(Path.Combine(dir, "runtime"));

    public static Toolchain Detect()
    {
        var t = new Toolchain();
        string? saved = null;
        try { if (File.Exists(ConfigFile)) saved = JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigFile))?.EngineSource; } catch { }
        var candidates = new List<string?> { saved, Environment.GetEnvironmentVariable("TANDEM_ENGINE"), Path.Combine(Games.InstallDir, "engine") };
        var parent = Path.GetDirectoryName(Games.InstallDir);
        if (parent != null) try { candidates.AddRange(Directory.GetDirectories(parent)); } catch { }
        t.EngineSource = candidates.FirstOrDefault(IsEngine);
        return t;
    }

    public static void SaveEngine(string dir)
    {
        File.WriteAllText(ConfigFile, JsonSerializer.Serialize(new Config { EngineSource = dir }, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static string? FindOnPath(string exe) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => { try { return Path.Combine(d.Trim(), exe); } catch { return ""; } })
            .FirstOrDefault(File.Exists);
}
