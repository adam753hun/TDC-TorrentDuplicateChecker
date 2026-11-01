using System.Globalization;
using System.Text;

namespace Tdc;

// A bencode adatokat közvetlenül olvassuk: nincs tracker-kapcsolat vagy letöltés.
internal sealed class TorrentReader(byte[] payload)
{
    private readonly byte[] data = payload;
    private int position;
    private const int Limit = 32 * 1024 * 1024;

    public static List<TorrentFile> Read(string path)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length > Limit) throw new InvalidDataException(L.T("A torrent nagyobb 32 MB-nál."));
        using var memory = new MemoryStream();
        // Korlátozott olvasás akkor is, ha a forrásfájl közben változna.
        byte[] buffer = new byte[8192];
        int read;
        while ((read = stream.Read(buffer)) > 0)
        {
            if (memory.Length + read > Limit) throw new InvalidDataException(L.T("A torrent nagyobb 32 MB-nál."));
            memory.Write(buffer, 0, read);
        }
        var parser = new TorrentReader(memory.ToArray());
        var root = parser.Parse(0) as Dictionary<string, object> ?? throw Invalid();
        if (parser.position != parser.data.Length) throw Invalid();
        var info = root.GetValueOrDefault("info") as Dictionary<string, object> ?? throw Invalid();
        if (info.ContainsKey("meta version")) throw new InvalidDataException(L.T("A v2/hibrid torrent még nem támogatott."));
        string Text(object? value) => value is byte[] bytes ? Encoding.UTF8.GetString(bytes) : throw Invalid();
        long Size(Dictionary<string, object> entry) => entry.GetValueOrDefault("length") is long n && n >= 0 ? n : throw Invalid();
        if (info.TryGetValue("files", out var files))
        {
            if (files is not List<object> list) throw Invalid();
            return list.Select(item =>
            {
                var entry = item as Dictionary<string, object> ?? throw Invalid();
                var parts = (entry.TryGetValue("path.utf-8", out var utfPath) ? utfPath : entry.GetValueOrDefault("path")) as List<object> ?? throw Invalid();
                if (parts.Count == 0) throw Invalid();
                var names = parts.Select(Text).ToArray();
                return new TorrentFile(names[^1], Size(entry), string.Join("/", names));
            }).ToList();
        }
        var name = Text(info.TryGetValue("name.utf-8", out var utfName) ? utfName : info.GetValueOrDefault("name"));
        return [new(name, Size(info), name)];
    }

    private static InvalidDataException Invalid() => new(L.T("Hibás vagy hiányos torrentadatok."));

    // Rekurzív feldolgozás: szám, bájtsorozat, lista vagy szótár következhet.
    private object Parse(int depth)
    {
        if (depth > 80 || position >= data.Length) throw Invalid();
        byte token = data[position++];
        if (token == 'i')
        {
            int start = position;
            while (position < data.Length && data[position] != 'e') position++;
            if (position >= data.Length) throw Invalid();
            string raw = Encoding.ASCII.GetString(data, start, position++ - start);
            if (!System.Text.RegularExpressions.Regex.IsMatch(raw, @"\A(?:0|-?[1-9][0-9]*)\z") || !long.TryParse(raw, CultureInfo.InvariantCulture, out long n)) throw Invalid();
            return n;
        }
        if (token == 'l' || token == 'd')
        {
            var list = new List<object>();
            var dictionary = new Dictionary<string, object>(StringComparer.Ordinal);
            while (position < data.Length && data[position] != 'e')
            {
                object value = Parse(depth + 1);
                if (token == 'l') list.Add(value);
                else
                {
                    if (value is not byte[] keyBytes) throw Invalid();
                    // A dictionary kulcsait UTF-8 szöveggel kezeljük; minden ismert kulcs ASCII.
                    string key = Encoding.UTF8.GetString(keyBytes);
                    if (!dictionary.TryAdd(key, Parse(depth + 1))) throw Invalid();
                }
            }
            if (position >= data.Length) throw Invalid();
            position++;
            return token == 'l' ? list : dictionary;
        }
        if (token is >= (byte)'0' and <= (byte)'9')
        {
            int start = --position;
            while (position < data.Length && data[position] is >= (byte)'0' and <= (byte)'9') position++;
            if (position >= data.Length || data[position] != ':') throw Invalid();
            string raw = Encoding.ASCII.GetString(data, start, position - start);
            if (raw.Length > 1 && raw[0] == '0') throw Invalid();
            if (!int.TryParse(raw, out int length) || length > data.Length - ++position) throw Invalid();
            var bytes = data.AsSpan(position, length).ToArray();
            position += length;
            return bytes;
        }
        throw Invalid();
    }
}
