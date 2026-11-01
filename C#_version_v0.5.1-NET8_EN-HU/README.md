# TDC — Torrent Duplicate Checker

**Windows · C# / .NET 8 · Hungarian / English UI**

[Magyar](README.hu.md) · [Changelog](CHANGELOG.md) · [Responsible use](USAGE_TDC is intended for local file management and duplicate checking. It is not intended to obtain or distribute infringing content. Users are responsible for ensuring that their use of the application and any content they access or forward complies with applicable law and the rights of others, including copyright.

The application is provided “as is”, without warranties. To the extent permitted by applicable law, its authors and contributors disclaim liability for damages arising from its use or misuse. This notice does not limit rights or obligations that cannot legally be excluded. It is not a substitute for a software licence..md)

TDC is a Windows desktop utility that checks torrent video files against a saved SQLite library to help avoid duplicate downloads. It uses file metadata rather than reading or hashing entire videos. C# / .NET 8, Hungarian and English interface.

## Features

- Metadata-only video indexing, with cancellation and preservation of the previous snapshot after an incomplete scan.
- Exact filename + size matches and separate similar-title suggestions, considering year and episode markers.
- Colour-coded results, filters, per-video totals and explanations.
- Searchable SQLite library, folder rescanning and removal without deleting media files.
- Validated import/export and automatic backup before import replaces the current library.
- Configurable torrent client, optional automatic forwarding and optional close after forwarding.
- Windows file association registration, confirmed by the user in Windows settings.
- Immediately switchable Hungarian/English interface; save the selection in Settings.

## Run

Install [.NET 8 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/8.0). Extract the full Windows package to a writable folder and launch `TDC.exe`. Keep all supplied DLLs, configuration files and licences. The regular .NET Runtime alone is not sufficient for Windows Forms.

Open **Library → Add folder**, wait for indexing, then select **Check torrent → Check .torrent**. Review the results before forwarding. Automatic forwarding is disabled by default and is blocked for empty libraries, failed checks, torrents without supported videos, or any exact/similar match. Forwarding may start a download, depending on the client settings. TDC cannot verify that the client accepted it.

Data and settings live beside the application in `data/index.sqlite3` and `data/settings.json`. Preserve `data` when updating; do not publish personal data. Import replaces rather than merges the library, creating a backup first. Close other TDC windows before importing.

## Build

Use a Windows machine with .NET 8 SDK or a newer compatible SDK and desktop development tools. Open `TDC.csproj` in Visual Studio or run:

```powershell
dotnet build TDC.csproj -c Release
dotnet run --project TDC.csproj
dotnet publish TDC.csproj -c Release -r win-x64 --self-contained false -o Windows-NET8-x64
```

`Build_Windows.bat` builds the small framework-dependent release. Source comments are Hungarian; translations are in `Localization.cs`. Offline diagnostic: `TDC.exe --self-test "C:\tests\result.json"`. It creates isolated test data without launching a torrent client or changing file associations.

## Scope

Version **v0.5.1-cs**, .NET 8. Only supported video extensions and v1 torrents are handled. Magnet links, v2/hybrid torrents, ISO/game indexing and direct torrent-client API integration are not implemented. Similar-title detection uses normalized edit distance with an 85% threshold and may miss unusual filenames. Metadata matches do not prove identical content. Each external opening currently starts a separate window.

## Responsible use

TDC is intended for local file management and duplicate checking. It is not intended to obtain or distribute infringing content. Users are responsible for ensuring that their use of the application and any content they access or forward complies with applicable law and the rights of others, including copyright.

The application is provided “as is”, without warranties. To the extent permitted by applicable law, its authors and contributors disclaim liability for damages arising from its use or misuse. This notice does not limit rights or obligations that cannot legally be excluded. It is not a substitute for a software licence.

Third-party components retain their respective licences; see `licences/`. This notice does not grant a source-code licence for the application.
