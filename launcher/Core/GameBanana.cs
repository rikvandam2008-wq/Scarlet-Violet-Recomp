using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Tandem.Core;

public sealed class GbMod
{
    public long Id { get; init; }
    public string Name { get; init; } = "";
    public string Author { get; init; } = "";
    public string? Thumb { get; init; }
    public string? Category { get; init; }
    public long Downloads { get; init; }
    public long Likes { get; init; }
    public long Views { get; init; }
    public DateTime Updated { get; init; }
    public string PageUrl => $"https://gamebanana.com/mods/{Id}";
    public string Stats => $"♥ {Likes:N0}   ◉ {Views:N0} views   {Updated:d MMM yyyy}";
}

public sealed class GbFile
{
    public long Id { get; init; }
    public string Name { get; init; } = "";
    public long Size { get; init; }
    public string Description { get; init; } = "";
    public string DownloadUrl { get; init; } = "";
    public bool Scanned { get; init; }        // GameBanana's virus scan finished and found nothing
    public string ScanText { get; init; } = "";
    public bool Archive => ModArchive.IsArchive(Name);
    public bool Installable => Scanned && Archive;
    public string SizeText => Size >= 1048576 ? $"{Size / 1048576.0:0.0} MB" : $"{Size / 1024.0:0} KB";
}

public sealed class GbModPage
{
    public required GbMod Mod { get; init; }
    public string Text { get; init; } = "";
    public string? Version { get; init; }
    public List<GbFile> Files { get; init; } = new();
    public List<string> Images { get; init; } = new();
}

