using System.IO;
using System.Text;

namespace Tandem.Core;

/// <summary>The paths the game keeps in its packs, as the hashes its pack index lists (arc/data.trpfd in the RomFS:
/// a FlatBuffer whose root table's field 0 is every packed file's path hash, FNV-1a 64 with Trinity's offset basis).
/// Mods for these games ship loose files at those paths (chara/model_pc/..., world/...), often without a romfs folder
/// around them; the folder whose relative paths match the most packed files is the mod's root. (The engine serves a
/// loose file over the packed one by taking its hash out of the index: runtime/services/fs.c trpfd_unlist.)</summary>
public sealed class PackIndex
{
    private readonly HashSet<ulong> _hashes;
    private PackIndex(HashSet<ulong> h) => _hashes = h;
    public int Count => _hashes.Count;

    private static readonly Dictionary<string, PackIndex?> Cache = new();

    public static PackIndex? For(Game game)
    {
        lock (Cache)
        {
            if (Cache.TryGetValue(game.Slot, out var c)) return c;
            PackIndex? idx = null;
            try { idx = Load(Path.Combine(game.Extracted, "romfs.bin")); } catch { }
            Cache[game.Slot] = idx;
            return idx;
        }
    }

    public static ulong Hash(string relPath)
    {
        ulong x = 0xCBF29CE484222645UL;
        foreach (var b in Encoding.UTF8.GetBytes(relPath.Replace('\\', '/'))) x = (x ^ b) * 0x100000001B3UL;
        return x;
    }

    public bool Has(string relPath) => _hashes.Contains(Hash(relPath));

    private static PackIndex? Load(string romfsBin)
    {
        if (!File.Exists(romfsBin)) return null;
        using var f = File.OpenRead(romfsBin);
        using var r = new BinaryReader(f);
        var h = new ulong[10];
        for (int i = 0; i < 10; i++) h[i] = r.ReadUInt64();
        ulong dirMeta = h[3], fileMeta = h[7], dataOff = h[9];

        (uint sib, uint childDir, uint childFile, string name) Dir(uint o)
        {
            f.Position = (long)(dirMeta + o);
            r.ReadUInt32(); uint s = r.ReadUInt32(), cd = r.ReadUInt32(), cf = r.ReadUInt32(); r.ReadUInt32();
            int n = (int)r.ReadUInt32();
            return (s, cd, cf, Encoding.UTF8.GetString(r.ReadBytes(n)));
        }
        (uint sib, ulong off, ulong size, string name) File_(uint o)
        {
            f.Position = (long)(fileMeta + o);
            r.ReadUInt32(); uint s = r.ReadUInt32(); ulong off = r.ReadUInt64(), size = r.ReadUInt64(); r.ReadUInt32();
            int n = (int)r.ReadUInt32();
            return (s, off, size, Encoding.UTF8.GetString(r.ReadBytes(n)));
        }

        const uint None = 0xFFFFFFFF;
        var root = Dir(0);
        uint arc = None;
        for (uint c = root.childDir; c != None;) { var d = Dir(c); if (d.name == "arc") { arc = c; break; } c = d.sib; }
        if (arc == None) return null;
        ulong tOff = 0, tSize = 0;
        for (uint c = Dir(arc).childFile; c != None;) { var e = File_(c); if (e.name == "data.trpfd") { tOff = e.off; tSize = e.size; break; } c = e.sib; }
        if (tSize == 0 || tSize > 256 << 20) return null;
        f.Position = (long)(dataOff + tOff);
        var d0 = r.ReadBytes((int)tSize);

        // FlatBuffer root table -> vtable -> field 0 (a vector of u64)
        int rootT = BitConverter.ToInt32(d0, 0);
        int vt = rootT - BitConverter.ToInt32(d0, rootT);
        int fo0 = BitConverter.ToUInt16(d0, vt + 4);
        int vec = rootT + fo0 + BitConverter.ToInt32(d0, rootT + fo0);
        int n0 = BitConverter.ToInt32(d0, vec);
        var set = new HashSet<ulong>(n0);
        for (int i = 0; i < n0; i++) set.Add(BitConverter.ToUInt64(d0, vec + 4 + 8 * i));
        return new PackIndex(set);
    }
}
