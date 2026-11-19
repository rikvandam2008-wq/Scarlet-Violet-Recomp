using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Tandem.Core;

/// <summary>What was found in the folder the player picked: their own dumps and keys.</summary>
public sealed class DumpSet
{
    public List<string> Packages { get; } = new();
    public string? ProdKeys { get; set; }
    public string? TitleKeys { get; set; }
    public List<string> Skipped { get; } = new();  // (compressed .nsz / .xcz: not readable)

    /// <summary>Game packages (.nsp / .xci) and keys in dir and a few levels below it. Keys are recognised by what's
    /// in them (any file name): prod.keys has header_key; title.keys is lines of 32 hex digits = 32 hex digits.</summary>
    public static DumpSet Scan(string dir)
    {
        var s = new DumpSet();
        foreach (var f in Walk(dir, 3))
        {
            var ext = Path.GetExtension(f).ToLowerInvariant();
            if (ext is ".nsp" or ".xci") s.Packages.Add(f);
            else if (ext is ".nsz" or ".xcz") s.Skipped.Add(f);
            else if (new FileInfo(f).Length < 512 * 1024 && ext is ".keys" or ".txt" or ".ini" or "")
            {
                string text;
                try { text = File.ReadAllText(f); } catch { continue; }
                if (s.ProdKeys == null && Regex.IsMatch(text, @"(?m)^\s*header_key\s*=\s*[0-9a-fA-F]{64}")) s.ProdKeys = f;
                else if (s.TitleKeys == null && Regex.IsMatch(text, @"(?m)^\s*[0-9a-fA-F]{32}\s*=\s*[0-9a-fA-F]{32}\s*$")) s.TitleKeys = f;
            }
        }
        s.Packages.Sort(StringComparer.OrdinalIgnoreCase);
        return s;
    }

    private static IEnumerable<string> Walk(string dir, int depth)
    {
        IEnumerable<string> files = Array.Empty<string>(), dirs = Array.Empty<string>();
        try { files = Directory.EnumerateFiles(dir).ToList(); dirs = Directory.EnumerateDirectories(dir).ToList(); } catch { }
        foreach (var f in files) yield return f;
        if (depth > 0) foreach (var d in dirs) foreach (var f in Walk(d, depth - 1)) yield return f;
    }
}

public sealed record SetupProgress(int Step, int Steps, string Text, double? Fraction);

/// <summary>Sets a game up from the player's own dumps and compiles its code for this PC:
///   1. import: the selected game's base game, update and DLC packages decrypted with the player's keys into
///      games\&lt;slot&gt;\extracted (exefs\, romfs.bin, aoc\) by the engine's importer;
///   2. check: the result must be the supported 4.0.0 build;
///   3. translate: the ARM64 code to C (the engine's generator, into &lt;engine&gt;\generated-&lt;slot&gt;);
///   4. compile: the C into the game library (&lt;engine&gt;\build\&lt;slot&gt;\lasgame.dll) with clang;
///   5. install: the library next to the game files, and the engine program it was built with next to the launcher.
/// Everything made from the game stays on this PC (generated-*\ and build\ are ignored by the engine's git).</summary>
public sealed class GameSetup
{
    private readonly Game _game;
    private readonly Toolchain _tc;
    private readonly IProgress<SetupProgress> _progress;
    private readonly Action<string> _log;
    private readonly CancellationToken _ct;
    private StreamWriter? _logFile;
    private const int Steps = 5;

    public GameSetup(Game game, Toolchain tc, IProgress<SetupProgress> progress, Action<string> log, CancellationToken ct)
    {
        _game = game; _tc = tc; _progress = progress; _log = log; _ct = ct;
    }

    private string Gen => Path.Combine(_tc.EngineSource!, "generated-" + _game.Slot);
    private string BuildDir => Path.Combine(_tc.EngineSource!, "build", _game.Slot);
    public static string EngineIdFile => Path.Combine(Games.InstallDir, "engine.id");

    private void Log(string line)
    {
        _logFile?.WriteLine(line);
        _log(line);
    }

    private void Report(int step, string text, double? f = null) => _progress.Report(new SetupProgress(step, Steps, text, f));

