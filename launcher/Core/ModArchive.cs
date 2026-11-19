using System.IO;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace Tandem.Core;

/// <summary>What a downloaded or picked mod holds, worked out after extracting it to a scratch folder.</summary>
public sealed record ModLayout(string? RomfsDir, bool HasCodePatch, string? Note);

/// <summary>Mod archives (.zip / .7z / .rar) and folders: extracted safely, then the game-data part found.
///
/// The native port only takes data mods - files that replace or add files in the game's RomFS. Mods made for
/// emulators and modded consoles ship that as a romfs\ folder somewhere inside: at the top, under the mod's name, or
/// under atmosphere\contents\&lt;title id&gt;\ (one archive may carry Scarlet's and Violet's). Code patches (exefs\,
/// .ips, .pchtxt, cheats) change the Switch program, which isn't there in a recompiled game: they're reported, not
/// installed.</summary>
public static class ModArchive
{
    // Never written anywhere: program files have no place in a data mod, whatever the archive says
    private static readonly HashSet<string> Blocked = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bat", ".cmd", ".com", ".scr", ".ps1", ".vbs", ".js", ".jse", ".msi", ".lnk", ".reg",
        ".hta", ".cpl", ".jar", ".sys", ".psm1", ".wsf",
    };

    public static bool IsArchive(string path) =>
        Path.GetExtension(path).ToLowerInvariant() is ".zip" or ".7z" or ".rar";

    /// <summary>Extracts every entry under dest. Entries whose path would land outside dest ("..\", absolute) and
    /// program files are skipped. Returns how many files were skipped as blocked.</summary>
    public static int Extract(string archive, string dest, IProgress<double>? progress = null)
    {
        Directory.CreateDirectory(dest);
        var root = Path.GetFullPath(dest) + Path.DirectorySeparatorChar;
        int blocked = 0;
        using var a = ArchiveFactory.OpenArchive(archive, new SharpCompress.Readers.ReaderOptions());
        var entries = a.Entries.Where(e => !e.IsDirectory).ToList();
        long total = Math.Max(1, entries.Sum(e => e.Size)), done = 0;
        foreach (var e in entries)
        {
            var key = (e.Key ?? "").Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
            var target = Path.GetFullPath(Path.Combine(dest, key));
            if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)) { blocked++; continue; }  // (zip slip)
            if (Blocked.Contains(Path.GetExtension(target))) { blocked++; continue; }
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using (var outFile = File.Create(target)) e.WriteTo(outFile, null);
            done += e.Size;
            progress?.Report((double)done / total);
        }
        return blocked;
    }

    /// <summary>The romfs folder to install from, for this game (by title ID when the archive carries both).</summary>
    public static ModLayout Inspect(string dir, Game game)
    {
        var romfs = Directory.EnumerateDirectories(dir, "*", SearchOption.AllDirectories)
            .Where(d => Path.GetFileName(d).Equals("romfs", StringComparison.OrdinalIgnoreCase))
            .Where(d => Directory.EnumerateFiles(d, "*", SearchOption.AllDirectories).Any())
            .ToList();
        bool code = Directory.EnumerateDirectories(dir, "*", SearchOption.AllDirectories)
                        .Any(d => Path.GetFileName(d).Equals("exefs", StringComparison.OrdinalIgnoreCase)) ||
                    Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
                        .Any(f => Path.GetExtension(f).ToLowerInvariant() is ".ips" or ".pchtxt");

        if (romfs.Count == 0 && PackIndex.For(game) is { } idx)
        {
            // no romfs folder: the folder whose paths match the most of the game's packed files is the mod's root
            var score = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var top = Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar);
            foreach (var file in Directory.EnumerateFiles(top, "*", SearchOption.AllDirectories))
                for (var anc = Path.GetDirectoryName(file); anc != null && anc.Length >= top.Length; anc = Path.GetDirectoryName(anc))
                {
                    if (idx.Has(Path.GetRelativePath(anc, file))) score[anc] = score.GetValueOrDefault(anc) + 1;
                    if (anc.Length == top.Length) break;
                }
            if (score.Count > 0)
            {
                int best = score.Values.Max();
                var ties = score.Where(kv => kv.Value == best).Select(kv => kv.Key).ToList();
                var otherName = Games.All.First(g => g != game).Name;
                var pickDir = ties.FirstOrDefault(d => d.Contains(game.Name, StringComparison.OrdinalIgnoreCase) ||
                                                       d.Contains(game.TitleId, StringComparison.OrdinalIgnoreCase))
                              ?? ties.FirstOrDefault(d => !d.Contains(otherName, StringComparison.OrdinalIgnoreCase))
                              ?? ties.OrderBy(d => d.Length).First();
                var rel = Path.GetRelativePath(top, pickDir);
                return new ModLayout(pickDir, code,
                    $"{best} file(s) replace game files" + (rel == "." ? "." : $" (from {rel})."));
            }
        }

        if (romfs.Count == 0)
            return new ModLayout(null, code, code
                ? "This is a code patch (exefs / IPS / pchtxt) made for emulators. The native port runs recompiled " +
                  "code, so it can't be applied - frame rate and resolution are already in the launcher's settings."
                : "No romfs folder was found in it, so it isn't a data mod this game can load.");

        // the other game's copy is skipped when the archive has one per title ID
        var other = Games.All.First(g => g != game).TitleId;
        var pick = romfs.FirstOrDefault(d => d.Contains(game.TitleId, StringComparison.OrdinalIgnoreCase))
                   ?? romfs.FirstOrDefault(d => !d.Contains(other, StringComparison.OrdinalIgnoreCase))
                   ?? romfs[0];
        // a code patch whose romfs part replaces nothing (FPS / resolution patches ship a config file there) isn't a mod
        // this port can use
        if (code && PackIndex.For(game) is { } ix2 &&
            !Directory.EnumerateFiles(pick, "*", SearchOption.AllDirectories).Any(f => ix2.Has(Path.GetRelativePath(pick, f))))
            return new ModLayout(null, true,
                "This is a code patch (exefs / IPS / pchtxt) made for emulators. The native port runs recompiled " +
                "code, so it can't be applied - frame rate and resolution are already in the launcher's settings.");
        var note = romfs.Count > 1 ? $"The archive has {romfs.Count} romfs folders (options or both games); " +
                                     $"installed: {Path.GetRelativePath(dir, pick)}." : null;
        if (code) note = (note == null ? "" : note + " ") + "Its code patch part was left out (not usable natively).";
        return new ModLayout(pick, code, note);
    }

    public static string SafeFolderName(string name)
    {
        var bad = Path.GetInvalidFileNameChars().Concat(new[] { '|', '.' }).ToHashSet();
        var s = new string(name.Select(c => bad.Contains(c) ? ' ' : c).ToArray()).Trim();
        while (s.Contains("  ")) s = s.Replace("  ", " ");
        if (s.StartsWith('_')) s = s.TrimStart('_');  // (a leading '_' means "off" to the engine)
        if (s.Length > 60) s = s[..60].Trim();
        return s.Length == 0 ? "Mod" : s;
    }
}
