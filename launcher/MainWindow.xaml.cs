using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Tandem.Core;
using Path = System.IO.Path;

namespace Tandem;

public partial class MainWindow : Window
{
    private Game _game = Games.Violet;
    private bool _loading = true;
    private Process? _running;

    // (label, ini value) for the choices
    private static readonly (string, string)[] FpsChoices = { ("60 fps", "1"), ("30 fps (original)", "0") };
    private static readonly (string, string)[] ResChoices =
    {
        ("1080p · 1×", "1x"), ("1620p · 1.5×", "1.5x"), ("4K · 2×", "2x"), ("6K · 3×", "3x"), ("8K · 4×", "4x"),
        ("Dynamic", "dynamic"),
    };
    private static readonly (string, string)[] ShadowChoices =
        { ("Original", "1"), ("High", "2"), ("Very high", "3"), ("Ultra", "4") };
    private static readonly (string, GameBanana.Sort)[] SortChoices =
    {
        ("Most downloaded", GameBanana.Sort.Popular), ("Most liked", GameBanana.Sort.Liked),
        ("Newest", GameBanana.Sort.Newest), ("Recently updated", GameBanana.Sort.Updated),
    };

    private readonly ObservableCollection<GbMod> _cards = new();
    private int _page = 1, _total;
    private bool _fetching;
    private GbModPage? _detail;
    private CancellationTokenSource? _browseCts;

    public MainWindow()
    {
        InitializeComponent();
        foreach (var c in new[] { QuickFps, SetFps }) c.ItemsSource = FpsChoices.Select(x => x.Item1).ToList();
        foreach (var c in new[] { QuickRes, SetRes }) c.ItemsSource = ResChoices.Select(x => x.Item1).ToList();
        SetShadows.ItemsSource = ShadowChoices.Select(x => x.Item1).ToList();
        SortBox.ItemsSource = SortChoices.Select(x => x.Item1).ToList();
        SortBox.SelectedIndex = 0;
        ModCards.ItemsSource = _cards;
        HeroArt.SizeChanged += (_, _) => DrawHero();

        var last = ReadLastGame();
        _loading = false;
        (last == "scarlet" ? SegScarlet : SegViolet).IsChecked = true;  // -> SelectGame
        if (!File.Exists(Games.EngineExe)) Status("The game engine (PokeV-Recomp.exe) isn't next to the launcher.");
    }

    // ------------------------------------------------------------------ game selection
    private static string GameFile => Path.Combine(Games.InstallDir, "game.txt");
    private static string? ReadLastGame()
    {
        try { return File.Exists(GameFile) ? File.ReadAllText(GameFile).Trim().ToLowerInvariant() : null; } catch { return null; }
    }

    private void Game_Checked(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        SelectGame(sender == SegScarlet ? Games.Scarlet : Games.Violet);
    }

    private void SelectGame(Game g)
    {
        _game = g;
        try { File.WriteAllText(GameFile, g.Slot + "\n"); } catch { }
        var accent = g == Games.Scarlet ? Color.FromRgb(0xE8, 0x55, 0x3D) : Color.FromRgb(0x8E, 0x5B, 0xE8);
        Resources["Accent"] = new SolidColorBrush(accent);
        Application.Current.Resources["Accent"] = new SolidColorBrush(accent);
        Application.Current.Resources["AccentSoft"] = new SolidColorBrush(Color.FromArgb(0x33, accent.R, accent.G, accent.B));
        ModsTitle.Text = $"Mods · {g.Name}";
        SetupTitle.Text = $"Set up {g.Name}";
        _dumps = null;
        DumpList.Text = "Choose the folder with your .nsp / .xci dumps and keys.";
        RefreshSetup();
        SettingsTitle.Text = $"Settings · {g.Name}";
        DrawHero();
        LoadSettings();
        RefreshPlay();
        RefreshMods();
        RefreshTextures();
    }