    public async Task RunAsync(DumpSet dumps, bool importFiles = true)
    {
        Directory.CreateDirectory(_game.Root);
        using var lf = new StreamWriter(Path.Combine(_game.Root, "setup.log"), append: false) { AutoFlush = true };
        _logFile = lf;
        Log($"Tandem setup of Pokémon {_game.Name}, {DateTime.Now:yyyy-MM-dd HH:mm}");
        if (!_tc.Ready) throw new InvalidOperationException("The build tools aren't all installed (Set up game > Build tools).");

        if (importFiles)
        {
            if (dumps.ProdKeys == null)
                throw new InvalidOperationException("No prod.keys was found in that folder. Put the keys dumped from your own Switch " +
                                                    "(Lockpick_RCM) next to your game dumps; any file name works.");
            if (dumps.Packages.Count == 0)
                throw new InvalidOperationException("No .nsp or .xci game dumps were found in that folder." +
                    (dumps.Skipped.Count > 0 ? " (Compressed .nsz / .xcz dumps can't be read: decompress them first.)" : ""));
            await ImportAsync(dumps);
        }
        Check();
        await TranslateAsync();
        await CompileAsync();
        Install();
        Report(Steps, $"{_game.Name} is set up and compiled for this PC.", 1);
        Log("done");
    }

    // ---- 1 + 2: import and check
    private async Task ImportAsync(DumpSet dumps)
    {
        if (!File.Exists(_tc.Importer!))
        {
            Report(1, "Building the game-file importer…");
            await RunAsync(Toolchain.FindOnPath("dotnet.exe")!, $"build -c Release -o \"{Path.GetDirectoryName(_tc.Importer)}\"",
                           _tc.ImporterProject!, null);
        }
        var tmp = _game.Extracted + ".new";
        if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
        var args = new StringBuilder();
        if (dumps.TitleKeys != null) args.Append($"--title-keys \"{dumps.TitleKeys}\" ");
        args.Append($"--title {_game.TitleId} \"{dumps.ProdKeys}\" \"{tmp}\"");
        foreach (var p in dumps.Packages) args.Append($" \"{p}\"");
        Report(1, $"Reading {_game.Name} from your dumps…", 0);
        string? last = null;
        int code = await RunAsync(_tc.Importer!, args.ToString(), _tc.EngineSource!, line =>
        {
            var m = Regex.Match(line, @"^progress (\d+) (\d+)");
            if (m.Success) { Report(1, $"Reading {_game.Name} from your dumps…", double.Parse(m.Groups[1].Value) / Math.Max(1, double.Parse(m.Groups[2].Value))); return false; }
            last = line;
            return true;
        }, throwOnFail: false);
        if (code != 0)
        {
            if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
            throw new InvalidOperationException(last?.StartsWith("error: ") == true ? Cap(last[7..]) : "The import failed (see setup.log).");
        }
        var build = BuildIdOf(Path.Combine(tmp, "exefs", "main"));
        if (build != _game.BuildId)
        {
            Directory.Delete(tmp, true);
            throw new InvalidOperationException($"These dumps make {_game.Name} build {build ?? "?"}, not version 4.0.0 " +
                $"({_game.BuildId}). Add your dump of the game's 4.0.0 update to the folder.");
        }
        // swap in: the old files (and their compiled library) go
        if (Directory.Exists(_game.Extracted)) Directory.Delete(_game.Extracted, true);
        Directory.Move(tmp, _game.Extracted);
        int dlc = Directory.Exists(Path.Combine(_game.Extracted, "aoc")) ? Directory.GetFiles(Path.Combine(_game.Extracted, "aoc"), "*.bin").Length : 0;
        Log($"imported: {_game.Name} 4.0.0, {dlc} DLC");
    }

    private void Check()
    {
        Report(2, "Checking the game files…");
        if (BuildIdOf(Path.Combine(_game.Extracted, "exefs", "main")) != _game.BuildId ||
            !File.Exists(Path.Combine(_game.Extracted, "romfs.bin")))
            throw new InvalidOperationException($"{_game.Name}'s game files aren't set up: choose the folder with your dumps.");
    }

    public static string? BuildIdOf(string main)
    {
        if (!File.Exists(main)) return null;
        using var f = File.OpenRead(main);
        if (f.Length < 0x48) return null;
        f.Position = 0x40;
        var id = new byte[8];
        f.ReadExactly(id);
        return Convert.ToHexString(id).ToLowerInvariant();
    }

    // ---- 3: translate (tools/gen.py, as the engine's Makefile `gen` target runs it, with absolute paths)
    private async Task TranslateAsync()
    {
        var exefs = Path.Combine(_game.Extracted, "exefs");
        var hle = Path.Combine(_tc.EngineSource!, "runtime", "hle", "hle_symbols.txt");
        var mods = new List<(string name, string baseAddr, bool hleSyms)> { ("sdk", "0x7008000000", true), ("main", "0x7000000000", false) };
        if (File.Exists(Path.Combine(exefs, "subsdk0"))) mods.Add(("subsdk0", "0x700a000000", true));
        for (int i = 0; i < mods.Count; i++)
        {
            var (name, addr, hs) = mods[i];
            var a = $"gen.py {name} \"{Path.Combine(exefs, name)}\" {addr} \"{Path.Combine(Gen, name)}\" --simd c" + (hs ? $" --hle \"{hle}\"" : "");
            int k = i;
            Report(3, $"Translating the game's code to C ({name})…", (double)k / mods.Count);
            await RunAsync(_tc.Python!, a, Path.Combine(_tc.EngineSource!, "tools"), line =>
            {
                var m = Regex.Match(line, @"progress (\d+)/(\d+)");
                if (m.Success)
                {
                    Report(3, $"Translating the game's code to C ({name})…",
                           (k + double.Parse(m.Groups[1].Value) / Math.Max(1, double.Parse(m.Groups[2].Value))) / mods.Count);
                    return false;
                }
                return true;
            }, env: new() { ["PYTHONUTF8"] = "1" });
        }
    }

