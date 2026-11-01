# GitHub-bemutatkozó szövegek

## Rövid angol leírás a repository Description mezőjéhez

Windows desktop app that checks torrent video files against an offline SQLite library, helping avoid duplicate downloads without reading or hashing entire videos. C# / .NET 8, Hungarian and English UI.

## Rövid magyar leírás

A TDC egy Windowsos alkalmazás, amely a torrentben szereplő videókat összeveti a lemezeinken már meglévő fájlok nyilvántartásával. Segít elkerülni a felesleges ismételt letöltéseket, a videók teljes beolvasása és tartalmi hash számítása nélkül.

## A program célja — magyar összefoglaló

A Torrent Duplicate Checker azért készült, hogy torrent megnyitása előtt könnyen ellenőrizhessük, megvan-e már az adott videó valamelyik háttértárunkon. Különösen akkor hasznos, ha nagy videógyűjteményt tartunk külső HDD-ken, amelyek nem mindig vannak csatlakoztatva.

A program először eltárolja a kiválasztott mappák videófájljainak nevét, méretét, elérési útját és módosítási idejét egy helyi SQLite-adatbázisban. Ezután egy v1 .torrent fájl videóit ehhez a mentett nyilvántartáshoz hasonlítja. Külön jelöli a pontos név+méret egyezést és a hasonló cím alapján feltételezett találatot, évszám- és epizódjelöléseket is figyelembe véve.

A cél a HDD-kímélő ellenőrzés: nem olvassa végig a videókat, és nem számol SHA-256 vagy más tartalmi hash-t. Emiatt a találatok tájékoztató jellegűek, és nem bizonyítják a fájlok tartalmi azonosságát. A mentett nyilvántartás a HDD leválasztása után is használható.

A felület magyarul és angolul használható. A program támogatja a nyilvántartás exportálását/importálását, valamint a torrent továbbítását egy kiválasztott kliensnek. A továbbítás és az utána történő bezárás beállítható; egyezésnél figyelmeztet.

## A feltöltéshez előkészített fájlok

- `README.md`: angol nyitóoldal a GitHubhoz.
- `README.hu.md`: részletes magyar útmutató.
- `CHANGELOG.md`: változásnapló.
- `.gitignore`: kizárja a szokásos buildmappákat és a személyes `data` mappát. Feltöltés előtt a repository tartalmát ettől függetlenül nézd át.

Ez a frissítés nem hozott létre GitHub-repositoryt, és nem töltött fel semmit.