/// <summary>GameBanana's public API (apiv11), for Pokémon Scarlet &amp; Violet (game 17220). Only reads public mod
/// listings and downloads from gamebanana.com; no account, nothing about the player is sent.</summary>
public static class GameBanana
{
    public const int GameId = 17220;
    private static readonly HttpClient Http = new(new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.All, AllowAutoRedirect = true,
    }) { Timeout = TimeSpan.FromMinutes(10) };

    static GameBanana() => Http.DefaultRequestHeaders.UserAgent.ParseAdd("TandemLauncher/1.0");

    public enum Sort { Popular, Newest, Updated, Liked }

    public static async Task<(List<GbMod> mods, int total)> ListAsync(string? search, Sort sort, int page,
                                                                     CancellationToken ct = default)
    {
        string url;
        if (!string.IsNullOrWhiteSpace(search))
            url = "https://gamebanana.com/apiv11/Util/Search/Results?_sModelName=Mod&_sOrder=best_match" +
                  $"&_idGameRow={GameId}&_sSearchString={Uri.EscapeDataString(search.Trim())}&_nPage={page}";
        else
        {
            var s = sort switch
            {
                Sort.Newest => "Generic_Newest", Sort.Updated => "Generic_LatestUpdated",
                Sort.Liked => "Generic_MostLiked", _ => "Generic_MostDownloaded",
            };
            url = $"https://gamebanana.com/apiv11/Mod/Index?_nPage={page}&_nPerpage=24" +
                  $"&_aFilters%5BGeneric_Game%5D={GameId}&_sSort={s}";
        }
        using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));
        var root = doc.RootElement;
        int total = root.TryGetProperty("_aMetadata", out var md) && md.TryGetProperty("_nRecordCount", out var rc)
            ? rc.GetInt32() : 0;
        var list = new List<GbMod>();
        if (root.TryGetProperty("_aRecords", out var recs))
            foreach (var r in recs.EnumerateArray())
            {
                if (Str(r, "_sModelName") is { } model && model != "Mod") continue;
                list.Add(ParseMod(r));
            }
        return (list, total);
    }

    public static async Task<GbModPage> PageAsync(long id, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(await Http.GetStringAsync($"https://gamebanana.com/apiv11/Mod/{id}/ProfilePage", ct));
        var r = doc.RootElement;
        var files = new List<GbFile>();
        if (r.TryGetProperty("_aFiles", out var fs))
            foreach (var f in fs.EnumerateArray())
            {
                var av = Str(f, "_sAvState") ?? "";
                var res = Str(f, "_sAnalysisResult") ?? "";
                var dl = Str(f, "_sDownloadUrl") ?? "";
                files.Add(new GbFile
                {
                    Id = Num(f, "_idRow"), Name = Str(f, "_sFile") ?? "file", Size = Num(f, "_nFilesize"),
                    Description = Str(f, "_sDescription") ?? "",
                    // only GameBanana's own download links, and only files its scan cleared
                    DownloadUrl = dl.StartsWith("https://gamebanana.com/dl/", StringComparison.Ordinal) ? dl : "",
                    Scanned = av == "done" && res.Equals("ok", StringComparison.OrdinalIgnoreCase) &&
                              dl.StartsWith("https://gamebanana.com/dl/", StringComparison.Ordinal),
                    ScanText = av == "done" ? (res.Equals("ok", StringComparison.OrdinalIgnoreCase) ? "Scanned: clean" : $"Scan: {res}")
                                            : "Not scanned yet",
                });
            }
        return new GbModPage
        {
            Mod = ParseMod(r), Text = HtmlToText(Str(r, "_sText") ?? ""), Version = Str(r, "_sVersion"),
            Files = files, Images = Images(r).ToList(),
        };
    }

    /// <summary>Downloads a file to a temporary path (only gamebanana.com links, see PageAsync).</summary>
    public static async Task<string> DownloadAsync(GbFile file, IProgress<double>? progress, CancellationToken ct = default)
    {
        if (!file.Installable || file.DownloadUrl.Length == 0) throw new InvalidOperationException("This file can't be installed.");
        var path = Path.Combine(Path.GetTempPath(), $"tandem-dl-{file.Id}{Path.GetExtension(file.Name)}");
        using var resp = await Http.GetAsync(file.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        var host = resp.RequestMessage?.RequestUri?.Host ?? "";
        if (!host.EndsWith("gamebanana.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"The download was redirected away from GameBanana ({host}): stopped.");
        long total = resp.Content.Headers.ContentLength ?? file.Size, done = 0;
        await using (var src = await resp.Content.ReadAsStreamAsync(ct))
        await using (var dst = File.Create(path))
        {
            var buf = new byte[1 << 16];
            int n;
            while ((n = await src.ReadAsync(buf, ct)) > 0)
            {
                await dst.WriteAsync(buf.AsMemory(0, n), ct);
                done += n;
                if (total > 0) progress?.Report((double)done / total);
            }
        }
        return path;
    }

    private static GbMod ParseMod(JsonElement r)
    {
        string? thumb = null;
        foreach (var img in Images(r, small: true)) { thumb = img; break; }
        long ts = Num(r, "_tsDateUpdated") is var u && u > 0 ? u : Num(r, "_tsDateModified") is var m && m > 0 ? m : Num(r, "_tsDateAdded");
        return new GbMod
        {
            Id = Num(r, "_idRow"), Name = WebUtility.HtmlDecode(Str(r, "_sName") ?? ""),
            Author = r.TryGetProperty("_aSubmitter", out var s) ? Str(s, "_sName") ?? "" : "",
            Category = r.TryGetProperty("_aRootCategory", out var c) || r.TryGetProperty("_aCategory", out c) ? Str(c, "_sName") : null,
            Downloads = Num(r, "_nDownloadCount"), Likes = Num(r, "_nLikeCount"), Views = Num(r, "_nViewCount"), Thumb = thumb,
            Updated = DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime,
        };
    }

    private static IEnumerable<string> Images(JsonElement r, bool small = false)
    {
        if (!r.TryGetProperty("_aPreviewMedia", out var pm) || pm.ValueKind != JsonValueKind.Object ||
            !pm.TryGetProperty("_aImages", out var imgs)) yield break;
        foreach (var i in imgs.EnumerateArray())
        {
            var b = Str(i, "_sBaseUrl");
            var f = small ? Str(i, "_sFile530") ?? Str(i, "_sFile220") ?? Str(i, "_sFile") : Str(i, "_sFile");
            if (b != null && f != null && b.StartsWith("https://images.gamebanana.com/", StringComparison.Ordinal))
                yield return $"{b}/{f}";
        }
    }

    private static string? Str(JsonElement e, string k) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static long Num(JsonElement e, string k) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number &&
        v.TryGetInt64(out var n) ? n : 0;

    private static string HtmlToText(string html)
    {
        var s = Regex.Replace(html, @"<(br|/p|/li|/h\d|/div)\s*/?>", "\n", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"<li[^>]*>", "• ", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, "<[^>]+>", "");
        s = WebUtility.HtmlDecode(s);
        s = Regex.Replace(s, @"\n{3,}", "\n\n");
        return s.Trim();
    }
}
