using System.Diagnostics;
using Microsoft.Win32;

namespace Tdc;

internal static class WindowsIntegration
{
    public const string AppName = "Torrent Duplicate Checker C#";
    private const string ProgId = "TDC.CSharp.Torrent";
    private const string Capabilities = @"Software\TDC.CSharp\Capabilities";

    public static void ValidateClient(string client)
    {
        if (string.IsNullOrWhiteSpace(client) || !Path.IsPathFullyQualified(client) || !File.Exists(client) || !new[] { ".exe", ".lnk" }.Contains(Path.GetExtension(client), StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException(L.T("Válassz létező torrentkliens .exe fájlt vagy .lnk parancsikont."));
        if (string.Equals(Path.GetFullPath(client), Environment.ProcessPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(L.T("A kliens nem lehet maga a TDC."));
    }

    public static void Launch(string client, string torrent)
    {
        ValidateClient(client);
        if (!File.Exists(torrent) || !Path.GetExtension(torrent).Equals(".torrent", StringComparison.OrdinalIgnoreCase)) throw new FileNotFoundException(L.T("Nem érhető el a torrent."));
        var start = new ProcessStartInfo(client) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(client)! };
        start.ArgumentList.Add(Path.GetFullPath(torrent));
        using var process = Process.Start(start);
    }

    public static string OpenCommand(string executable) => $"\"{executable}\" \"%1\"";

    // Csak a felhasználó nevében regisztrálunk; UserChoice felülírás nincs.
    public static void Register()
    {
        string exe = Environment.ProcessPath ?? throw new InvalidOperationException(L.T("Ismeretlen programútvonal."));
        void Set(string key, string name, string value)
        {
            using var entry = Registry.CurrentUser.CreateSubKey(key);
            entry.SetValue(name, value, RegistryValueKind.String);
        }
        Set(@"Software\Classes\" + ProgId, "", "TDC C# torrentellenőrzés");
        Set(@"Software\Classes\" + ProgId + @"\shell\open\command", "", OpenCommand(exe));
        Set(@"Software\Classes\.torrent\OpenWithProgids", ProgId, "");
        Set(Capabilities, "ApplicationName", AppName);
        Set(Capabilities, "ApplicationDescription", L.T("Torrent ellenőrzése a helyi nyilvántartásban."));
        Set(Capabilities + @"\FileAssociations", ".torrent", ProgId);
        Set(@"Software\RegisteredApplications", AppName, Capabilities);
    }

    public static void OpenDefaults() => Process.Start(new ProcessStartInfo("ms-settings:defaultapps?registeredAppUser=" + Uri.EscapeDataString(AppName)) { UseShellExecute = true })?.Dispose();
}
