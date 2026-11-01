# TDC - Torrent Duplicate Checker

TDC is a Windows desktop utility that checks torrent video files against a saved SQLite library to help avoid duplicate downloads. It uses file metadata rather than reading or hashing entire videos. C# / .NET 8, Hungarian and English interface.

## Intended purpose

TDC helps you check whether a video may already exist on your drives before downloading it again. It is useful for large collections and external drives that are not always connected. It separates exact filename + byte size matches from similar-title suggestions, with per-video totals and explanations.

The saved library can be searched offline and exported or imported. A selected torrent client can receive the checked torrent, optionally automatically when no matches are found. Filename and size matches do not prove identical content.

## Responsible use

TDC is intended for local file management and duplicate checking. It is not intended to obtain or distribute infringing content. Users are responsible for ensuring that their use of the application and any content they access or forward complies with applicable law and the rights of others, including copyright.

The application is provided “as is”, without warranties. To the extent permitted by applicable law, its authors and contributors disclaim liability for damages arising from its use or misuse. This notice does not limit rights or obligations that cannot legally be excluded. It is not a substitute for a software licence.
