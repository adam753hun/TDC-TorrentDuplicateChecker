using System.Text.Json;

namespace Tdc;

// Közös kiadási adatok: a címsor és a Névjegy is innen olvassa őket.
internal static class Release
{
    public const string Version = "0.5.1-cs";
    public const string Build = "20261027";
    public const string Date = "2026.10.27";
}

// A record egyszerű adatcsomag. A neveket a felület oszlopokhoz is használja.
internal sealed record StoredFile(string Root, string Path, string Name, long Size, long MtimeNs, string Title);
internal sealed record RootInfo(string Folder, string Checked, long Videos);
internal sealed record TorrentFile(string Name, long Size, string RelativePath);
internal sealed record MatchRow(string Category, string Torrent, string Local, string Similarity, string Reason);
internal sealed record Report(List<MatchRow> Rows, int Total, int Exact, int Similar, int Missing);

internal sealed class Settings
{
    // A JSON-kulcsok a Python-változattal egyeznek meg.
    [System.Text.Json.Serialization.JsonPropertyName("automatic")]
    public bool Automatic { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("close_after_forward")]
    public bool CloseAfterForward { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("client")]
    public string Client { get; set; } = "";
    // Régebbi beállításfájlban még nincs ilyen kulcs: ilyenkor magyar marad a felület.
    [System.Text.Json.Serialization.JsonPropertyName("language")]
    public string Language { get; set; } = "hu";

    public static Settings Load(string path) => File.Exists(path)
        ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? throw new InvalidDataException(L.T("Üres beállításfájl."))
        : new();

    public void Save(string path)
    {
        if (Automatic || !string.IsNullOrWhiteSpace(Client)) WindowsIntegration.ValidateClient(Client);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, path, true);
    }
}
