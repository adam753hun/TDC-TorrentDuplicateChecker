# TDC — Torrent Duplicate Checker

**Windows desktop app · C# / .NET 8 · Hungarian and English interface**

[Magyar útmutató](README.hu.md) · [Changelog](CHANGELOG.md)

TDC helps you check whether video files listed in a torrent may already exist in your collection before downloading them again. It is designed for large video libraries, including collections stored on external hard drives that are not always connected.

## How it works

1. Add folders to the library. TDC stores video filenames, byte sizes, paths and modification times in a local SQLite database.
2. Open a v1 `.torrent` file. TDC compares its video entries with the saved library, even when the original drive is disconnected.
3. Review separate **exact filename + size**, **similar title** and **no match** results, with per-video totals and match explanations.

TDC does **not** read entire video files or calculate SHA-256/content hashes. Directory traversal still performs disk I/O, but avoids reading the full video collection. Neither a matching name and size nor a similar title proves identical file content.

## Features

- Metadata-only video indexing with cancellation and preservation of the previous snapshot after an incomplete scan.
- Local SQLite library with search, folder rescanning and removal from the library without deleting media files.
- Title matching that considers year and season/episode markers, including `S01E02` and `1x02`.
- Colour-coded results, category filtering and detailed explanations.
- Validated library import/export, with an automatic backup before replacement.
- Configurable torrent client executable or Windows shortcut.
- Optional automatic forwarding when no exact or similar video matches are found, plus optional closing after forwarding.
- Windows `.torrent` association registration, completed by the user in Windows settings.
- Hungarian and English UI, switchable immediately and saved for the next launch.

## Requirements and installation

The small Windows x64 release requires **.NET 8 Desktop Runtime x64**. The regular .NET Runtime or ASP.NET Core Runtime alone is not sufficient for the Windows Forms interface. Download the Desktop Runtime from [Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0).

Extract the entire Windows package to a writable folder and launch `TDC.exe`. Keep the supplied DLLs, configuration files and licences alongside it. Python is not required.

The app uses its own `data/index.sqlite3` database and `data/settings.json` settings file. Preserve the **data** folder when upgrading; do not include your personal data folder when sharing a release or publishing a repository. Existing Python TDC databases can be imported, and existing settings remain compatible.

## Language and forwarding settings

Open **Beállítások / Settings → Nyelv / Language** and choose **Magyar** or **English**. The interface switches immediately. Click **Beállítások mentése / Save settings** to keep the choice for the next launch. Older settings files default to Hungarian. Windows-owned dialogs may follow the operating system language.

Select the actual torrent client's `.exe` or `.lnk`, not TDC itself. Automatic forwarding is disabled by default and is blocked for an empty library, an unsuccessful check, torrents without supported videos, or any exact/similar video match. Manual forwarding asks for confirmation. Depending on the torrent client's settings, forwarding may start a download. TDC can detect a launch failure but cannot confirm that the client accepted the torrent.

Library import **replaces**, rather than merges, the current library. A backup is created first. Close other TDC windows before importing. Each external torrent opening currently starts a separate window.

## Build from source

Use a Windows machine with the **.NET 8 SDK or a newer compatible SDK** and desktop development tools. Open `TDC.csproj` in Visual Studio, or run:

```powershell
dotnet build TDC.csproj -c Release
dotnet run --project TDC.csproj
dotnet publish TDC.csproj -c Release -r win-x64 --self-contained false -o Windows-NET8-x64
```

`Build_Windows.bat` performs the framework-dependent Windows publish. NuGet restores Microsoft.Data.Sqlite and its dependencies. Hungarian source comments explain the main sections; translations are centralized in `Localization.cs`.

An offline diagnostic is available as `TDC.exe --self-test "C:\full\path\result.json"`. It creates isolated test data, checks core features and the GUI, and never launches a torrent client or changes file associations. The test workspaces are retained for inspection. A language regression test checks HU/EN switching, persistence and filtering.

## Scope and limitations

Current version: **v0.5.1-cs**, targeting **.NET 8**. This bilingual package update keeps the version and build metadata unchanged.

Only supported video extensions and v1 torrents are handled. Magnet links, v2/hybrid torrents, games/ISO files and direct qBittorrent API integration are not implemented. Title matching is heuristic, using normalized edit distance with an 85% threshold; it may miss alternate-language or unusual filenames. Percentages can differ from the Python edition. No controlled, same-algorithm Python–C# performance benchmark has been conducted.

The application source licence has not been specified by this update. Third-party component licences are included in `licences/` and remain applicable to their respective components.

## Credits

Created by Sarkament Ádám with assistance from AI. © 2026

Third-party components retain their respective licences.
