using System.IO;
using System.Text.RegularExpressions;

namespace Tandem.Core;

/// <summary>HD texture packs for the native port's texture replacement (runtime/nvn/vulkan/textures.c): images named
/// after the game texture's hash, textures\hd\&lt;16 hex digits&gt;.png or .dds, drawn instead of the game's texture
/// when setting tex_hd is on. (Texture mods for emulators that replace files in the RomFS are ordinary mods: Mods page.)
/// Packs are made from the game's own textures: with tex_dump on, the game saves each texture it loads to
/// textures\dump\, which an upscaler turns into a pack.</summary>
public static class TexturePacks
{
    private static readonly Regex HashName = new(@"^[0-9a-fA-F]{16}\.(png|dds)$");

    public static int Count(string dir) =>
        Directory.Exists(dir) ? Directory.EnumerateFiles(dir).Count(f => HashName.IsMatch(Path.GetFileName(f))) : 0;

    /// <summary>Copies every hash-named texture from an archive or folder into the game's textures\hd. Returns how many.</summary>
    public static int Install(string source, Game game, IProgress<double>? progress = null)
    {
        string work = Path.Combine(Path.GetTempPath(), "tandem-tex-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            string scan = Directory.Exists(source) ? source : work;
            if (!Directory.Exists(source))
            {
                if (!ModArchive.IsArchive(source)) throw new InvalidOperationException("Pick a .zip / .7z / .rar or a folder.");
                ModArchive.Extract(source, work, progress);
            }
            var files = Directory.EnumerateFiles(scan, "*", SearchOption.AllDirectories)
                                 .Where(f => HashName.IsMatch(Path.GetFileName(f))).ToList();
            if (files.Count == 0)
                throw new InvalidOperationException("No textures named like 0123456789abcdef.png / .dds were found. " +
                    "Packs for this port are made from the game's own texture hashes (see Make your own).");
            Directory.CreateDirectory(game.HdTexturesDir);
            foreach (var f in files) File.Copy(f, Path.Combine(game.HdTexturesDir, Path.GetFileName(f).ToLowerInvariant()), true);
            return files.Count;
        }
        finally { try { if (Directory.Exists(work)) Directory.Delete(work, true); } catch { } }
    }

    public static void Clear(Game game)
    {
        if (!Directory.Exists(game.HdTexturesDir)) return;
        foreach (var f in Directory.EnumerateFiles(game.HdTexturesDir).Where(f => HashName.IsMatch(Path.GetFileName(f))))
            File.Delete(f);
    }
}
