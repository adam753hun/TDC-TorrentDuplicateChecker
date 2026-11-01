namespace Tdc;

// A felület építése külön metódusokban van, hogy könnyen módosítható legyen.
// A Task.Run csak a számításokat és I/O-t végzi; az await után a GUI-szál folytatódik.
internal sealed class MainForm : Form
{
    private readonly Database database;
    private readonly string settingsPath;
    private Settings settings;
    private readonly List<Button> actionButtons = [];
    private readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    private readonly Label registryStatus = TextLabel(L.T("Nyilvántartás betöltése…"), bold: true);
    private readonly Label status = TextLabel(L.T("Kész."));
    private readonly Label summary = TextLabel(L.T("Még nincs ellenőrzött torrent."), bold: true);
    private readonly DataGridView results = Grid();
    private readonly DataGridView roots = Grid();
    private readonly DataGridView searchResults = Grid();
    private readonly ComboBox filter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly ComboBox language = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly TextBox search = new() { Width = 350, PlaceholderText = L.T("Keresés a mentett elérési utakban…") };
    private readonly Label searchNote = TextLabel("");
    private readonly TextBox client = new() { Width = 620, Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly CheckBox automatic = new() { Text = L.T("Automatikus továbbítás, ha nincs pontos vagy hasonló találat"), AutoSize = true };
    private readonly CheckBox closeAfter = new() { Text = L.T("TDC bezárása a torrent átadása után"), AutoSize = true };
    private CancellationTokenSource? cancellation;
    private bool busy, forwarded;
    private string? currentTorrent;
    private Report? report;
    internal string ActiveDatabase => database.PathName;
    internal bool FooterVisible => status.Visible && status.Bottom <= status.Parent!.ClientSize.Height;

    public MainForm(string dataDirectory, string? startupTorrent = null)
    {
        Text = $"TDC — Torrent Duplicate Checker · C# v{Release.Version}";
        Size = new Size(1180, 790);
        MinimumSize = new Size(940, 660);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(246, 248, 251);
        Directory.CreateDirectory(dataDirectory);
        database = new Database(Path.Combine(dataDirectory, "index.sqlite3"));
        database.Initialize();
        settingsPath = Path.Combine(dataDirectory, "settings.json");
        string? settingsError = null;
        try { settings = Settings.Load(settingsPath); }
        catch (Exception ex) { settings = new(); settingsError = L.T("Nem olvasható beállítások; automatizálás kikapcsolva. ") + ex.Message; }
        L.SetLanguage(settings.Language);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(10) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(tabs, 0, 0);
        registryStatus.Margin = new Padding(8, 8, 8, 2);
        status.Margin = new Padding(8, 2, 8, 4);
        layout.Controls.Add(registryStatus, 0, 1);
        layout.Controls.Add(status, 0, 2);
        layout.SizeChanged += (_, _) => { registryStatus.MaximumSize = new Size(Math.Max(200, layout.ClientSize.Width - 40), 0); status.MaximumSize = registryStatus.MaximumSize; };
        Controls.Add(layout);
        BuildCheck(); BuildRegistry(); BuildSettings(); BuildAbout();
        ApplyLanguage();
        ReloadRoots();
        status.Text = settingsError ?? L.T("Kész. Adatbázis: ") + database.PathName;
        FormClosing += (_, e) =>
        {
            if (!busy) return;
            e.Cancel = true;
            cancellation?.Cancel();
            status.Text = L.T("Megszakítás kérése elküldve. A művelet lezárása után zárd be újra az ablakot.");
        };
        Shown += async (_, _) => { if (!string.IsNullOrWhiteSpace(startupTorrent)) await CheckPath(startupTorrent); };
    }

    private static Label TextLabel(string text, bool bold = false) => new() { Text = text, AutoSize = true, Margin = new Padding(6), Font = new Font("Segoe UI", bold ? 10 : 9.5f, bold ? FontStyle.Bold : FontStyle.Regular) };
    private static DataGridView Grid() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AutoGenerateColumns = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle,
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None, RowTemplate = { Height = 30 },
        EnableHeadersVisualStyles = false, ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(230, 237, 247), ForeColor = Color.FromArgb(25, 45, 75), Font = new Font("Segoe UI", 9, FontStyle.Bold) }
    };
    private static void Column(DataGridView grid, string name, string header, int width) => grid.Columns.Add(new DataGridViewTextBoxColumn { Name = name, DataPropertyName = name, HeaderText = header, Width = width, SortMode = DataGridViewColumnSortMode.NotSortable });

    private Button ActionButton(string text, Func<Task> action)
    {
        var button = new Button { Text = text, AutoSize = true, Padding = new Padding(8, 4, 8, 4), Margin = new Padding(4) };
        button.Click += async (_, _) =>
        {
            if (busy) return;
            try { await action(); }
            catch (Exception ex) { Error(ex); }
        };
        actionButtons.Add(button);
        return button;
    }
    private Button ActionButton(string text, Action action) => ActionButton(text, () => { action(); return Task.CompletedTask; });
    private static FlowLayoutPanel Toolbar() => new() { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(2) };
    private (TabPage Page, TableLayoutPanel Body) Page(string name)
    {
        var page = new TabPage(name) { BackColor = Color.FromArgb(246, 248, 251), Padding = new Padding(10) };
        tabs.TabPages.Add(page);
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 1 };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.Controls.Add(body);
        return (page, body);
    }
    private static void Add(TableLayoutPanel body, Control control, bool fill = false)
    {
        int row = body.Controls.Count;
        body.RowCount = row + 1;
        body.RowStyles.Add(new RowStyle(fill ? SizeType.Percent : SizeType.AutoSize, fill ? 100 : 0));
        body.Controls.Add(control, 0, row);
    }

    private void BuildCheck()
    {
        var (_, body) = Page(L.T("Torrent ellenőrzése"));
        var bar = Toolbar();
        bar.Controls.Add(ActionButton(L.T(".torrent ellenőrzése…"), async () =>
        {
            using var picker = new OpenFileDialog { Filter = "Torrent|*.torrent" };
            if (picker.ShowDialog(this) == DialogResult.OK) await CheckPath(picker.FileName);
        }));
        bar.Controls.Add(ActionButton(L.T("Átadás a torrentkliensnek"), () => Forward(false)));
        Add(body, bar);
        Add(body, TextLabel(L.T("Metaadatok alapján · Nincs SHA-256 · A név és méret egyezése nem bizonyít tartalmi azonosságot.")));
        Add(body, summary);
        filter.Items.AddRange([L.T("Összes"), Matching.Exact, Matching.Similar, Matching.Missing]); filter.SelectedIndex = 0;
        filter.SelectedIndexChanged += (_, _) => RenderResults();
        Add(body, filter);
        foreach (var col in new[] { ("Category", L.T("Találat"), 185), ("Torrent", L.T("Torrentfájl"), 240), ("Local", L.T("Nyilvántartott elérési út"), 360), ("Similarity", L.T("Címhasonlóság"), 120), ("Reason", L.T("Találati indok"), 650) }) Column(results, col.Item1, col.Item2, col.Item3);
        results.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && results.Rows[e.RowIndex].DataBoundItem is MatchRow row) MessageBox.Show(this, row.Torrent + "\n\n" + row.Local + "\n\n" + row.Reason, L.T("Találat részletei")); };
        results.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewRow row in results.Rows)
                if (row.DataBoundItem is MatchRow match)
                    row.DefaultCellStyle.BackColor = L.Canonical(match.Category) switch { Matching.Exact => Color.FromArgb(223, 242, 226), Matching.Similar => Color.FromArgb(255, 242, 210), _ => Color.FromArgb(251, 224, 224) };
        };
        Add(body, results, true);
    }

    private void BuildRegistry()
    {
        var (_, body) = Page(L.T("Nyilvántartás"));
        Add(body, TextLabel(L.T("A program automatikusan a saját data mappájából dolgozik. Jelenleg csak videófájlokat kezel."), true));
        var path = new TextBox { Text = database.PathName, ReadOnly = true, Dock = DockStyle.Fill };
        Add(body, path);
        var bar = Toolbar();
        bar.Controls.Add(ActionButton(L.T("Mappa hozzáadása…"), async () => { using var picker = new FolderBrowserDialog(); if (picker.ShowDialog(this) == DialogResult.OK) await Scan(picker.SelectedPath); }));
        bar.Controls.Add(ActionButton(L.T("Mappa újraellenőrzése"), async () => { if (SelectedRoot() is { } root) await Scan(root.Folder); }));
        bar.Controls.Add(ActionButton(L.T("Eltávolítás a nyilvántartásból"), () =>
        {
            if (SelectedRoot() is { } root && Confirm(L.F($"Eltávolítod ezt a mappát a nyilvántartásból?\n{root.Folder}\nA lemezen lévő fájlok megmaradnak."))) { database.Remove(root.Folder); InvalidateReport(); ReloadRoots(); }
        }));
        var stop = new Button { Text = L.T("Megszakítás"), AutoSize = true, Margin = new Padding(4), Padding = new Padding(8, 4, 8, 4) };
        stop.Click += (_, _) => cancellation?.Cancel();
        bar.Controls.Add(stop);
        Add(body, bar);
        Column(roots, "Folder", L.T("Nyilvántartott mappa"), 550); Column(roots, "Checked", L.T("Utoljára ellenőrizve"), 280); Column(roots, "Videos", L.T("Videók"), 100);
        roots.Height = 160; roots.Dock = DockStyle.Top;
        Add(body, roots);
        var transfer = Toolbar();
        transfer.Controls.Add(ActionButton(L.T("Nyilvántartás exportálása…"), async () =>
        {
            using var picker = new SaveFileDialog { Filter = "SQLite|*.sqlite3", FileName = "TDC-nyilvantartas.sqlite3" };
            if (picker.ShowDialog(this) == DialogResult.OK) await Work(() => { database.Export(picker.FileName); return L.T("Exportálás kész: ") + picker.FileName; });
        }));
        transfer.Controls.Add(ActionButton(L.T("Nyilvántartás importálása…"), async () =>
        {
            using var picker = new OpenFileDialog { Filter = L.T("SQLite|*.sqlite3;*.db|Minden fájl|*.*") };
            if (picker.ShowDialog(this) == DialogResult.OK && Confirm(L.T("Az import lecseréli, nem egyesíti a nyilvántartást. Előtte mentés készül.\nA videófájlokat nem érinti. Folytatod?")))
                await Work(() => L.T("Importálás kész. Biztonsági mentés: ") + database.Import(picker.FileName), message => { InvalidateReport(); ReloadRoots(); status.Text = message; });
        }));
        Add(body, transfer);
        var searches = Toolbar();
        searches.Controls.Add(search);
        async Task Search() => await Work(() => database.Files(search.Text, 501), files => { searchResults.DataSource = files.Take(500).ToList(); searchNote.Text = files.Count > 500 ? L.T("Az első 500 találat látható; szűkítsd a keresést.") : L.F($"{files.Count} találat."); });
        searches.Controls.Add(ActionButton(L.T("Keresés"), Search));
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter && !busy) { e.SuppressKeyPress = true; await Search(); } };
        Add(body, searches);
        Column(searchResults, "Path", L.T("Nyilvántartott elérési út"), 800); Column(searchResults, "Size", L.T("Méret (bájt)"), 150);
        Add(body, searchResults, true); Add(body, searchNote);
    }

    private void BuildSettings()
    {
        var (page, _) = Page(L.T("Beállítások"));
        // A hosszabb beállításlap függőlegesen görgethető, nagyított betűknél is.
        page.Controls.Clear();
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(18) };
        page.Controls.Add(flow);
        automatic.Checked = settings.Automatic; closeAfter.Checked = settings.CloseAfterForward; client.Text = settings.Client;
        flow.Controls.Add(TextLabel(L.T("Nyelv"), true));
        language.Items.AddRange(["Magyar", "English"]);
        language.SelectedIndex = L.Language == "en" ? 1 : 0;
        language.SelectedIndexChanged += (_, _) =>
        {
            if (busy) return;
            L.SetLanguage(language.SelectedIndex == 1 ? "en" : "hu");
            ApplyLanguage();
        };
        flow.Controls.Add(language);
        flow.Controls.Add(TextLabel(L.T("A nyelv azonnal változik; megőrzéséhez kattints a Beállítások mentése gombra.")));
        flow.Controls.Add(TextLabel(L.T("Torrentkliens és automatizálás"), true));
        flow.Controls.Add(automatic); flow.Controls.Add(closeAfter);
        flow.Controls.Add(TextLabel(L.T("A bezárás kézi és automatikus átadás után is érvényes; indítási hibánál nyitva marad.")));
        flow.Controls.Add(TextLabel(L.T("Torrentkliens programja (.exe) vagy parancsikonja (.lnk):")));
        flow.Controls.Add(client);
        var bar = Toolbar(); bar.Dock = DockStyle.None;
        bar.Controls.Add(ActionButton(L.T("Tallózás…"), () => { using var picker = new OpenFileDialog { Filter = L.T("Torrentkliens|*.exe;*.lnk") }; if (picker.ShowDialog(this) == DialogResult.OK) client.Text = picker.FileName; }));
        bar.Controls.Add(ActionButton(L.T("Beállítások mentése"), () =>
        {
            var next = new Settings { Automatic = automatic.Checked, CloseAfterForward = closeAfter.Checked, Client = client.Text.Trim(), Language = L.Language };
            next.Save(settingsPath); settings = next; status.Text = L.T("Beállítások elmentve.");
        }));
        flow.Controls.Add(bar);
        flow.Controls.Add(TextLabel(L.T("A mentett beállítások a következő ellenőrzésre érvényesek.\nEgyezésnél figyelmeztet; üres nyilvántartásnál vagy videó nélküli torrentnél nem továbbít automatikusan.\nA kliens saját beállításai alapján az átadás letöltést is indíthat.")));
        flow.Controls.Add(TextLabel(L.T("Windows fájltársítás"), true));
        flow.Controls.Add(ActionButton(L.T(".torrent fájlok megnyitása a TDC-vel…"), () => { WindowsIntegration.Register(); WindowsIntegration.OpenDefaults(); status.Text = L.T("Regisztráció kész. A Windowsban válaszd a Torrent Duplicate Checker C# alkalmazást."); }));
        flow.Controls.Add(ActionButton(L.T("Windows társítási beállítások megnyitása"), WindowsIntegration.OpenDefaults));
        flow.Controls.Add(TextLabel(L.T("A társítást a Windowsban véglegesíted. Áthelyezés után regisztráld újra.\nA Pythonos társítástól külön regisztráció; visszaállítható a Windows beállításaiban.")));
    }

    private void BuildAbout()
    {
        var (_, body) = Page(L.T("Névjegy"));
        Add(body, TextLabel("TDC (Torrent Duplicate Checker) — C#", true));
        Add(body, TextLabel($"Version: v{Release.Version} · Build {Release.Build} · {Release.Date}"));
        Add(body, TextLabel("Created by Sarkament Ádám with assistance from AI. © 2026\nThird-party components retain their respective licences."));
        Add(body, TextLabel(L.T(".NET 8 · Windows Forms · Microsoft.Data.Sqlite · SQLite\n\nVideófájlok metaadatainak összehasonlítása, tartalmi hash nélkül.\nA hasonló cím felismerése fájlnévalapú becslés, nem tartalmi azonosítás.\n\nA C# változat normalizált szerkesztési távolságot használ;\na hasonlósági százalék eltérhet a Pythonos kiadástól.")));
    }

    private bool Confirm(string text) => MessageBox.Show(this, text, "TDC", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    private void Error(Exception ex) { status.Text = L.T("A művelet sikertelen. ") + ex.Message; MessageBox.Show(this, ex.Message, "TDC", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    private RootInfo? SelectedRoot() => roots.CurrentRow?.DataBoundItem as RootInfo;
    private void InvalidateReport() { report = null; results.DataSource = null; searchResults.DataSource = null; summary.Text = L.T("A nyilvántartás megváltozott; a torrentet ellenőrizd újra."); }
    private void ReloadRoots()
    {
        try
        {
            var rows = database.Roots(); roots.DataSource = rows;
            registryStatus.Text = rows.Count > 0 ? L.F($"Nyilvántartás sikeresen betöltődött · {rows.Count} mappa · {rows.Sum(r => r.Videos)} videó") : L.T("A nyilvántartás üres · Adj hozzá mappát, vagy importálj nyilvántartást.");
            registryStatus.ForeColor = rows.Count > 0 ? Color.FromArgb(22, 112, 58) : Color.FromArgb(154, 87, 0);
        }
        catch (Exception ex) { registryStatus.Text = L.T("A nyilvántartás nem tölthető be."); registryStatus.ForeColor = Color.DarkOrange; status.Text = ex.Message; }
    }

    // Az await után újra a felület szálán vagyunk, itt szabad vezérlőt módosítani.
    private async Task Work<T>(Func<T> operation, Action<T>? done = null)
    {
        if (busy) return;
        busy = true;
        language.Enabled = false;
        cancellation = new CancellationTokenSource();
        foreach (var button in actionButtons) button.Enabled = false;
        try { var value = await Task.Run(operation); if (done is not null) done(value); else status.Text = value?.ToString() ?? L.T("Kész."); }
        catch (OperationCanceledException) { status.Text = L.T("Művelet megszakítva. A korábbi nyilvántartás megmaradt."); }
        catch (Exception ex) { Error(ex); }
        finally { busy = false; language.Enabled = true; cancellation.Dispose(); cancellation = null; foreach (var button in actionButtons) button.Enabled = true; }
    }

    private async Task Scan(string folder)
    {
        status.Text = L.T("Mappa beolvasása…");
        var progress = new Progress<int>(count => status.Text = L.F($"Beolvasott videók: {count}"));
        await Work(() => database.Scan(folder, cancellation!.Token, progress), count => { InvalidateReport(); ReloadRoots(); status.Text = L.F($"Beolvasás kész: {count} videó. Adatbázis: {database.PathName}"); });
    }

    internal async Task CheckPath(string torrent)
    {
        if (busy) return;
        if (!File.Exists(torrent) || !Path.GetExtension(torrent).Equals(".torrent", StringComparison.OrdinalIgnoreCase)) { Error(new FileNotFoundException(L.T("Nem érhető el a .torrent fájl."))); return; }
        currentTorrent = Path.GetFullPath(torrent); report = null; forwarded = false; results.DataSource = null;
        tabs.SelectedIndex = 0; status.Text = L.T("Torrent ellenőrzése…");
        await Work(() => Matching.Compare(database.Files(), TorrentReader.Read(torrent), cancellation!.Token), value =>
        {
            report = value; RenderResults();
            summary.Text = L.F($"{value.Total} videó: {value.Exact} pontos név+méret · {value.Similar} csak hasonló · {value.Missing} nincs a nyilvántartásban");
            status.Text = L.T("Ellenőrzés kész. Az összesítés minden torrentvideóra vonatkozik.");
            // A Work még foglaltnak jelöli az ablakot, ezért a bezárást későbbre ütemezzük.
            if (settings.Automatic) BeginInvoke(new Action(() => { try { Forward(true); } catch (Exception ex) { Error(ex); } }));
        });
    }

    internal void RenderResults()
    {
        // A szűrés a stabil kategóriakulcsot használja, nem a lefordított feliratot.
        string[] categories = ["", Matching.Exact, Matching.Similar, Matching.Missing];
        int index = Math.Clamp(filter.SelectedIndex, 0, 3);
        results.DataSource = report?.Rows.Where(r => index == 0 || r.Category == categories[index])
            .Select(r => r with { Category = L.T(r.Category), Reason = L.Reason(r.Reason) }).ToList();
    }

    // A nyelvcsere a vezérlők tartalmát változtatja, nem épít új ablakot és nem törli az eredményeket.
    internal void ApplyLanguage()
    {
        void Translate(Control control)
        {
            // A szabadon beírt neveket és útvonalakat soha nem fordítjuk le.
            if (control is not TextBoxBase and not ComboBox && !string.IsNullOrEmpty(control.Text)) control.Text = L.T(control.Text);
            if (control is DataGridView grid)
                foreach (DataGridViewColumn column in grid.Columns) column.HeaderText = L.T(column.HeaderText);
            foreach (Control child in control.Controls) Translate(child);
        }
        SuspendLayout();
        Translate(this);
        search.PlaceholderText = L.T("Keresés a mentett elérési utakban…");
        int selected = Math.Clamp(filter.SelectedIndex, 0, 3);
        filter.Items.Clear();
        filter.Items.AddRange([L.T("Összes"), L.T(Matching.Exact), L.T(Matching.Similar), L.T(Matching.Missing)]);
        filter.SelectedIndex = selected;
        ReloadRoots();
        if (report is { } value)
            summary.Text = L.F($"{value.Total} videó: {value.Exact} pontos név+méret · {value.Similar} csak hasonló · {value.Missing} nincs a nyilvántartásban");
        else summary.Text = L.T("Még nincs ellenőrzött torrent.");
        searchResults.DataSource = null;
        searchNote.Text = "";
        status.Text = L.T("Kész. Adatbázis: ") + database.PathName;
        RenderResults();
        ResumeLayout(true);
    }

    internal static bool CanForward(Report value, long storedVideos) => storedVideos > 0 && value.Total > 0 && value.Exact == 0 && value.Similar == 0 && value.Missing == value.Total;
    private void Forward(bool auto)
    {
        if (report is null || currentTorrent is null || forwarded) { if (!auto) MessageBox.Show(this, L.T("Előbb ellenőrizz egy torrentet. Egy ellenőrzésből egyszer adható át.")); return; }
        if (auto && !CanForward(report, database.Roots().Sum(r => r.Videos)))
        {
            if (report.Exact + report.Similar > 0) MessageBox.Show(this, L.T("Pontos vagy hasonló találat van. Az átadás előtt nézd át a listát."), L.T("Lehetséges duplikáció"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!auto && !Confirm((report.Exact + report.Similar > 0 ? L.T("Pontos vagy hasonló találat van!\n\n") : "") + L.T("Átadod a torrentet a kliensnek? A kliens letöltést indíthat."))) return;
        try
        {
            WindowsIntegration.Launch(settings.Client, currentTorrent); forwarded = true;
            status.Text = L.T("A torrent átadva. A kliens tényleges fogadása nem ellenőrizhető.");
            if (settings.CloseAfterForward) Close();
        }
        catch (Exception ex) { Error(ex); }
    }
}
