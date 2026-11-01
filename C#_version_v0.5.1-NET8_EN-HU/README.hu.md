# TDC — C# / Windows Forms

**v0.5.1-cs · Build 20261027 · 2026.10.27**

[English overview](README.md)

## Magyar és angol felület

A **Beállítások → Nyelv** mezőben választhatsz a Magyar és English között. A felület azonnal átvált; a **Beállítások mentése / Save settings** gombbal a választás a következő indításra is megmarad. Régi beállításfájlnál magyar marad az alapértelmezés. Nyelvváltás közben a meglévő találatok megmaradnak; a keresési eredményt új kereséssel töltheted vissza.

A nyelv a `data/settings.json` fájl `language` mezőjében (`hu` vagy `en`) tárolódik. A fordítások a `Localization.cs` fájlban vannak; az adatbázis, a fájlnevek és a belső találati kategóriák nem változnak meg a nyelvtől. A Windows saját rendszerpárbeszédablakai a Windows nyelvi beállításait követhetik.

Ez a kétnyelvű csomagfrissítés változatlan verzióval és kiadási adatokkal készült. A korábbi data mappa tovább használható. A GitHubhoz készített bemutatkozó szövegek a `GITHUB_LEIRAS.md` fájlban találhatók.

A Python TDC első C#-os változata. Windows Forms felület, .NET 8, SQLite. A kód magyar megjegyzéseket tartalmaz. A Python-változatot nem írja felül.

## Indítás és fejlesztés

- Kész Windows x64 csomag: csomagold ki az egész mappát, indítsd a `TDC.exe` fájlt. Python nem szükséges, de a **.NET 8 Desktop Runtime x64** legyen telepítve. A sima .NET Runtime vagy ASP.NET Core Runtime önmagában nem elég. Letöltés: https://dotnet.microsoft.com/download/dotnet/8.0 — Windows / Desktop Runtime / x64. A mellette lévő DLL-eket és egyéb fájlokat is tartsd meg.
- Forrás: Visual Studio-ban nyisd meg a `TDC.slnx` vagy `TDC.csproj` fájlt. .NET 8 SDK és Windows desktop fejlesztési eszközök szükségesek. Parancssorból: `dotnet run --project TDC.csproj`.
- A telepített .NET 8-at használó Windows-kiadás újraépítése: `Build_Windows.bat`. Első fordításnál a NuGet letölti a függőségeket. A `Windows-NET8-x64` mappa tartalmát együtt kell továbbadni. A `--self-contained false` kapcsoló miatt a .NET futtatókörnyezet nem kerül bele.

## Használat

1. **Nyilvántartás → Mappa hozzáadása**: válaszd ki a videók gyökérmappáját.
2. A bejárás csak méretet, fájlnevet és időbélyeget olvas; nincs SHA-256 vagy videótartalom-olvasás. Megszakítható; hibás/hiányos bejárás nem cseréli le a korábbi pillanatképet. Linkeket és reparse pontokat kihagy.
3. **Torrent ellenőrzése → .torrent ellenőrzése**: v1 torrentet válassz.
4. Nézd meg az összesítést; kategóriára szűrhetsz, dupla kattintással az indok olvasható.

Zöld: pontos név és bájtméret. Sárga: hasonló cím. Piros: nincs találat a tárolt adatokban. Egyik egyezés sem bizonyít tartalmi azonosságot. A C# változat normalizált Levenshtein-hasonlóságot használ 85%-os küszöbbel; a százalék és egyes határesetek a Python SequenceMatcher eredményétől eltérhetnek. A név+méret egyezés szabálya változatlan. Évszám, évad, epizód és kiadási jelölések segítik a találatok szűrését.

## Saját adatok és átállás Pythonról

Az adatok a futtatott program melletti `data/index.sqlite3` és `data/settings.json` fájlokban vannak. Első indításkor üres nyilvántartás jön létre. Írható helyre csomagold ki; a Program Files helyett például saját felhasználói mappát válassz.

A Python-változatban exportáld a nyilvántartást, és a C# változatban importáld. Vagy a programok bezárása után másold a meglévő `data` mappát a C# .exe mellé. Az SQLite-táblák és JSON-kulcsok kompatibilisek. Az import ellenőrzi a forrást, lecseréli (nem egyesíti) az aktív adatokat, és előtte `index-before-import-…sqlite3` mentést készít. Az exportált fájl később Pythonból is használható. Más gépen a tárolt elérési utak változatlanok maradnak.

