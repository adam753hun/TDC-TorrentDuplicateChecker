using System.Text.Json;

namespace Tdc;

internal static class Program
{
    // Az STAThread a Windows fájlválasztókhoz és COM-parancsikonokhoz szükséges.
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length == 2 && args[0] == "--self-test") return Diagnostics.Run(args[1]);
        try
        {
            Application.Run(new MainForm(Path.Combine(AppContext.BaseDirectory, "data"), args.FirstOrDefault()));
            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(L.F($"A TDC nem indítható.\n{ex.Message}"), "TDC", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
