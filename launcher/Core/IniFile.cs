using System.IO;

namespace Tandem.Core;

/// <summary>The engine's "key = value" settings file, edited in place: every other line (and the order) is kept, a
/// missing key is added at the end. The engine rewrites the whole file when it exits, so this is only written while no
/// game is running.</summary>
public sealed class IniFile
{
    private readonly string _path;
    private readonly List<string> _lines;

    public IniFile(string path)
    {
        _path = path;
        _lines = File.Exists(path) ? File.ReadAllLines(path).ToList() : new List<string>();
    }

    private int Find(string key)
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            var l = _lines[i];
            int eq = l.IndexOf('=');
            if (eq > 0 && !l.TrimStart().StartsWith('#') && l[..eq].Trim() == key) return i;
        }
        return -1;
    }

    public string? Get(string key)
    {
        int i = Find(key);
        return i < 0 ? null : _lines[i][(_lines[i].IndexOf('=') + 1)..].Trim();
    }

    public string Get(string key, string fallback) => Get(key) ?? fallback;
    public bool GetBool(string key, bool fallback = false) => Get(key) is { } v ? v != "0" : fallback;

    public void Set(string key, string value)
    {
        int i = Find(key);
        var line = $"{key} = {value}";
        if (i < 0) _lines.Add(line); else _lines[i] = line;
    }

    public void SetBool(string key, bool value) => Set(key, value ? "1" : "0");

    /// <summary>A '|'-separated list setting (mods_order, mods_off).</summary>
    public List<string> GetList(string key) =>
        (Get(key) ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();

    public void SetList(string key, IEnumerable<string> items) => Set(key, string.Join('|', items));

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var tmp = _path + ".tmp";
        File.WriteAllLines(tmp, _lines);
        File.Move(tmp, _path, overwrite: true);
    }
}