    // ------------------------------------------------------------------ navigation and window
    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (PagePlay == null) return;
        var pages = new FrameworkElement[] { PagePlay, PageMods, PageDiscover, PageTextures, PageSettings, PageSetup, PageAbout };
        var navs = new[] { NavPlay, NavMods, NavDiscover, NavTextures, NavSettings, NavSetup, NavAbout };
        if (NavSetup.IsChecked == true) RefreshSetup();
        for (int i = 0; i < pages.Length; i++) pages[i].Visibility = navs[i].IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        if (NavDiscover.IsChecked == true && _cards.Count == 0) _ = LoadCards(reset: true);
        if (NavMods.IsChecked == true) RefreshMods();
        if (NavTextures.IsChecked == true) RefreshTextures();
        if (NavPlay.IsChecked == true) RefreshPlay();
    }

    private void Min_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Max_Click(object s, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object s, RoutedEventArgs e) => Close();

    private void Status(string text) => StatusLine.Text = text;
    private void ShowProgress(double? v)
    {
        Progress.Visibility = v == null ? Visibility.Collapsed : Visibility.Visible;
        if (v != null) Progress.Value = v.Value;
    }

    private static void Open(string path)
    {
        if (!path.StartsWith("http") ) Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    // ------------------------------------------------------------------ hero art (drawn, per game)
    private void DrawHero()
    {
        HeroArt.Children.Clear();
        double w = HeroArt.ActualWidth, h = HeroArt.ActualHeight;
        if (w < 10 || h < 10) return;
        bool scarlet = _game == Games.Scarlet;
        var a = scarlet ? Color.FromRgb(0xE8, 0x55, 0x3D) : Color.FromRgb(0x8E, 0x5B, 0xE8);
        var b = scarlet ? Color.FromRgb(0xF2, 0xA6, 0x3B) : Color.FromRgb(0x4F, 0x8C, 0xF0);

        // a wash of the game's colour from the right
        HeroArt.Children.Add(new Rectangle
        {
            Width = w, Height = h,
            Fill = new LinearGradientBrush(new GradientStopCollection
            {
                new(Color.FromArgb(0, a.R, a.G, a.B), 0.35), new(Color.FromArgb(0x30, a.R, a.G, a.B), 1),
            }, new Point(0, 0.5), new Point(1, 0.5)),
        });

        double cx = w * 0.78, cy = h * 0.42, r = Math.Min(w, h) * 0.30;
        // orbit rings
        for (int i = 0; i < 4; i++)
        {
            double rr = r * (1.35 + i * 0.32);
            var ring = new Ellipse
            {
                Width = rr * 2, Height = rr * 2, StrokeThickness = 1,
                Stroke = new SolidColorBrush(Color.FromArgb((byte)(46 - i * 9), 255, 255, 255)),
            };
            Canvas.SetLeft(ring, cx - rr); Canvas.SetTop(ring, cy - rr);
            HeroArt.Children.Add(ring);
        }
        // the body: a sun disc for Scarlet, a crescent for Violet
        var disc = new EllipseGeometry(new Point(cx, cy), r, r);
        Geometry body = scarlet ? disc
            : new CombinedGeometry(GeometryCombineMode.Exclude, disc, new EllipseGeometry(new Point(cx + r * 0.42, cy - r * 0.18), r * 0.86, r * 0.86));
        HeroArt.Children.Add(new System.Windows.Shapes.Path
        {
            Data = body,
            Fill = new RadialGradientBrush(b, a) { GradientOrigin = new Point(0.3, 0.3), Center = new Point(0.4, 0.4), RadiusX = 0.8, RadiusY = 0.8 },
            Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = a, BlurRadius = 90, ShadowDepth = 0, Opacity = 0.75 },
        });
        // small satellites
        var rnd = new Random(scarlet ? 7 : 11);
        for (int i = 0; i < 26; i++)
        {
            double sz = rnd.NextDouble() * 2.6 + 1;
            var dot = new Ellipse { Width = sz, Height = sz, Fill = new SolidColorBrush(Color.FromArgb((byte)rnd.Next(60, 200), 255, 255, 255)) };
            Canvas.SetLeft(dot, w * 0.45 + rnd.NextDouble() * w * 0.55); Canvas.SetTop(dot, rnd.NextDouble() * h * 0.9);
            HeroArt.Children.Add(dot);
        }
        // fade at the bottom into the page
        HeroArt.Children.Add(new Rectangle
        {
            Width = w, Height = h * 0.35, Fill = new LinearGradientBrush(Color.FromArgb(0, 0x0F, 0x11, 0x15), (Color)FindResource("BgColor"), 90),
        });
        Canvas.SetTop(HeroArt.Children[^1], h * 0.65);
    }

    // ------------------------------------------------------------------ play
    private void RefreshPlay()
    {
        var st = _game.State();
        GameCaption.Text = $"POKÉMON {_game.Name.ToUpperInvariant()} · VERSION 4.0.0";
        GameTitle.Text = _game.Name;
        StateText.Text = _running != null ? "Playing now. The launcher comes back when you quit the game."
            : st switch
            {
                GameState.Ready => "Ready to play. Recompiled for this PC.",
                GameState.NotSetUp => "This game isn't set up in this folder yet (games\\" + _game.Slot + "\\extracted).",
                GameState.WrongVersion => "The game files here aren't version 4.0.0 of " + _game.Name + ".",
                _ => "The game code hasn't been compiled for these files yet.",
            };
        bool ready = st == GameState.Ready && File.Exists(Games.EngineExe);
        PlayButton.IsEnabled = _running == null && _setupCts == null;
        PlayLabel.Text = _running != null ? "Running" : _setupCts != null ? "Setting up…" : ready ? "Play" : "Set up";
        SegScarlet.IsEnabled = SegViolet.IsEnabled = _running == null && _setupCts == null;

        var ini = new IniFile(_game.SettingsFile);
        var mods = new ModManager(_game).List();
        Chips.Children.Clear();
        Chip(ini.GetBool("fps60") ? "60 fps" : "30 fps");
        Chip(ResChoices.FirstOrDefault(x => x.Item2 == ini.Get("resolution", "1x")).Item1 ?? ini.Get("resolution", "1x"));
        Chip(ini.GetBool("fullscreen") ? "Fullscreen" : "Windowed");
        Chip($"{mods.Count(m => m.Enabled)} mod{(mods.Count(m => m.Enabled) == 1 ? "" : "s")} on");
        int tex = TexturePacks.Count(_game.HdTexturesDir);
        if (tex > 0 && ini.GetBool("tex_hd", true)) Chip($"HD textures · {tex:N0}");
        SideStatus.Text = st == GameState.Ready ? $"{_game.Name} 4.0.0 · ready" : $"{_game.Name} · not ready";
    }

    private void Chip(string text) => Chips.Children.Add(new Border
    {
        Background = new SolidColorBrush(Color.FromArgb(0x1C, 255, 255, 255)), CornerRadius = new CornerRadius(14),
        Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(0, 0, 8, 8),
        Child = new TextBlock { Text = text, FontSize = 12.5, Foreground = (Brush)FindResource("Soft") },
    });

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        if (_running != null || _setupCts != null) return;
        if (_game.State() != GameState.Ready || !File.Exists(Games.EngineExe)) { NavSetup.IsChecked = true; return; }
        var psi = new ProcessStartInfo(Games.EngineExe, "--play")
        {
            WorkingDirectory = Games.InstallDir, UseShellExecute = false,
        };
        psi.Environment["LARECOMP_GAME"] = _game.Slot;   // the engine's data folder: games\<slot>
        psi.Environment["LARECOMP_NO_LAUNCHER"] = "1";    // "Title screen" in game quits back to here
        try { _running = Process.Start(psi); }
        catch (Exception ex) { Status("Couldn't start the game: " + ex.Message); return; }
        if (_running == null) return;
        _running.EnableRaisingEvents = true;
        var game = _game;
        _running.Exited += (_, _) => Dispatcher.Invoke(() =>
        {
            int code = -1;
            try { code = _running!.ExitCode; } catch { }
            _running = null;
            WindowState = WindowState.Normal;
            Activate();
            Status(code == 0 || code == 3 ? $"{game.Name} closed." : $"{game.Name} stopped (exit code {code}). Settings > Open latest log shows why.");
            RefreshPlay();
        });
        Status($"Starting {_game.Name}…");
        RefreshPlay();
        WindowState = WindowState.Minimized;
    }

    private void OpenGameFolder_Click(object s, RoutedEventArgs e) => Open(_game.Root);
    private void OpenSaves_Click(object s, RoutedEventArgs e) => Open(_game.SavesDir);
    private void OpenLog_Click(object s, RoutedEventArgs e)
    {
        var log = Path.Combine(_game.Root, "logs", "play.log");
        if (File.Exists(log)) Process.Start(new ProcessStartInfo(log) { UseShellExecute = true });
        else Status("No log yet: it's written while the game runs.");
    }

    // ------------------------------------------------------------------ settings
    private bool GuardRunning()
    {
        if (_running == null) return false;
        Status("Close the game first: it saves its own settings when it quits, over any change made now.");
        LoadSettings();
        return true;
    }

    private void LoadSettings()
    {
        _loading = true;
        var ini = new IniFile(_game.SettingsFile);
        int fps = ini.GetBool("fps60") ? 0 : 1;
        int res = Math.Max(0, Array.FindIndex(ResChoices, x => x.Item2 == ini.Get("resolution", "1x")));
        QuickFps.SelectedIndex = SetFps.SelectedIndex = fps;
        QuickRes.SelectedIndex = SetRes.SelectedIndex = res;
        QuickFull.IsChecked = SetFull.IsChecked = ini.GetBool("fullscreen");
        SetShowFps.IsChecked = ini.GetBool("show_fps");
        SetHdr.IsChecked = ini.GetBool("hdr");
        SetShadows.SelectedIndex = Math.Max(0, Array.FindIndex(ShadowChoices, x => x.Item2 == ini.Get("shadow_resolution", "1")));
        SetAo.IsChecked = ini.Get("ambient_occlusion", "0") != "0";
        SetGi.IsChecked = ini.Get("indirect_light", "0") != "0";
        SetAniso.IsChecked = ini.GetBool("aniso16", true);
        TexHd.IsChecked = ini.GetBool("tex_hd", true);
        TexDump.IsChecked = ini.GetBool("tex_dump");
        _loading = false;
    }

    private void Quick_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || GuardRunning()) return;
        var ini = new IniFile(_game.SettingsFile);
        if (QuickFps.SelectedIndex >= 0) ini.Set("fps60", FpsChoices[QuickFps.SelectedIndex].Item2);
        if (QuickRes.SelectedIndex >= 0) ini.Set("resolution", ResChoices[QuickRes.SelectedIndex].Item2);
        ini.SetBool("fullscreen", QuickFull.IsChecked == true);
        ini.Set("video_recommended", "done");  // (the engine's first-start suggestion would otherwise replace these)
        ini.Save();
        LoadSettings();
        RefreshPlay();
        Status($"Saved for {_game.Name}.");
    }

    private void Setting_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || GuardRunning()) return;
        var ini = new IniFile(_game.SettingsFile);
        if (SetFps.SelectedIndex >= 0) ini.Set("fps60", FpsChoices[SetFps.SelectedIndex].Item2);
        if (SetRes.SelectedIndex >= 0) ini.Set("resolution", ResChoices[SetRes.SelectedIndex].Item2);
        ini.SetBool("fullscreen", SetFull.IsChecked == true);
        ini.SetBool("show_fps", SetShowFps.IsChecked == true);
        ini.SetBool("hdr", SetHdr.IsChecked == true);
        if (SetShadows.SelectedIndex >= 0) ini.Set("shadow_resolution", ShadowChoices[SetShadows.SelectedIndex].Item2);
        ini.Set("ambient_occlusion", SetAo.IsChecked == true ? "1" : "0");
        ini.Set("indirect_light", SetGi.IsChecked == true ? "1" : "0");
        ini.SetBool("aniso16", SetAniso.IsChecked == true);
        ini.Set("video_recommended", "done");
        ini.Save();
        LoadSettings();
        RefreshPlay();
        Status($"Saved for {_game.Name}.");
    }

    // ------------------------------------------------------------------ mods
    private List<InstalledMod> _mods = new();

    private void RefreshMods()
    {
        _mods = new ModManager(_game).List();
        ModsList.ItemsSource = null;
        ModsList.ItemsSource = _mods;
        ModsEmpty.Visibility = _mods.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SaveMods()
    {
        new ModManager(_game).Save(_mods);
        RefreshMods();
        RefreshPlay();
    }

    private void ModToggle_Click(object sender, RoutedEventArgs e)
    {
        var m = (InstalledMod)((FrameworkElement)sender).Tag;
        if (GuardRunning()) { RefreshMods(); return; }
        if (!m.Enabled && _mods.Count(x => x.Enabled) >= ModManager.MaxActive)
        {
            Status($"{ModManager.MaxActive} mods are on already - the engine loads at most that many. Switch one off first.");
            RefreshMods();
            return;
        }
        m.Enabled = !m.Enabled;
        SaveMods();
    }

    private void ModMove(object sender, int by)
    {
        if (GuardRunning()) return;
        var m = (InstalledMod)((FrameworkElement)sender).Tag;
        int i = _mods.IndexOf(m), j = i + by;
        if (j < 0 || j >= _mods.Count) return;
        (_mods[i], _mods[j]) = (_mods[j], _mods[i]);
        SaveMods();
    }
    private void ModUp_Click(object s, RoutedEventArgs e) => ModMove(s, -1);
    private void ModDown_Click(object s, RoutedEventArgs e) => ModMove(s, +1);

    private void ModPage_Click(object s, RoutedEventArgs e)
    {
        var m = (InstalledMod)((FrameworkElement)s).Tag;
        if (m.Source?.PageUrl is { } u && u.StartsWith("https://gamebanana.com/")) Open(u);
        else Open(m.Path);
    }

    private void ModDelete_Click(object s, RoutedEventArgs e)
    {
        if (GuardRunning()) return;
        var m = (InstalledMod)((FrameworkElement)s).Tag;
        if (MessageBox.Show(this, $"Remove \"{m.Title}\"? Its files are deleted from the mods folder.", "Remove mod",
                            MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
        new ModManager(_game).Delete(m);
        _mods.Remove(m);
        SaveMods();
        Status($"Removed {m.Title}.");
    }

    private void OpenMods_Click(object s, RoutedEventArgs e) => Open(_game.ModsDir);

    private async void InstallFile_Click(object s, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Mod archives|*.zip;*.7z;*.rar", Multiselect = true, Title = "Install mods" };
        if (dlg.ShowDialog(this) == true) foreach (var f in dlg.FileNames) await InstallLocal(f);
    }

    private async void InstallFolder_Click(object s, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Pick a mod folder (one with a romfs folder in it)" };
        if (dlg.ShowDialog(this) == true) await InstallLocal(dlg.FolderName);
    }

    private async void Mods_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            foreach (var f in files) await InstallLocal(f);
    }

    private async Task InstallLocal(string path)
    {
        if (GuardRunning()) return;
        var meta = new ModSource { Title = Path.GetFileNameWithoutExtension(path.TrimEnd('\\')), FileName = Path.GetFileName(path) };
        await RunInstall(() => new ModManager(_game).Install(path, meta, Prog()));
    }

    private IProgress<double> Prog() => new Progress<double>(v => ShowProgress(v));

    private async Task RunInstall(Func<string> work)
    {
        Status("Installing…");
        ShowProgress(0);
        try
        {
            var msg = await Task.Run(work);
            Status(msg);
        }
        catch (Exception ex) { Status(ex.Message); MessageBox.Show(this, ex.Message, "Not installed", MessageBoxButton.OK, MessageBoxImage.Information); }
        finally { ShowProgress(null); RefreshMods(); RefreshPlay(); }
    }

    // ------------------------------------------------------------------ discover (GameBanana)
    private async Task LoadCards(bool reset)
    {
        if (_fetching) return;
        if (reset) { _browseCts?.Cancel(); _browseCts = new(); _cards.Clear(); _page = 1; _total = 0; }
        if (!reset && _total > 0 && _cards.Count >= _total) return;
        _fetching = true;
        DiscoverInfo.Text = "Loading…";
        try
        {
            var sort = SortChoices[Math.Max(0, SortBox.SelectedIndex)].Item2;
            var (mods, total) = await GameBanana.ListAsync(SearchBox.Text, sort, _page, _browseCts!.Token);
            _total = total;
            foreach (var m in mods) if (_cards.All(c => c.Id != m.Id)) _cards.Add(m);
            _page++;
            DiscoverInfo.Text = _cards.Count == 0 ? "Nothing found." : _cards.Count >= _total ? $"{_total} mods" : "";
            if (mods.Count == 0) _total = _cards.Count;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { DiscoverInfo.Text = "GameBanana couldn't be reached: " + ex.Message; }
        finally { _fetching = false; }
    }

    private void Search_Click(object s, RoutedEventArgs e) => _ = LoadCards(reset: true);
    private void Search_KeyDown(object s, KeyEventArgs e) { if (e.Key == Key.Enter) _ = LoadCards(reset: true); }
    private void Sort_Changed(object s, SelectionChangedEventArgs e) { if (IsLoaded && PageDiscover.IsVisible) _ = LoadCards(reset: true); }
    private void Discover_Scroll(object s, ScrollChangedEventArgs e)
    {
        if (DiscoverScroll.ScrollableHeight > 0 && DiscoverScroll.VerticalOffset > DiscoverScroll.ScrollableHeight - 400)
            _ = LoadCards(reset: false);
    }

    private void FindTextureMods_Click(object s, RoutedEventArgs e)
    {
        SearchBox.Text = "texture";
        NavDiscover.IsChecked = true;
        _ = LoadCards(reset: true);
    }

    private async void Card_Click(object sender, RoutedEventArgs e)
    {
        var mod = (GbMod)((FrameworkElement)sender).Tag;
        DetailPanel.Visibility = Visibility.Visible;
        DetTitle.Text = mod.Name;
        DetMeta.Text = $"by {mod.Author} · {mod.Stats}";
        DetText.Text = "Loading…";
        DetFiles.ItemsSource = null;
        DetImage.Source = mod.Thumb != null ? new BitmapImage(new Uri(mod.Thumb)) : null;
        try
        {
            _detail = await GameBanana.PageAsync(mod.Id);
            if (DetTitle.Text != mod.Name) return;  // (another card was opened meanwhile)
            DetText.Text = string.IsNullOrWhiteSpace(_detail.Text) ? "(no description)" : _detail.Text;
            DetFiles.ItemsSource = _detail.Files;
            if (_detail.Images.Count > 0) DetImage.Source = new BitmapImage(new Uri(_detail.Images[0]));
            if (_detail.Version is { Length: > 0 } v) DetMeta.Text += $" · v{v}";
        }
        catch (Exception ex) { DetText.Text = "Couldn't load the mod's page: " + ex.Message; }
    }

    private void DetailClose_Click(object s, RoutedEventArgs e) => DetailPanel.Visibility = Visibility.Collapsed;
    private void DetailOpen_Click(object s, RoutedEventArgs e) { if (_detail != null) Open(_detail.Mod.PageUrl); }

    private async void FileInstall_Click(object sender, RoutedEventArgs e)
    {
        if (_detail == null || GuardRunning()) return;
        var file = (GbFile)((FrameworkElement)sender).Tag;
        var page = _detail;
        string? tmp = null;
        try
        {
            Status($"Downloading {file.Name}…");
            ShowProgress(0);
            tmp = await GameBanana.DownloadAsync(file, Prog());
            var meta = new ModSource
            {
                Title = page.Mod.Name, Author = page.Mod.Author, GameBananaId = page.Mod.Id, PageUrl = page.Mod.PageUrl,
                FileName = file.Name, Version = page.Version,
            };
            var path = tmp;
            await RunInstall(() => new ModManager(_game).Install(path, meta, Prog()));
        }
        catch (Exception ex) { Status("Download failed: " + ex.Message); ShowProgress(null); }
        finally { try { if (tmp != null) File.Delete(tmp); } catch { } }
    }

    // ------------------------------------------------------------------ setup (dumps -> game files -> compiled code)
    private Toolchain _tc = Toolchain.Detect();
    private DumpSet? _dumps;
    private CancellationTokenSource? _setupCts;

    private void RefreshSetup()
    {
        if (_setupCts != null) return;
        string Mark(bool ok) => ok ? "✓" : "✗";
        ToolList.Text =
            $"{Mark(_tc.HasEngine)}  Engine source: {_tc.EngineSource ?? "not found (Engine source…)"}\n" +
            $"{Mark(_tc.HasCompiler)}  Compiler: MSYS2 clang + make ({Toolchain.Msys})\n" +
            $"{Mark(_tc.HasPython)}  Code generator: Python environment in the engine source\n" +
            $"{Mark(_tc.HasDotnet)}  .NET SDK (the game-file importer)";
        ToolInstall.IsEnabled = _tc.HasEngine && File.Exists(_tc.SetupScript!) && !(_tc.HasCompiler && _tc.HasPython);
        bool files = GameSetup.BuildIdOf(Path.Combine(_game.Extracted, "exefs", "main")) == _game.BuildId;
        SetupGo.IsEnabled = _tc.Ready && _dumps?.ProdKeys != null && _dumps.Packages.Count > 0 && _running == null;
        RecompileGo.IsEnabled = _tc.Ready && files && _running == null;
        if (SetupText.Text == "Not started")
            SetupStep.Text = _game.State() == GameState.Ready ? $"{_game.Name} is set up" : files ? "game files set up, code not compiled" : "not set up";
    }

    private void ToolCheck_Click(object s, RoutedEventArgs e) { _tc = Toolchain.Detect(); RefreshSetup(); }

    private void EnginePick_Click(object s, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "The engine's source folder (with Makefile, runtime and tools)" };
        if (dlg.ShowDialog(this) != true) return;
        if (!Toolchain.IsEngine(dlg.FolderName)) { Status("That isn't the engine's source folder (Makefile, runtime\\, tools\\gen.py)."); return; }
        Toolchain.SaveEngine(dlg.FolderName);
        _tc = Toolchain.Detect();
        RefreshSetup();
    }

    private void ToolInstall_Click(object s, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "This installs MSYS2 (clang, make, SDL3, FFmpeg, Vulkan headers) and Python 3.12 with winget, " +
                "and makes the code generator's Python environment in the engine source. It opens a console window and takes a " +
                "few minutes. Continue?", "Install build tools", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
        var p = Process.Start(new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{_tc.SetupScript}\"")
        {
            WorkingDirectory = _tc.EngineSource!, UseShellExecute = true,
        });
        if (p == null) return;
        p.EnableRaisingEvents = true;
        p.Exited += (_, _) => Dispatcher.Invoke(() => { _tc = Toolchain.Detect(); RefreshSetup(); Status("Build tools: checked again."); });
        Status("Installing the build tools in the console window…");
    }

    private void DumpPick_Click(object s, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = $"The folder with your {_game.Name} dumps (base game, 4.0.0 update, DLC) and prod.keys" };
        if (dlg.ShowDialog(this) != true) return;
        _dumps = DumpSet.Scan(dlg.FolderName);
        var lines = new List<string>
        {
            $"{dlg.FolderName}",
            $"{(_dumps.ProdKeys != null ? "✓" : "✗")}  prod.keys{(_dumps.ProdKeys != null ? ": " + Path.GetFileName(_dumps.ProdKeys) : " not found")}" +
            (_dumps.TitleKeys != null ? $"  ·  title keys: {Path.GetFileName(_dumps.TitleKeys)}" : ""),
            $"{(_dumps.Packages.Count > 0 ? "✓" : "✗")}  {_dumps.Packages.Count} game package(s):",
        };
        lines.AddRange(_dumps.Packages.Take(8).Select(p => "     " + Path.GetFileName(p)));
        if (_dumps.Packages.Count > 8) lines.Add($"     … and {_dumps.Packages.Count - 8} more");
        if (_dumps.Skipped.Count > 0) lines.Add($"   {_dumps.Skipped.Count} compressed .nsz / .xcz skipped (decompress them first)");
        lines.Add($"Only {_game.Name}'s packages are used; anything else in the folder is left alone.");
        DumpList.Text = string.Join("\n", lines);
        RefreshSetup();
    }

    private async void SetupGo_Click(object s, RoutedEventArgs e) => await RunSetup(import: true);
    private async void RecompileGo_Click(object s, RoutedEventArgs e) => await RunSetup(import: false);
    private void SetupCancel_Click(object s, RoutedEventArgs e) { _setupCts?.Cancel(); SetupText.Text = "Cancelling…"; }

    private async Task RunSetup(bool import)
    {
        if (_running != null || _setupCts != null) return;
        var other = Games.All.First(g => g != _game);
        _setupCts = new CancellationTokenSource();
        SetupGo.IsEnabled = RecompileGo.IsEnabled = false;
        SetupCancel.Visibility = Visibility.Visible;
        SetupLog.Clear();
        RefreshPlay();
        var log = new System.Collections.Concurrent.ConcurrentQueue<string>();
        var flush = new System.Windows.Threading.DispatcherTimer(TimeSpan.FromMilliseconds(250), System.Windows.Threading.DispatcherPriority.Background,
            (_, _) =>
            {
                if (log.IsEmpty) return;
                var sb = new System.Text.StringBuilder();
                while (log.TryDequeue(out var l)) sb.AppendLine(l);
                SetupLog.AppendText(sb.ToString());
                if (SetupLog.LineCount > 2000) SetupLog.Text = SetupLog.Text[^60000..];
                SetupLog.ScrollToEnd();
            }, Dispatcher);
        var prog = new Progress<SetupProgress>(p =>
        {
            SetupStep.Text = $"step {p.Step} of {p.Steps}";
            SetupText.Text = p.Text;
            SetupBar.IsIndeterminate = p.Fraction == null;
            if (p.Fraction != null) SetupBar.Value = p.Fraction.Value;
        });
        var game = _game;
        var otherWasReady = other.State() == GameState.Ready;
        try
        {
            var setup = new GameSetup(game, _tc, prog, l => log.Enqueue(l), _setupCts.Token);
            await Task.Run(() => setup.RunAsync(_dumps ?? new DumpSet(), import));
            var note = otherWasReady && other.State() != GameState.Ready
                ? $" The engine changed, so {other.Name} needs \"Recompile only\" too." : "";
            Status($"{game.Name} is ready to play.{note}");
            SetupText.Text = $"{game.Name} is set up and compiled for this PC.";
        }
        catch (OperationCanceledException) { SetupText.Text = "Cancelled."; Status("Setup cancelled."); }
        catch (Exception ex) { SetupText.Text = "Setup stopped"; Status(ex.Message); MessageBox.Show(this, ex.Message, "Setup stopped", MessageBoxButton.OK, MessageBoxImage.Information); }
        finally
        {
            flush.Stop();
            while (log.TryDequeue(out var rest)) SetupLog.AppendText(rest + "\n");  // (the last lines)
            SetupLog.ScrollToEnd();
            SetupBar.IsIndeterminate = false;
            _setupCts = null;
            SetupCancel.Visibility = Visibility.Collapsed;
            RefreshSetup();
            RefreshPlay();
        }
    }

    // ------------------------------------------------------------------ textures
    private void RefreshTextures()
    {
        int n = TexturePacks.Count(_game.HdTexturesDir);
        TexHdCount.Text = n == 0 ? $"No HD textures installed for {_game.Name}." : $"{n:N0} HD textures installed for {_game.Name}.";
    }

    private void TexHd_Click(object s, RoutedEventArgs e)
    {
        if (GuardRunning()) return;
        var ini = new IniFile(_game.SettingsFile);
        ini.SetBool("tex_hd", TexHd.IsChecked == true);
        ini.Save();
        RefreshPlay();
    }

    private void TexDump_Click(object s, RoutedEventArgs e)
    {
        if (GuardRunning()) return;
        var ini = new IniFile(_game.SettingsFile);
        ini.SetBool("tex_dump", TexDump.IsChecked == true);
        ini.Save();
        Status(TexDump.IsChecked == true ? "Textures are saved to textures\\dump while you play (turn it off when done)." : "Texture dumping off.");
    }

    private async void TexInstall_Click(object s, RoutedEventArgs e)
    {
        if (GuardRunning()) return;
        var dlg = new OpenFileDialog { Filter = "Texture packs|*.zip;*.7z;*.rar", Title = "Install an HD texture pack" };
        if (dlg.ShowDialog(this) != true) return;
        Status("Installing textures…");
        ShowProgress(0);
        try
        {
            var game = _game;
            int n = await Task.Run(() => TexturePacks.Install(dlg.FileName, game, Prog()));
            Status($"{n:N0} textures installed for {game.Name}.");
        }
        catch (Exception ex) { Status(ex.Message); MessageBox.Show(this, ex.Message, "Not installed", MessageBoxButton.OK, MessageBoxImage.Information); }
        finally { ShowProgress(null); RefreshTextures(); RefreshPlay(); }
    }

    private void TexOpen_Click(object s, RoutedEventArgs e) => Open(_game.HdTexturesDir);
    private void TexDumpOpen_Click(object s, RoutedEventArgs e) => Open(_game.DumpTexturesDir);
    private void TexClear_Click(object s, RoutedEventArgs e)
    {
        if (GuardRunning()) return;
        if (MessageBox.Show(this, $"Delete every HD texture installed for {_game.Name}?", "Remove textures",
                            MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
        TexturePacks.Clear(_game);
        RefreshTextures();
        RefreshPlay();
    }
}
