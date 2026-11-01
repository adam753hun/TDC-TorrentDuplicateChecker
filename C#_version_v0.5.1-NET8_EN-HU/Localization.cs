using System.Globalization;

namespace Tdc;

// Egyetlen fordítási táblázat: az adatbázis és a találati kategóriák nyelvfüggetlenek maradnak.
// Új feliratnál adj magyar–angol párt a táblához, és használd a T vagy F segédet.
internal static class L
{
    public static string Language { get; private set; } = "hu";
    public static CultureInfo Culture => CultureInfo.GetCultureInfo(Language == "en" ? "en-US" : "hu-HU");
    public static void SetLanguage(string? language) => Language = language == "en" ? "en" : "hu";
    private static readonly Dictionary<string, string> English = new(StringComparer.Ordinal)
    {
        ["Nyilvántartás betöltése…"] = "Loading library…",
        ["Kész."] = "Ready.",
        ["Még nincs ellenőrzött torrent."] = "No torrent checked yet.",
        ["Keresés a mentett elérési utakban…"] = "Search stored file paths…",
        ["Automatikus továbbítás, ha nincs pontos vagy hasonló találat"] = "Automatically forward when there are no exact or similar matches",
        ["TDC bezárása a torrent átadása után"] = "Close TDC after forwarding the torrent",
        ["Nem olvasható beállítások; automatizálás kikapcsolva. "] = "Settings could not be read; automation is disabled. ",
        ["Kész. Adatbázis: "] = "Ready. Database: ",
        ["Megszakítás kérése elküldve. A művelet lezárása után zárd be újra az ablakot."] = "Cancellation requested. Close the window again after the operation finishes.",
        ["Torrent ellenőrzése"] = "Check torrent",
        [".torrent ellenőrzése…"] = "Check .torrent…",
        ["Átadás a torrentkliensnek"] = "Forward to torrent client",
        ["Metaadatok alapján · Nincs SHA-256 · A név és méret egyezése nem bizonyít tartalmi azonosságot."] = "Metadata only · No SHA-256 · Matching name and size does not prove identical content.",
        ["Összes"] = "All",
        ["Találat"] = "Match",
        ["Torrentfájl"] = "Torrent file",
        ["Nyilvántartott elérési út"] = "Stored file path",
        ["Címhasonlóság"] = "Title similarity",
        ["Találati indok"] = "Match reason",
        ["Találat részletei"] = "Match details",
        ["Nyilvántartás"] = "Library",
        ["A program automatikusan a saját data mappájából dolgozik. Jelenleg csak videófájlokat kezel."] = "The app automatically uses its own data folder. Currently, only video files are indexed.",
        ["Mappa hozzáadása…"] = "Add folder…",
        ["Mappa újraellenőrzése"] = "Rescan folder",
        ["Eltávolítás a nyilvántartásból"] = "Remove from library",
        ["Eltávolítod ezt a mappát a nyilvántartásból?\n{0}\nA lemezen lévő fájlok megmaradnak."] = "Remove this folder from the library?\n{0}\nFiles on disk will not be deleted.",
        ["Megszakítás"] = "Cancel",
        ["Nyilvántartott mappa"] = "Indexed folder",
        ["Utoljára ellenőrizve"] = "Last scanned",
        ["Videók"] = "Videos",
        ["Nyilvántartás exportálása…"] = "Export library…",
        ["Nyilvántartás importálása…"] = "Import library…",
        ["Exportálás kész: "] = "Export complete: ",
        ["SQLite|*.sqlite3;*.db|Minden fájl|*.*"] = "SQLite|*.sqlite3;*.db|All files|*.*",
        ["Az import lecseréli, nem egyesíti a nyilvántartást. Előtte mentés készül.\nA videófájlokat nem érinti. Folytatod?"] = "Import replaces the current library; it does not merge libraries. A backup will be created first.\nVideo files will not be changed. Continue?",
        ["Importálás kész. Biztonsági mentés: "] = "Import complete. Backup: ",
        ["Az első 500 találat látható; szűkítsd a keresést."] = "Only the first 500 results are shown; narrow your search.",
        ["{0} találat."] = "{0} results.",
        ["Keresés"] = "Search",
        ["Méret (bájt)"] = "Size (bytes)",
        ["Beállítások"] = "Settings",
        ["Torrentkliens és automatizálás"] = "Torrent client and automation",
        ["A bezárás kézi és automatikus átadás után is érvényes; indítási hibánál nyitva marad."] = "Applies to manual and automatic forwarding. TDC stays open if launching fails.",
        ["Torrentkliens programja (.exe) vagy parancsikonja (.lnk):"] = "Torrent client executable (.exe) or shortcut (.lnk):",
        ["Tallózás…"] = "Browse…",
        ["Torrentkliens|*.exe;*.lnk"] = "Torrent client|*.exe;*.lnk",
        ["Beállítások mentése"] = "Save settings",
        ["Beállítások elmentve."] = "Settings saved.",
        ["A mentett beállítások a következő ellenőrzésre érvényesek.\nEgyezésnél figyelmeztet; üres nyilvántartásnál vagy videó nélküli torrentnél nem továbbít automatikusan.\nA kliens saját beállításai alapján az átadás letöltést is indíthat."] = "Saved settings apply to the next check.\nMatches trigger a warning. No automatic forwarding with an empty library or no supported video files.\nForwarding may start a download, depending on the client settings.",
        ["Windows fájltársítás"] = "Windows file association",
        [".torrent fájlok megnyitása a TDC-vel…"] = "Open .torrent files with TDC…",
        ["Regisztráció kész. A Windowsban válaszd a Torrent Duplicate Checker C# alkalmazást."] = "Registration complete. Select Torrent Duplicate Checker C# in Windows settings.",
        ["Windows társítási beállítások megnyitása"] = "Open Windows default app settings",
        ["A társítást a Windowsban véglegesíted. Áthelyezés után regisztráld újra.\nA Pythonos társítástól külön regisztráció; visszaállítható a Windows beállításaiban."] = "Confirm the association in Windows settings. Register again after moving the app.\nThis registration is separate from the Python edition and can be changed in Windows settings.",
        ["Névjegy"] = "About",
        [".NET 8 · Windows Forms · Microsoft.Data.Sqlite · SQLite\n\nVideófájlok metaadatainak összehasonlítása, tartalmi hash nélkül.\nA hasonló cím felismerése fájlnévalapú becslés, nem tartalmi azonosítás.\n\nA C# változat normalizált szerkesztési távolságot használ;\na hasonlósági százalék eltérhet a Pythonos kiadástól."] = ".NET 8 · Windows Forms · Microsoft.Data.Sqlite · SQLite\n\nVideo file metadata comparison without content hashing.\nSimilar titles are inferred from filenames, not verified by content.\n\nThe C# edition uses normalized edit distance;\nsimilarity scores may differ from the Python edition.",
        ["A művelet sikertelen. "] = "Operation failed. ",
        ["A nyilvántartás megváltozott; a torrentet ellenőrizd újra."] = "The library has changed; check the torrent again.",
        ["Nyilvántartás sikeresen betöltődött · {0} mappa · {1} videó"] = "Library loaded successfully · {0} folders · {1} videos",
        ["A nyilvántartás üres · Adj hozzá mappát, vagy importálj nyilvántartást."] = "The library is empty · Add a folder or import a library.",
        ["A nyilvántartás nem tölthető be."] = "The library could not be loaded.",
        ["Művelet megszakítva. A korábbi nyilvántartás megmaradt."] = "Operation cancelled. The previous library has been preserved.",
        ["Mappa beolvasása…"] = "Scanning folder…",
        ["Beolvasott videók: {0}"] = "Videos scanned: {0}",
        ["Beolvasás kész: {0} videó. Adatbázis: {1}"] = "Scan complete: {0} videos. Database: {1}",
        ["Nem érhető el a .torrent fájl."] = "The .torrent file is unavailable.",
        ["Torrent ellenőrzése…"] = "Checking torrent…",
        ["{0} videó: {1} pontos név+méret · {2} csak hasonló · {3} nincs a nyilvántartásban"] = "{0} videos: {1} exact name + size · {2} similar only · {3} not in library",
        ["Ellenőrzés kész. Az összesítés minden torrentvideóra vonatkozik."] = "Check complete. The summary includes all torrent videos.",
        ["Előbb ellenőrizz egy torrentet. Egy ellenőrzésből egyszer adható át."] = "Check a torrent first. Each check can be forwarded only once.",
        ["Pontos vagy hasonló találat van. Az átadás előtt nézd át a listát."] = "Exact or similar matches were found. Review the list before forwarding.",
        ["Lehetséges duplikáció"] = "Possible duplicate",
        ["Pontos vagy hasonló találat van!\n\n"] = "Exact or similar matches were found!\n\n",
        ["Átadod a torrentet a kliensnek? A kliens letöltést indíthat."] = "Forward this torrent to the client? The client may start a download.",
        ["A torrent átadva. A kliens tényleges fogadása nem ellenőrizhető."] = "Torrent forwarded. Actual receipt by the client cannot be verified.",
        ["Üres beállításfájl."] = "Empty settings file.",
        ["Ismeretlen (korábbi adat)"] = "Unknown (legacy data)",
        ["A mappa nem érhető el."] = "The folder is unavailable.",
        ["A bejárás hiányos: {0} hiba. A korábbi nyilvántartás megmaradt.\n"] = "Scan incomplete: {0} errors. The previous library has been preserved.\n",
        ["Az aktív adatbázis nem lehet exportcél."] = "The active database cannot be the export destination.",
        ["Ez már az aktív nyilvántartás."] = "This is already the active library.",
        ["Sérült adatbázis."] = "Damaged database.",
        ["Nem TDC-adatbázis."] = "Not a TDC database.",
        ["Hibás fájladatok."] = "Invalid file data.",
        ["Hibás mappatábla."] = "Invalid folder table.",
        ["Hibás mappaadatok."] = "Invalid folder data.",
        ["A torrent nagyobb 32 MB-nál."] = "The torrent exceeds 32 MB.",
        ["A v2/hibrid torrent még nem támogatott."] = "V2/hybrid torrents are not supported yet.",
        ["Hibás vagy hiányos torrentadatok."] = "Invalid or incomplete torrent data.",
        ["Válassz létező torrentkliens .exe fájlt vagy .lnk parancsikont."] = "Select an existing torrent client .exe file or .lnk shortcut.",
        ["A kliens nem lehet maga a TDC."] = "The client cannot be TDC itself.",
        ["Nem érhető el a torrent."] = "The torrent is unavailable.",
        ["Ismeretlen programútvonal."] = "Unknown application path.",
        ["TDC C# torrentellenőrzés"] = "TDC C# torrent check",
        ["Torrent ellenőrzése a helyi nyilvántartásban."] = "Check torrents against the local library.",
        ["A TDC nem indítható.\n{0}"] = "TDC could not start.\n{0}",
        ["PONTOS NÉV + MÉRET"] = "EXACT NAME + SIZE",
        ["HASONLÓ CÍM"] = "SIMILAR TITLE",
        ["NINCS TALÁLAT"] = "NO MATCH",
        ["Azonos fájlnév és bájtméret; a tartalom nincs ellenőrizve."] = "Same filename and byte size; content has not been verified.",
        ["azonos cím"] = "same title",
        ["hasonló cím"] = "similar title",
        ["filmjelölt (nincs epizódjelölés)"] = "possible movie (no episode marker)",
        ["azonos évad-/epizódjelölés"] = "same season/episode marker",
        ["azonos évszám"] = "same year",
        ["évszám hiányzik"] = "year missing",
        ["kézi ellenőrzés szükséges"] = "manual review required",
        ["eltérő felbontás"] = "different resolution",
        ["azonos fájlméret"] = "same file size",
        ["eltérő fájlméret"] = "different file size",
        ["A jelenlegi nyilvántartásban nincs találat."] = "No matching entry in the current library.",
        ["Nyelv"] = "Language",
        ["A nyelv azonnal változik; megőrzéséhez kattints a Beállítások mentése gombra."] = "The language changes immediately. Click Save settings to keep it for the next launch.",
    };
    private static readonly Dictionary<string, string> Hungarian = English
        .GroupBy(pair => pair.Value).ToDictionary(group => group.Key, group => group.First().Key, StringComparer.Ordinal);

    public static string Canonical(string text) => Hungarian.GetValueOrDefault(text, text);
    public static string T(string text)
    {
        string canonical = Canonical(text);
        return Language == "en" ? English.GetValueOrDefault(canonical, canonical) : canonical;
    }
    // Az interpolált szöveg mintáját fordítjuk, az útvonalakat és számokat nem szövegként cserélgetjük.
    public static string F(FormattableString value) => string.Format(Culture, T(value.Format), value.GetArguments());
    public static string Reason(string text)
    {
        string whole = T(text);
        return whole != text ? whole : string.Join("; ", text.Split("; ").Select(T));
    }
}