Átálláskor a kliens beállításait nézd át. A C# társítás külön regisztráció; a korábbi Pythonos társítást nem írja át magától. A társítási gomb után a Windowsban válaszd a **Torrent Duplicate Checker C#** alkalmazást. Áthelyezés után ismételd meg.

## Torrentkliens

Az .exe vagy .lnk útvonal és a jelölőnégyzetek menthetők. Automatikus átadás csak sikeres, támogatott videót tartalmazó és teljesen találat nélküli ellenőrzésnél történik, nem üres nyilvántartás mellett. Egyezésnél figyelmeztet; kézi átadás előtt megerősítés van. Bezárás az átadás után külön kapcsoló. A kliens saját szabályai szerint az átadás letöltést indíthat. A TDC az indítás sikerét látja, a kliens tényleges fogadását nem tudja visszaigazolni. Egyedi parancsikonok viselkedése eltérhet; a közvetlen kliens .exe a legkiszámíthatóbb.

Minden külső megnyitás külön ablakot indít. Egyszerre egy ablakban módosítsd a nyilvántartást; import előtt a többi példányt zárd be.

## Teljesítmény

A .NET optimalizált gépi kódot futtathat, a C#-os változat a címadatokat előre számolja az összehasonlításhoz. Ez CPU-igényes részeken előny lehet. A HDD-bejárás viszont főként a háttértár és az operációs rendszer metaadat-műveleteitől függ. Nem készült azonos adatokon, azonos algoritmussal végzett Python–C# sebességmérés, ezért konkrét gyorsulást nem állítunk. A címhasonlóság nagy állománynál ebben a kiadásban is költséges lehet.

## Ellenőrzés és korlátok

Fejlesztői diagnosztika: `TDC.exe --self-test "C:\teljes\utvonal\eredmeny.json"`. Elkülönített próbaadatokkal dolgozik, nem indít klienst és nem módosít társítást. A jelentés mellé GUI-képet is ment. Ha a jelentés mappájában `python-fixture.sqlite3` van a tesztben használt `python.2026.mkv` sorral, a Python-átjárhatóságot is ellenőrzi; különben ezt kihagyja. A tesztmappákat vizsgálathoz megtartja.

Csak videófájlok és v1 torrentek támogatottak; v2/hibrid, magnet, ISO/játékfájlok és qBittorrent API még nem. Nem töröl és nem nevez át videófájlokat. A valódi kliensfogadást és a felhasználó által véglegesített Windows-társítást külön gépen még nem teszteltük. A Windows-build nincs digitálisan aláírva. A C#-os változat még korai kiadás; a Pythonos adatokról tarts mentést.

## Fájlok és tanulás

- `Program.cs`: belépési pont, normál indítás vagy diagnosztika.
- `Models.cs`: adatcsomagok, JSON-beállítások és kiadási adatok.
- `MainForm.cs`: a négy fül, események, háttérmunka és állapotsor.
- `Database.cs`: SQLite-séma, bejárás, export/import. Az SQL-paraméterek megakadályozzák, hogy egy fájlnév utasításként értelmeződjön.
- `Matching.cs`: címértelmezés és hasonlóság. Új videókiterjesztés a `Video` halmazba írható; ez önmagában nem általános fájltámogatás.
- `TorrentReader.cs`: a torrent bencode formátumának korlátozott beolvasása.
- `WindowsIntegration.cs`: kliensindítás és Windows-társítás.
- `Diagnostics.cs`: offline funkcionális és GUI-ellenőrzések.

A `record` adatokat fog össze, a `using` lezárja az erőforrásokat, az `async/await` a felület válaszkészségét segíti. A tényleges hosszú munka `Task.Run` alatt fut; vezérlőket csak a GUI-szálon módosítunk. A verziót a `Models.cs` Release osztályában és a projekt verziómezőjében frissítsd.

Created by Sarkament Ádám with assistance from AI. © 2026

Third-party components retain their respective licences.

## Áttérés a nagyobb .NET 10-es kiadásról

Az új, kisebb futtatómappa neve `Windows-NET8-x64`. A korábbi `Windows-x64` külön megmaradhat. A programok bezárása után a régi `data` mappát másold az új TDC.exe mellé, vagy használd az export/import funkciót. Az SQLite-formátum és a beállítások nem változtak. A .torrent társítást az új helyről regisztráld újra.
