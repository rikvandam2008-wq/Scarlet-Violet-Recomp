using System.IO;

namespace Tandem.Core;

/// <summary>One of the two games. Each lives in its own folder next to the engine: games\scarlet or games\violet
/// (game files in extracted\, compiled code extracted\lasgame.dll, saves, settings larecomp.ini, mods\, textures\).</summary>
public sealed class Game
{
    public string Slot { get; }          // "scarlet" / "violet": the engine's LARECOMP_GAME
    public string Name { get; }          // "Scarlet"
    public string TitleId { get; }       // the base game's title ID
    public string BuildId { get; }       // exefs/main build ID of the supported 4.0.0 update
    public string Root { get; }

    public Game(string installDir, string slot, string name, string titleId, string buildId)
    {
        Slot = slot; Name = name; TitleId = titleId; BuildId = buildId;
        Root = Path.Combine(installDir, "games", slot);
    }

    public string Extracted => Path.Combine(Root, "extracted");
    public string SettingsFile => Path.Combine(Root, "larecomp.ini");
    public string ModsDir => Path.Combine(Root, "mods");
    public string HdTexturesDir => Path.Combine(Root, "textures", "hd");
    public string DumpTexturesDir => Path.Combine(Root, "textures", "dump");
    public string SavesDir => Path.Combine(Root, "save");

    /// <summary>The build ID in extracted\exefs\main (NSO header, 8 bytes at 0x40), lowercase hex; null: none.</summary>
    public string? InstalledBuild()
    {
        var main = Path.Combine(Extracted, "exefs", "main");
        if (!File.Exists(main)) return null;
        using var f = File.OpenRead(main);
        if (f.Length < 0x48) return null;
        f.Position = 0x40;
        var id = new byte[8];
        f.ReadExactly(id);
        return Convert.ToHexString(id).ToLowerInvariant();
    }

    public GameState State()
    {
        var build = InstalledBuild();
        if (build == null || !File.Exists(Path.Combine(Extracted, "romfs.bin"))) return GameState.NotSetUp;
        if (build != BuildId) return GameState.WrongVersion;
        var lib = Path.Combine(Extracted, "lasgame.dll");
        var id = Path.Combine(Extracted, "lasgame.dll.id");
        if (!File.Exists(lib) || !File.Exists(id) ||
            !File.ReadAllText(id).Trim().EndsWith(BuildId, StringComparison.OrdinalIgnoreCase))
            return GameState.NotCompiled;
        // a library only loads into the engine it was built with (its generator/flags hash): after the other game was
        // recompiled with a newer engine, this one needs its code compiled again too
        var engine = Path.Combine(Games.InstallDir, "engine.id");
        if (File.Exists(engine) &&
            !File.ReadAllText(id).Trim().StartsWith(File.ReadAllText(engine).Trim() + "-", StringComparison.OrdinalIgnoreCase))
            return GameState.NotCompiled;
        return GameState.Ready;
    }
}

public enum GameState { Ready, NotSetUp, WrongVersion, NotCompiled }

public static class Games
{
    public static string InstallDir { get; } = AppContext.BaseDirectory.TrimEnd('\\');

    public static readonly Game Scarlet = new(InstallDir, "scarlet", "Scarlet", "0100A3D008C5C000", "421c5411b487eb4d");
    public static readonly Game Violet = new(InstallDir, "violet", "Violet", "01008F6008C5E000", "709bfd6611529864");
    public static readonly Game[] All = { Scarlet, Violet };

    public static Game BySlot(string? slot) => slot == "scarlet" ? Scarlet : Violet;

    // (its own name: the game libraries import the engine's functions from "PokeV-Recomp.exe" by name)
    public static string EngineExe => Path.Combine(InstallDir, "PokeV-Recomp.exe");
}
