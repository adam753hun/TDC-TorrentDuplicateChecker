using System.Text;
using System.Text.RegularExpressions;

namespace Tdc;

internal sealed record Identity(string Title, string Year, string Episode, string Resolution);

internal static class Matching
{
    public static readonly HashSet<string> Video = new(StringComparer.OrdinalIgnoreCase)
        { ".mkv", ".mp4", ".avi", ".mov", ".wmv", ".m4v", ".ts", ".webm", ".mpg", ".mpeg" };
    public const string Exact = "PONTOS NÉV + MÉRET", Similar = "HASONLÓ CÍM", Missing = "NINCS TALÁLAT";
    // A minták egyszer készülnek el, nem minden helyi fájl / torrent párnál.
    private static readonly Regex Episode = new(@"\b(?:s\d{1,2}[ -]*e\d{1,3}(?:[ -]*e\d{1,3})*|\d{1,2}x\d{1,3})\b", RegexOptions.Compiled);
    private static readonly Regex Season = new(@"\bs\d{1,2}\b", RegexOptions.Compiled);
    private static readonly Regex Year = new(@"\b(?:19\d{2}|20\d{2})\b", RegexOptions.Compiled);
    private static readonly Regex ReleaseTag = new(@"\b(?:2160p|1080[pi]|720p|480p|4k|x26[45]|h[ -]?26[45]|hevc|av1|blu[ -]?ray|b[dr]rip|web[ -]?(?:dl|rip)|hdtv|dvdrip|remux|aac|dts|ddp|ac3|hun|eng|multi|dubbed|subbed)\b", RegexOptions.Compiled);
    private static readonly Regex Digits = new(@"\d+", RegexOptions.Compiled);
    private static readonly Regex Numbers = new(@"\b\d+\b", RegexOptions.Compiled);
    private static readonly Regex Words = new(@"\w+", RegexOptions.Compiled);
    private static readonly Regex Resolution = new(@"\b(?:2160p|1080[pi]|720p|480p|4k)\b", RegexOptions.Compiled);

    public static Identity Parse(string name)
    {
        var text = Regex.Replace(Path.GetFileNameWithoutExtension(name).Normalize(NormalizationForm.FormKC).ToLowerInvariant(), @"[._]+", " ");
        var episode = Episode.Match(text);
        var season = episode.Success ? Match.Empty : Season.Match(text);
        var year = Year.Match(text);
        if (year.Success && text[..year.Index].Trim(' ', '-', '(', '[', '{').Length == 0) year = Match.Empty;
        var tag = ReleaseTag.Match(text);
        int boundary = new[] { episode, season, year, tag }.Where(m => m.Success).Select(m => m.Index).DefaultIfEmpty(text.Length).Min();
        string key = episode.Success ? string.Join(",", Digits.Matches(episode.Value).Select(m => int.Parse(m.Value)))
            : season.Success ? "season:" + int.Parse(Digits.Match(season.Value).Value) : "";
        return new(string.Join(" ", Words.Matches(text[..boundary]).Select(m => m.Value)), year.Value, key, Resolution.Match(text).Value);
    }

    // Normalizált Levenshtein-hasonlóság. Két sor memóriát használ, nem teljes mátrixot.
    public static double Score(string a, string b)
    {
        if (a == b) return 1;
        if (a.Length == 0 || b.Length == 0) return 0;
        if ((double)Math.Min(a.Length, b.Length) / Math.Max(a.Length, b.Length) < .85) return 0;
        int[] previous = Enumerable.Range(0, b.Length + 1).ToArray(), current = new int[b.Length + 1];
        for (int i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (int j = 1; j <= b.Length; j++)
                current[j] = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1), previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            (previous, current) = (current, previous);
        }
        return 1 - (double)previous[b.Length] / Math.Max(a.Length, b.Length);
    }

    public static Report Compare(List<StoredFile> files, List<TorrentFile> torrent, CancellationToken cancel)
    {
        // Azonossági adatok egyszer készülnek el a teljes összehasonlításhoz.
        var candidates = files.DistinctBy(f => f.Path, StringComparer.OrdinalIgnoreCase).Select(f => (File: f, Id: Parse(f.Name))).ToList();
        var rows = new List<MatchRow>();
        int total = 0, exact = 0, similar = 0, missing = 0;
        foreach (var file in torrent.Where(f => Video.Contains(Path.GetExtension(f.Name))))
        {
            cancel.ThrowIfCancellationRequested();
            total++;
            var id = Parse(file.Name);
            var matches = new List<MatchRow>();
            foreach (var candidate in candidates)
            {
                cancel.ThrowIfCancellationRequested();
                var local = candidate.File;
                var other = candidate.Id;
                if (string.Equals(file.Name, local.Name, StringComparison.OrdinalIgnoreCase) && file.Size == local.Size)
                {
                    matches.Add(new(Exact, file.RelativePath, local.Path, "—", "Azonos fájlnév és bájtméret; a tartalom nincs ellenőrizve."));
                    continue;
                }
                if (id.Episode != other.Episode || (id.Year != "" && other.Year != "" && id.Year != other.Year)) continue;
                if (id.Title.Length == 0 || other.Title.Length == 0) continue;
                if (!Numbers.Matches(id.Title).Select(m => m.Value).SequenceEqual(Numbers.Matches(other.Title).Select(m => m.Value))) continue;
                double score = Score(id.Title, other.Title);
                if (score < .85) continue;
                var reason = new List<string> { id.Title == other.Title ? "azonos cím" : "hasonló cím" };
                reason.Add(id.Episode == "" ? "filmjelölt (nincs epizódjelölés)" : "azonos évad-/epizódjelölés");
                reason.Add(id.Year != "" && other.Year != "" ? "azonos évszám" : "évszám hiányzik; kézi ellenőrzés szükséges");
                if (id.Resolution != "" && other.Resolution != "" && id.Resolution != other.Resolution) reason.Add("eltérő felbontás");
                reason.Add(file.Size == local.Size ? "azonos fájlméret" : "eltérő fájlméret");
                matches.Add(new(Similar, file.RelativePath, local.Path, score.ToString("P0"), string.Join("; ", reason)));
            }
            if (matches.Any(r => r.Category == Exact)) exact++;
            else if (matches.Count > 0) similar++;
            else { missing++; matches.Add(new(Missing, file.RelativePath, "", "", "A jelenlegi nyilvántartásban nincs találat.")); }
            rows.AddRange(matches);
        }
        return new(rows, total, exact, similar, missing);
    }
}