    // ---- 4: compile (the engine's Makefile: the program and the game library)
    private async Task CompileAsync()
    {
        int total = Directory.EnumerateFiles(Gen, "*.c", SearchOption.AllDirectories).Count();
        var tmp = Path.Combine(_tc.EngineSource!, "build", "tmp");
        Directory.CreateDirectory(tmp);
        var objDir = Path.Combine(BuildDir, "obj");
        int jobs = Math.Max(1, Environment.ProcessorCount);
        var t = tmp.Replace('\\', '/');
        var args = $"-j{jobs} B=build/{_game.Slot} GEN=generated-{_game.Slot} GENARCH=-march=x86-64-v3 " +
                   $"\"GENOPT=-O1 -DGUEST_VOLATILE\" TMP={t} TEMP={t}";
        Report(4, $"Compiling for this PC ({jobs} threads, about 10-20 minutes)…", 0);
        DateTime start = DateTime.Now;
        using var poll = new Timer(_ =>
        {
            int done = Directory.Exists(objDir) ? Directory.EnumerateFiles(objDir, "*.o", SearchOption.AllDirectories)
                                                           .Count(f => File.GetLastWriteTime(f) >= start.AddSeconds(-2)) : 0;
            Report(4, $"Compiling for this PC ({jobs} threads, about 10-20 minutes)…", Math.Min(0.99, (double)done / Math.Max(1, total)));
        }, null, 2000, 2000);
        await RunAsync(Toolchain.Make, args, _tc.EngineSource!, line => !line.StartsWith("clang ") || line.Contains("error"),
                       env: new() { ["PATH"] = $@"{Toolchain.Msys}\ucrt64\bin;{Toolchain.Msys}\usr\bin;" + Environment.GetEnvironmentVariable("PATH") });
    }

    // ---- 5: install
    private void Install()
    {
        Report(5, "Installing…");
        var lib = Path.Combine(BuildDir, "lasgame.dll");
        if (!File.Exists(lib)) throw new InvalidOperationException("The build finished without a game library (see setup.log).");
        File.Copy(lib, Path.Combine(_game.Extracted, "lasgame.dll"), true);
        File.Copy(lib + ".id", Path.Combine(_game.Extracted, "lasgame.dll.id"), true);
        // the engine program it was built with: a library only loads into the engine built from the same source
        var exe = Path.Combine(BuildDir, "PokeV-Recomp.exe");
        if (File.Exists(exe)) File.Copy(exe, Games.EngineExe, true);
        var id = File.ReadAllText(lib + ".id").Trim();
        File.WriteAllText(EngineIdFile, id.Split('-')[0] + "\n");
        Log($"installed {id}");
    }

    private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..] + (s.EndsWith('.') ? "" : ".");

    /// <summary>Runs a tool, its output lines to the log (filter: false = not logged), cancelled with the setup.</summary>
    private async Task<int> RunAsync(string exe, string args, string cwd, Func<string, bool>? filter,
                                     Dictionary<string, string>? env = null, bool throwOnFail = true)
    {
        Log($"> {Path.GetFileName(exe)} {args}");
        var psi = new ProcessStartInfo(exe, args)
        {
            WorkingDirectory = cwd, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
        };
        if (env != null) foreach (var (k, v) in env) psi.Environment[k] = v;
        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"{exe} didn't start");
        void Line(string? l) { if (l != null && (filter == null || filter(l))) Log(l); }
        p.OutputDataReceived += (_, e) => Line(e.Data);
        p.ErrorDataReceived += (_, e) => Line(e.Data);
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        using var reg = _ct.Register(() => { try { p.Kill(entireProcessTree: true); } catch { } });
        await p.WaitForExitAsync();
        _ct.ThrowIfCancellationRequested();
        if (p.ExitCode != 0 && throwOnFail)
            throw new InvalidOperationException($"{Path.GetFileName(exe)} failed (exit code {p.ExitCode}); setup.log has the details.");
        return p.ExitCode;
    }
}
