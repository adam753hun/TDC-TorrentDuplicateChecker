using System.Text;
using System.Text.Json;

namespace Tdc;

// Egyszerű, hálózat és torrentkliens nélküli diagnosztika. Nem igényel tesztkeretrendszert.
internal static class Diagnostics
{
    public static int Run(string resultPath)
    {
        resultPath = Path.GetFullPath(resultPath);
        string workspace = Path.Combine(Path.GetDirectoryName(resultPath)!, "tdc-cs-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        var checks = new List<string>();
        void Check(bool condition, string name) { if (!condition) throw new Exception("Sikertelen teszt: " + name); checks.Add(name); }
        try
        {
            L.SetLanguage("hu");
            string media = Path.Combine(workspace, "media"); Directory.CreateDirectory(media);
            File.WriteAllText(Path.Combine(media, "Film.2020.1080p.mkv"), "abc");
            File.WriteAllText(Path.Combine(media, "note.txt"), "ignored");
            var db = new Database(Path.Combine(workspace, "data", "index.sqlite3")); db.Initialize();
            Check(db.Roots().Count == 0, "Üres adatbázis");
            Check(db.Scan(media, CancellationToken.None) == 1, "Csak videó-metaadatok beolvasása");
            Check(db.Roots().Single().Videos == 1 && db.Files().Single().Size == 3, "SQLite méret és összesítés");
            var exact = Matching.Compare(db.Files(), [new("Film.2020.1080p.mkv", 3, "Film.2020.1080p.mkv")], CancellationToken.None);
            Check(exact.Exact == 1 && exact.Total == 1, "Pontos név+méret");
            var similar = Matching.Compare(db.Files(), [new("Film.2020.720p.mkv", 9, "Film.2020.720p.mkv")], CancellationToken.None);
            Check(similar.Similar == 1 && similar.Rows[0].Reason.Contains("eltérő felbontás"), "Hasonló cím és találati indok");
            Check(Matching.Compare(db.Files(), [new("Film.2021.mkv", 3, "Film.2021.mkv")], CancellationToken.None).Missing == 1, "Eltérő évszám kizárása");
            Check(Matching.Parse("Some.Show.S01E02.1080p.mkv").Episode == Matching.Parse("Some.Show.1x02.720p.mkv").Episode, "Epizódformátumok");
            Check(Matching.Parse("Some.Show.S01E03.mkv").Episode != Matching.Parse("Some.Show.S01E02.mkv").Episode, "Eltérő epizód");
            Check(Matching.Parse("1917.1080p.mkv").Title == "1917", "Számból álló filmcím");
            Check(Matching.Score("example film", "example film") == 1 && Matching.Score("abc", "xyz") < .85, "Hasonlósági számítás");
            var multi = Matching.Compare([.. db.Files(), db.Files()[0] with { Path = "other" }], [new("Film.2020.1080p.mkv", 3, "a")], CancellationToken.None);
            Check(multi.Exact == 1 && multi.Rows.Count == 2, "Összesítés videónként, nem találatonként");
            var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            bool cancelledOk = false;
            try { db.Scan(media, cancelled.Token); } catch (OperationCanceledException) { cancelledOk = true; }
            Check(cancelledOk && db.Files().Count == 1, "Megszakítás megőrzi az adatokat");
            string dot = Database.NativePath(Path.Combine(media, "folder.")); Directory.CreateDirectory(dot);
            File.WriteAllText(Path.Combine(dot, "second.mp4"), "x");
            Check(db.Scan(media, CancellationToken.None) == 2, "Ponttal végződő Windows-mappa");
            string exported = Path.Combine(workspace, "export.sqlite3"); db.Export(exported);
            File.WriteAllText(Path.Combine(media, "third.avi"), "y"); db.Scan(media, CancellationToken.None);
            string backup = db.Import(exported);
            Check(db.Files().Count == 2 && new Database(backup).Files().Count == 3, "Export/import és biztonsági mentés");
            string invalid = Path.Combine(workspace, "invalid.db"); File.WriteAllText(invalid, "broken");
            bool rejected = false;
            try { db.Import(invalid); } catch { rejected = true; }
            Check(rejected && db.Files().Count == 2, "Hibás import nem módosít");
            Check(db.Files("second").Count == 1, "Nyilvántartás keresése");
            db.Remove(media); Check(db.Roots().Count == 0 && File.Exists(Path.Combine(media, "third.avi")), "Eltávolítás nem töröl videófájlt");

            string torrent = Path.Combine(workspace, "test.torrent");
            File.WriteAllBytes(torrent, Encode(new Dictionary<string, object> { ["info"] = new Dictionary<string, object> { ["name"] = "Film.mkv", ["length"] = 3L } }));
            Check(TorrentReader.Read(torrent).Single().Size == 3, "Egyfájlos v1 torrent");
            File.WriteAllBytes(torrent, Encode(new Dictionary<string, object> { ["info"] = new Dictionary<string, object> { ["name"] = "Pack", ["files"] = new List<object> { new Dictionary<string, object> { ["path"] = new List<object> { "sub", "film.mkv" }, ["length"] = 7L } } } }));
            Check(TorrentReader.Read(torrent).Single().RelativePath == "sub/film.mkv", "Többfájlos torrent");
            foreach (string value in new[] { "", "d", "3:ab", "i-0e", "lee" })
            {
                File.WriteAllText(torrent, value); bool malformed = false;
                try { TorrentReader.Read(torrent); } catch (InvalidDataException) { malformed = true; }
                Check(malformed, "Hibás torrent: " + value);
            }
            File.WriteAllBytes(torrent, Encode(new Dictionary<string, object> { ["info"] = new Dictionary<string, object> { ["meta version"] = 2L } }));
            rejected = false; try { TorrentReader.Read(torrent); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "V2 torrent visszautasítása");
            Check(!MainForm.CanForward(exact, 10) && !MainForm.CanForward(similar, 10), "Egyezésnél nincs automatikus átadás");
            var clean = new Report([], 1, 0, 0, 1);
            Check(MainForm.CanForward(clean, 1) && !MainForm.CanForward(clean, 0) && !MainForm.CanForward(new([], 0, 0, 0, 0), 1), "Automatikus átadás biztonsági feltételei");
            string settingsPath = Path.Combine(workspace, "settings.json");
            Check(!Settings.Load(settingsPath).Automatic, "Beállítások alapértékei");
            File.WriteAllText(settingsPath, "{\"automatic\":false,\"close_after_forward\":true,\"client\":\"\"}");
            Check(Settings.Load(settingsPath).CloseAfterForward, "Mentett JSON-beállítások");
            Settings.Load(settingsPath).Save(settingsPath); Check(Settings.Load(settingsPath).CloseAfterForward, "Beállítások mentése");
            Check(Settings.Load(settingsPath).Language == "hu", "Régi JSON alapból magyar");
            var bilingualSettings = Settings.Load(settingsPath); bilingualSettings.Language = "en"; bilingualSettings.Save(settingsPath);
            Check(Settings.Load(settingsPath).Language == "en", "Angol nyelv mentése és visszaolvasása");
            L.SetLanguage("en");
            Check(L.T("Nyilvántartás") == "Library" && L.T("Beállítások") == "Settings", "Angol alapszövegek");
            Check(L.F($"Beolvasott videók: {12}") == "Videos scanned: 12", "Interpolált állapot angolul");
            Check(L.Reason(similar.Rows[0].Reason).Contains("different resolution"), "Találati indok angolul");
            Check(L.T(Matching.Exact) == "EXACT NAME + SIZE" && exact.Rows[0].Category == Matching.Exact, "Kategóriák logikája nyelvfüggetlen");
            L.SetLanguage("unknown"); Check(L.Language == "hu", "Ismeretlen nyelv magyarra esik vissza");
            Check(WindowsIntegration.OpenCommand("C:\\Some path\\TDC.exe").EndsWith("\"%1\""), "Társítási argumentum idézőjelezése");

            // Ha van mellékelt kompatibilitási adatbázis, annak importálását is ellenőrizzük.
            string fixture = Path.Combine(Path.GetDirectoryName(resultPath)!, "compatibility-fixture.sqlite3");
            if (File.Exists(fixture)) { db.Import(fixture); Check(db.Files().Single().Name == "sample.2026.mkv", "SQLite kompatibilitási import"); db.Export(Path.Combine(Path.GetDirectoryName(resultPath)!, "csharp-export.sqlite3")); }
            using var form = new MainForm(Path.Combine(workspace, "gui-data")) { ShowInTaskbar = false, Opacity = 0 };
            form.Show(); Application.DoEvents();
            using (var picture = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(picture, new Rectangle(Point.Empty, form.Size));
                picture.Save(Path.ChangeExtension(resultPath, ".png"), System.Drawing.Imaging.ImageFormat.Png);
            }
            Check(form.ActiveDatabase.StartsWith(workspace), "Adatok a saját data mappában");
            foreach (Size size in new[] { new Size(1180, 790), new Size(940, 660) })
            {
                form.Size = size;
                foreach (var tab in Find<TabControl>(form).Single().TabPages.Cast<TabPage>())
                {
                    Find<TabControl>(form).Single().SelectedTab = tab; Application.DoEvents();
                    Check(form.FooterVisible, "Alsó állapotsor: " + tab.Text + " " + size);
                }
            }
            // Valódi vezérlőeseményen keresztül próbáljuk az oda-vissza nyelvcserét.
            var languagePicker = Find<ComboBox>(form).Single(box => box.Items.Contains("English"));
            typeof(MainForm).GetField("report", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(form, new Report([exact.Rows[0], similar.Rows[0], new(Matching.Missing, "missing.mkv", "", "", "A jelenlegi nyilvántartásban nincs találat.")], 3, 1, 1, 1));
            languagePicker.SelectedIndex = 1; Application.DoEvents();
            var tabControl = Find<TabControl>(form).Single();
            Check(tabControl.TabPages[1].Text == "Library" && tabControl.TabPages[2].Text == "Settings" && tabControl.TabPages[3].Text == "About", "Mind a négy GUI-fül angolul");
            var resultGrid = Find<DataGridView>(form).Single(grid => grid.Columns.Contains("Category"));
            Check(resultGrid.Columns["Reason"]!.HeaderText == "Match reason", "Oszlopfejlécek angolul");
            Check(((List<MatchRow>)resultGrid.DataSource!).Any(row => row.Category == "SIMILAR TITLE" && row.Reason.Contains("different resolution")), "Meglévő eredmények újrafordítása");
            var filterPicker = Find<ComboBox>(form).Single(box => box.Items.Contains("EXACT NAME + SIZE"));
            filterPicker.SelectedIndex = 2; Application.DoEvents();
            Check(((List<MatchRow>)resultGrid.DataSource!).Count == 1 && ((List<MatchRow>)resultGrid.DataSource!)[0].Category == "SIMILAR TITLE", "Angol kategóriaszűrés");
            using (var picture = new Bitmap(form.Width, form.Height))
            {
                tabControl.SelectedIndex = 2; Application.DoEvents();
                form.DrawToBitmap(picture, new Rectangle(Point.Empty, form.Size));
                picture.Save(Path.ChangeExtension(resultPath, ".english.png"), System.Drawing.Imaging.ImageFormat.Png);
            }
            languagePicker.SelectedIndex = 0; Application.DoEvents();
            Check(tabControl.TabPages[1].Text == "Nyilvántartás" && filterPicker.SelectedIndex == 2 && ((List<MatchRow>)resultGrid.DataSource!).Count == 1, "Magyar visszaváltás megtartja a szűrőt és az eredményt");
            L.SetLanguage("en");
            using (var englishStartup = new MainForm(Path.GetDirectoryName(settingsPath)!) { ShowInTaskbar = false, Opacity = 0 })
            {
                englishStartup.Show(); Application.DoEvents();
                Check(Find<TabControl>(englishStartup).Single().TabPages[2].Text == "Settings", "Mentett angol nyelv indításkor");
                englishStartup.Close();
            }
            L.SetLanguage("hu");
            form.Close();
            File.WriteAllText(resultPath, JsonSerializer.Serialize(new { ok = true, version = Release.Version, runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, count = checks.Count, checks, workspace }, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch (Exception ex)
        {
            File.WriteAllText(resultPath, JsonSerializer.Serialize(new { ok = false, checks, error = ex.ToString(), workspace }, new JsonSerializerOptions { WriteIndented = true }));
            return 1;
        }
    }

    private static IEnumerable<T> Find<T>(Control control) where T : Control
    {
        foreach (Control child in control.Controls) { if (child is T match) yield return match; foreach (var nested in Find<T>(child)) yield return nested; }
    }

    // Kis teszttorrentek létrehozása; nem a felhasználó fájljait használja.
    private static byte[] Encode(object value)
    {
        if (value is long n) return Encoding.ASCII.GetBytes("i" + n + "e");
        if (value is string text) { byte[] bytes = Encoding.UTF8.GetBytes(text); return [.. Encoding.ASCII.GetBytes(bytes.Length + ":"), .. bytes]; }
        if (value is List<object> list) return [(byte)'l', .. list.SelectMany(Encode), (byte)'e'];
        if (value is Dictionary<string, object> dictionary) return [(byte)'d', .. dictionary.OrderBy(p => p.Key, StringComparer.Ordinal).SelectMany(p => Encode(p.Key).Concat(Encode(p.Value))), (byte)'e'];
        throw new ArgumentException("Nem támogatott tesztérték.");
    }
}
