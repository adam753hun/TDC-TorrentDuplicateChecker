# TDC — Torrent Duplicate Checker

**Windows · C# / .NET 8 · Magyar / English felület**

[English](README.md) · [Változásnapló](CHANGELOG.md) · [Felelős használat](USAGE_A TDC helyi fájlkezelési és duplikáció-ellenőrzési célra készült. Nem jogsértő tartalmak megszerzésére vagy terjesztésére szolgál. A felhasználó felelőssége, hogy a program használata és a hozzáfért vagy továbbított tartalom megfeleljen az alkalmazandó jogszabályoknak és mások jogainak, ideértve a szerzői jogokat is.

A program „adott állapotban”, garancia nélkül érhető el. A jogszabályok által megengedett mértékben a készítők és közreműködők kizárják a felelősségüket a használatból vagy visszaélésből eredő károkért. Ez a nyilatkozat nem korlátozza azokat a jogokat vagy kötelezettségeket, amelyek alkalmazandó jog alapján nem zárhatók ki. A tájékoztató nem helyettesít szoftverlicencet..md)

A TDC Windowsos alkalmazás, amely a torrentek videófájljait helyi SQLite-nyilvántartással hasonlítja össze a felesleges ismételt letöltések elkerüléséhez. Fájlmetaadatokkal dolgozik, a videók teljes beolvasása és tartalmi hash nélkül. C# / .NET 8, magyar és angol felület.

## Használat

Telepítsd a [.NET 8 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/8.0) környezetet, csomagold ki a teljes kiadást írható helyre, és indítsd a `TDC.exe` fájlt. A többi DLL-t és konfigurációs fájlt is tartsd meg.

1. Nyilvántartás → Mappa hozzáadása: a videók metaadatainak beolvasása.
2. Torrent ellenőrzése → .torrent ellenőrzése: v1 torrent összevetése a mentett adatokkal.
3. Nézd át a színes találatlistát és az összesítést. Dupla kattintással az indok is olvasható.

Zöld: pontos név és méret; sárga: hasonló cím; piros: nincs találat. Az egyezés nem bizonyít tartalmi azonosságot. A hasonlóság fájlnévalapú becslés. A beolvasás megszakítható; hiányos bejárás nem cseréli le a korábbi pillanatképet.

A nyilvántartás és beállítások a program melletti `data` mappában vannak. Frissítéskor őrizd meg, megosztáskor hagyd ki a személyes adatokat. Az import lecseréli, nem egyesíti az adatbázist; előtte mentést készít. Import előtt zárd be a többi TDC-ablakot.

A Beállításokban választható Magyar / English nyelv, torrentkliens, automatikus továbbítás és átadás utáni bezárás. A nyelv azonnal vált, a mentés megőrzi a következő indításra. Egyezésnél a program figyelmeztet. A kliens saját szabályai alapján az átadás letöltést indíthat; a tényleges fogadás nem ellenőrizhető. A Windows-társítást a felhasználó véglegesíti.

## Fejlesztés és korlátok

Nyisd meg a `TDC.csproj` fájlt Visual Studio-ban, vagy használd a README.md fordítási parancsait. Magyar kódmegjegyzések és offline diagnosztika segítik a módosítást. A `Build_Windows.bat` a telepített .NET 8-ra támaszkodó kiadást készíti.

Verzió: **v0.5.1-cs**. Videófájlok és v1 torrentek támogatottak; magnet, v2/hibrid, ISO/játékfájlok és közvetlen kliens-API még nem. Minden külső megnyitás külön ablakot indít. Nincs videótartalom-olvasás, hash-számítás vagy videófájl-törlés.

## Felelős használat

A TDC helyi fájlkezelési és duplikáció-ellenőrzési célra készült. Nem jogsértő tartalmak megszerzésére vagy terjesztésére szolgál. A felhasználó felelőssége, hogy a program használata és a hozzáfért vagy továbbított tartalom megfeleljen az alkalmazandó jogszabályoknak és mások jogainak, ideértve a szerzői jogokat is.

A program „adott állapotban”, garancia nélkül érhető el. A jogszabályok által megengedett mértékben a készítők és közreműködők kizárják a felelősségüket a használatból vagy visszaélésből eredő károkért. Ez a nyilatkozat nem korlátozza azokat a jogokat vagy kötelezettségeket, amelyek alkalmazandó jog alapján nem zárhatók ki. A tájékoztató nem helyettesít szoftverlicencet.

A külső összetevők saját licencei érvényesek; lásd a `licences` mappát. Ez a tájékoztató önmagában nem ad forráskód-felhasználási licencet az alkalmazáshoz.
