using System.IO;
using System.Text.Json;

namespace Tandem.Core;

/// <summary>Where an installed mod came from (mods\&lt;name&gt;\tandem.json, written by this launcher).</summary>
public sealed class ModSource
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public long GameBananaId { get; set; }
    public string? PageUrl { get; set; }
    public string? FileName { get; set; }
    public string? Version { get; set; }
    public DateTime Installed { get; set; }
    public string? Note { get; set; }
}

public sealed class InstalledMod
{
    public required string Folder { get; init; }   // folder name under mods\ (the engine's key)
    public required string Path { get; init; }
    public bool Enabled { get; set; }
    public ModSource? Source { get; init; }
    public long Files { get; init; }
    public long Bytes { get; init; }
    public string Title => Source?.Title ?? Folder;
    public string Detail =>
        $"{Files} file{(Files == 1 ? "" : "s")} · {Bytes / 1048576.0:0.#} MB" +
        (Source?.Author is { } a ? $" · by {a}" : "") +
        (Source?.GameBananaId > 0 ? " · GameBanana" : "");
}

/// <summary>A game's mods, as the engine reads them (runtime/services/fs.c): every mods\&lt;name&gt;\romfs folder is a
/// layer, in the order of the setting mods_order (unlisted ones after, by name); names in mods_off are skipped; the
/// first layer that has a file wins. At most 15 layers are loaded.</summary>
public sealed class ModManager
{
    public const int MaxActive = 15;
    private readonly Game _game;
    public ModManager(Game game) => _game = game;

    public List<InstalledMod> List()
    {
        var ini = new IniFile(_game.SettingsFile);
        var order = ini.GetList("mods_order");
        var off = ini.GetList("mods_off").ToHashSet(StringComparer.Ordinal);
        if (!Directory.Exists(_game.ModsDir)) return new();
        var mods = new List<InstalledMod>();
        foreach (var dir in Directory.GetDirectories(_game.ModsDir))
        {
            var name = System.IO.Path.GetFileName(dir);
            if (name.StartsWith('.')) continue;
            var romfs = System.IO.Path.Combine(dir, "romfs");
            if (!Directory.Exists(romfs)) continue;
            var files = Directory.EnumerateFiles(romfs, "*", SearchOption.AllDirectories).Select(f => new FileInfo(f)).ToList();
            ModSource? src = null;
            var meta = System.IO.Path.Combine(dir, "tandem.json");
            if (File.Exists(meta))
                try { src = JsonSerializer.Deserialize<ModSource>(File.ReadAllText(meta)); } catch { }
            mods.Add(new InstalledMod
            {
                Folder = name, Path = dir, Source = src, Files = files.Count, Bytes = files.Sum(f => f.Length),
                Enabled = !off.Contains(name) && !name.StartsWith('_'),
            });
        }
        int Rank(string n) { int i = order.IndexOf(n); return i < 0 ? int.MaxValue : i; }
        return mods.OrderBy(m => Rank(m.Folder)).ThenBy(m => m.Folder, StringComparer.Ordinal).ToList();
    }

    /// <summary>Order and on/off as shown (first = highest priority) into the settings file.</summary>
    public void Save(IReadOnlyList<InstalledMod> mods)
    {
        var ini = new IniFile(_game.SettingsFile);
        ini.SetList("mods_order", mods.Select(m => m.Folder));
        ini.SetList("mods_off", mods.Where(m => !m.Enabled).Select(m => m.Folder));
        ini.Save();
    }

    public void Delete(InstalledMod mod)
    {
        var full = System.IO.Path.GetFullPath(mod.Path);
        var root = System.IO.Path.GetFullPath(_game.ModsDir) + System.IO.Path.DirectorySeparatorChar;
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("not a mod folder");
        Directory.Delete(full, recursive: true);
    }

    /// <summary>Installs a mod from an archive or a folder. Returns the result message. Throws with a readable message
    /// when there's nothing installable in it.</summary>
    public string Install(string source, ModSource meta, IProgress<double>? progress = null)
    {
        string work = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tandem-" + Guid.NewGuid().ToString("N")[..8]);
        int blocked = 0;
        try
        {
            string scan;
            if (Directory.Exists(source)) scan = source;
            else if (ModArchive.IsArchive(source)) { blocked = ModArchive.Extract(source, work, progress); scan = work; }
            else throw new InvalidOperationException("Only .zip, .7z and .rar archives, or a folder, can be installed.");

            var layout = ModArchive.Inspect(scan, _game);
            if (layout.RomfsDir == null) throw new InvalidOperationException(layout.Note);

            var name = ModArchive.SafeFolderName(meta.Title ?? System.IO.Path.GetFileNameWithoutExtension(source));
            var dest = System.IO.Path.Combine(_game.ModsDir, name);
            for (int n = 2; Directory.Exists(dest) && !(meta.GameBananaId > 0 && SameSource(dest, meta)); n++)
                dest = System.IO.Path.Combine(_game.ModsDir, $"{name} ({n})");
            if (Directory.Exists(dest)) Directory.Delete(dest, true);  // (an update of the same GameBanana mod)
            CopyTree(layout.RomfsDir, System.IO.Path.Combine(dest, "romfs"));
            meta.Installed = DateTime.Now;
            meta.Note = layout.Note;
            File.WriteAllText(System.IO.Path.Combine(dest, "tandem.json"),
                              JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }));

            // new mods go to the top (highest priority) and on, unless the active limit is reached
            var mods = List();
            var mine = mods.First(m => m.Folder == System.IO.Path.GetFileName(dest));
            mods.Remove(mine);
            mods.Insert(0, mine);
            mine.Enabled = mods.Count(m => m.Enabled && m != mine) < MaxActive;
            Save(mods);

            var msg = $"Installed \"{mine.Title}\" ({mine.Files} files).";
            if (!mine.Enabled) msg += $" It's switched off: {MaxActive} mods are already on (the engine's limit).";
            if (layout.Note != null) msg += " " + layout.Note;
            if (blocked > 0) msg += $" {blocked} file(s) were skipped (program files or unsafe paths).";
            return msg;
        }
        finally
        {
            try { if (Directory.Exists(work)) Directory.Delete(work, true); } catch { }
        }
    }

    private static bool SameSource(string dir, ModSource meta)
    {
        try
        {
            var m = JsonSerializer.Deserialize<ModSource>(File.ReadAllText(System.IO.Path.Combine(dir, "tandem.json")));
            return m?.GameBananaId == meta.GameBananaId;
        }
        catch { return false; }
    }

    private static void CopyTree(string from, string to)
    {
        foreach (var f in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            var t = System.IO.Path.Combine(to, System.IO.Path.GetRelativePath(from, f));
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(t)!);
            File.Copy(f, t, overwrite: true);
        }
    }
}
