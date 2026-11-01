# TDC — Torrent Duplicate Checker

A TDC Windowsos alkalmazás, amely a torrentek videófájljait helyi SQLite-nyilvántartással hasonlítja össze a felesleges ismételt letöltések elkerüléséhez. Fájlmetaadatokkal dolgozik, a videók teljes beolvasása és tartalmi hash nélkül. C# / .NET 8, magyar és angol felület.

## Rendeltetés

A program segítségével torrent megnyitása előtt ellenőrizhető, hogy az adott videó megtalálható-e már valamelyik háttértáron. Különösen nagy gyűjteményeknél és időszakosan csatlakoztatott külső meghajtóknál hasznos. Külön jelöli a pontos fájlnév+bájtméret egyezést és a hasonló cím alapján feltételezett találatot, videónkénti összesítéssel és indokokkal.

A mentett nyilvántartás HDD nélkül is kereshető, exportálható és importálható. A torrent átadható egy kiválasztott kliensnek, találat nélkül opcionálisan automatikusan. A név és méret egyezése nem bizonyít tartalmi azonosságot.

## Felelős használat

A TDC helyi fájlkezelési és duplikáció-ellenőrzési célra készült. Nem jogsértő tartalmak megszerzésére vagy terjesztésére szolgál. A felhasználó felelőssége, hogy a program használata és a hozzáfért vagy továbbított tartalom megfeleljen az alkalmazandó jogszabályoknak és mások jogainak, ideértve a szerzői jogokat is.

A program „adott állapotban”, garancia nélkül érhető el. A jogszabályok által megengedett mértékben a készítők és közreműködők kizárják a felelősségüket a használatból vagy visszaélésből eredő károkért. Ez a nyilatkozat nem korlátozza azokat a jogokat vagy kötelezettségeket, amelyek alkalmazandó jog alapján nem zárhatók ki. A tájékoztató nem helyettesít szoftverlicencet.
