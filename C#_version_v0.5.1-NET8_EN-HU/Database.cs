using Microsoft.Data.Sqlite;

namespace Tdc;

// A mezők és táblák a Python TDC formátumával egyeznek meg.
internal sealed class Database(string path)
{
    public string PathName { get; } = Path.GetFullPath(path);

    private static SqliteConnection Open(string path, bool readOnly = false)
    {
        var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate, Pooling = false, DefaultTimeout = 10 }.ToString());
        db.Open();
        return db;
    }

    private static void Execute(SqliteConnection db, string sql, SqliteTransaction? transaction = null, params object[] args)
    {
        using var command = db.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        for (int i = 0; i < args.Length; i++) command.Parameters.AddWithValue("$p" + i, args[i]);
        command.ExecuteNonQuery();
    }

    public void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PathName)!);
        using var db = Open(PathName);
        Execute(db, """
            CREATE TABLE IF NOT EXISTS files(root TEXT NOT NULL,path TEXT NOT NULL,name TEXT NOT NULL,size INTEGER NOT NULL,mtime_ns INTEGER NOT NULL,title TEXT NOT NULL,PRIMARY KEY(root,path));
            CREATE INDEX IF NOT EXISTS exact_file ON files(name,size);
            CREATE TABLE IF NOT EXISTS roots(root TEXT PRIMARY KEY,scanned_at TEXT);
            INSERT OR IGNORE INTO roots(root) SELECT DISTINCT root FROM files;
            """);
    }

    public List<RootInfo> Roots()
    {
        using var db = Open(PathName, true);
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT roots.root,roots.scanned_at,COUNT(files.path) FROM roots LEFT JOIN files ON roots.root=files.root GROUP BY roots.root ORDER BY roots.root";
        using var rows = cmd.ExecuteReader();
        var result = new List<RootInfo>();
        while (rows.Read()) result.Add(new(rows.GetString(0), rows.IsDBNull(1) ? L.T("Ismeretlen (korábbi adat)") : rows.GetString(1), rows.GetInt64(2)));
        return result;
    }

    public List<StoredFile> Files(string? query = null, int limit = 0)
    {
        using var db = Open(PathName, true);
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT root,path,name,size,mtime_ns,title FROM files" + (query is null ? "" : " WHERE instr(lower(path),lower($query))>0") + " ORDER BY path" + (limit > 0 ? " LIMIT " + limit : "");
        if (query is not null) cmd.Parameters.AddWithValue("$query", query);
        using var rows = cmd.ExecuteReader();
        var result = new List<StoredFile>();
        while (rows.Read()) result.Add(new(rows.GetString(0), rows.GetString(1), rows.GetString(2), rows.GetInt64(3), rows.GetInt64(4), rows.GetString(5)));
        return result;
    }

    public void Remove(string root)
    {
        using var db = Open(PathName);
        using var tx = db.BeginTransaction();
        Execute(db, "DELETE FROM files WHERE root=$p0", tx, root);
        Execute(db, "DELETE FROM roots WHERE root=$p0", tx, root);
        tx.Commit();
    }

    public static string NativePath(string path)
    {
        if (path.StartsWith(@"\\?\")) return path;
        path = Path.GetFullPath(path);
        return path.StartsWith(@"\\") ? @"\\?\UNC\" + path[2..] : @"\\?\" + path;
    }
    private static string DisplayPath(string path) => path.StartsWith(@"\\?\UNC\") ? @"\\" + path[8..] : path.StartsWith(@"\\?\") ? path[4..] : path;

    public int Scan(string folder, CancellationToken cancel, IProgress<int>? progress = null)
    {
        string root = DisplayPath(Path.GetFullPath(folder));
        if (!Directory.Exists(NativePath(root))) throw new DirectoryNotFoundException(L.T("A mappa nem érhető el."));
        using var db = Open(PathName);
        using var tx = db.BeginTransaction();
        Execute(db, "CREATE TEMP TABLE snapshot AS SELECT * FROM files WHERE 0", tx);
        using var insert = db.CreateCommand();
        insert.Transaction = tx;
        insert.CommandText = "INSERT INTO snapshot VALUES($root,$path,$name,$size,$time,$title)";
        foreach (string name in new[] { "$root", "$path", "$name", "$size", "$time", "$title" }) insert.Parameters.Add(new SqliteParameter(name, null));
        var pending = new Stack<string>();
        pending.Push(NativePath(root));
        int count = 0, errors = 0;
        var messages = new List<string>();
        void Record(string item, Exception ex) { errors++; if (messages.Count < 20) messages.Add($"{DisplayPath(item)}: {ex.Message}"); }
        while (pending.TryPop(out string? current))
        {
            cancel.ThrowIfCancellationRequested();
            try
            {
                foreach (var item in new DirectoryInfo(current).EnumerateFileSystemInfos("*", new EnumerationOptions { AttributesToSkip = 0, IgnoreInaccessible = false, ReturnSpecialDirectories = false }))
                {
                    cancel.ThrowIfCancellationRequested();
                    try
                    {
                        // A linkeket kihagyjuk, nehogy ciklus vagy másik meghajtó bejárása történjen.
                        if ((item.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                        if (item is DirectoryInfo directory) { pending.Push(directory.FullName); continue; }
                        if (item is not FileInfo file || !Matching.Video.Contains(file.Extension)) continue;
                        long ns = checked((file.LastWriteTimeUtc.Ticks - DateTime.UnixEpoch.Ticks) * 100);
                        object[] values = [root, DisplayPath(file.FullName), file.Name.ToLowerInvariant(), file.Length, ns, Matching.Parse(file.Name).Title];
                        for (int i = 0; i < values.Length; i++) insert.Parameters[i].Value = values[i];
                        insert.ExecuteNonQuery();
                        if (++count % 100 == 0) progress?.Report(count);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OverflowException) { Record(item.FullName, ex); }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Record(current, ex); }
        }
        cancel.ThrowIfCancellationRequested();
        if (errors > 0) throw new IOException(L.F($"A bejárás hiányos: {errors} hiba. A korábbi nyilvántartás megmaradt.\n") + string.Join("\n", messages));
        Execute(db, "DELETE FROM files WHERE root=$p0", tx, root);
        Execute(db, "INSERT INTO files SELECT * FROM snapshot", tx);
        Execute(db, "INSERT OR REPLACE INTO roots VALUES($p0,$p1)", tx, root, DateTimeOffset.Now.ToString("O"));
        tx.Commit();
        return count;
    }

    public void Export(string destination)
    {
        if (string.Equals(Path.GetFullPath(destination), PathName, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException(L.T("Az aktív adatbázis nem lehet exportcél."));
        string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var source = Open(PathName, true))
            using (var target = Open(temporary)) source.BackupDatabase(target);
            File.Move(temporary, destination, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public string Import(string sourcePath)
    {
        if (string.Equals(Path.GetFullPath(sourcePath), PathName, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException(L.T("Ez már az aktív nyilvántartás."));
        string temporary = PathName + ".import-" + Guid.NewGuid().ToString("N");
        string backup = Path.Combine(Path.GetDirectoryName(PathName)!, $"index-before-import-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.sqlite3");
        try
        {
            var stage = new Database(temporary);
            stage.Initialize();
            using (var source = Open(sourcePath, true))
            using (var target = Open(temporary))
            {
                Execute(source, "PRAGMA trusted_schema=OFF; PRAGMA query_only=ON;");
                using var readTx = source.BeginTransaction(deferred: true);
                using var check = source.CreateCommand();
                check.Transaction = readTx;
                check.CommandText = "PRAGMA quick_check";
                if (!Equals(check.ExecuteScalar(), "ok")) throw new InvalidDataException(L.T("Sérült adatbázis."));
                check.CommandText = "SELECT type FROM sqlite_master WHERE name='files'";
                if (!Equals(check.ExecuteScalar(), "table")) throw new InvalidDataException(L.T("Nem TDC-adatbázis."));
                using var writeTx = target.BeginTransaction();
                check.CommandText = "SELECT root,path,name,size,mtime_ns,title FROM files";
                using (var rows = check.ExecuteReader())
                {
                    while (rows.Read())
                    {
                        var values = Enumerable.Range(0, 6).Select(rows.GetValue).ToArray();
                        if (values[0] is not string r || r.Length == 0 || values[1] is not string p || p.Length == 0 || values[2] is not string n || n.Length == 0 || values[3] is not long size || size < 0 || values[4] is not long || values[5] is not string) throw new InvalidDataException(L.T("Hibás fájladatok."));
                        Execute(target, "INSERT INTO files VALUES($p0,$p1,$p2,$p3,$p4,$p5)", writeTx, values);
                    }
                }
                check.CommandText = "SELECT type FROM sqlite_master WHERE name='roots'";
                object? type = check.ExecuteScalar();
                if (type is not null)
                {
                    if (!Equals(type, "table")) throw new InvalidDataException(L.T("Hibás mappatábla."));
                    check.CommandText = "SELECT root,scanned_at FROM roots";
                    using var rows = check.ExecuteReader();
                    while (rows.Read())
                    {
                        if (rows.GetValue(0) is not string root || root.Length == 0 || (!rows.IsDBNull(1) && rows.GetValue(1) is not string)) throw new InvalidDataException(L.T("Hibás mappaadatok."));
                        Execute(target, "INSERT INTO roots VALUES($p0,$p1)", writeTx, rows.GetValue(0), rows.GetValue(1));
                    }
                }
                Execute(target, "INSERT OR IGNORE INTO roots(root) SELECT DISTINCT root FROM files", writeTx);
                writeTx.Commit();
            }
            Export(backup);
            using (var source = Open(temporary, true))
            using (var target = Open(PathName)) source.BackupDatabase(target);
            return backup;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
