namespace MandelbrotViewer;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // Diagnostica senza UI: MandelbrotViewer --diag-dx [nome scheda] | --diag-gpu (vedi Diagnostics.cs).
        if (args.Length > 0 && args[0] == "--diag-dx")
        {
            Diagnostics.DiagDx(args.Length > 1 ? args[1] : null);
            return;
        }
        if (args.Length > 0 && args[0] == "--diag-gpu")
        {
            Diagnostics.DiagGpu();
            return;
        }
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new MandelbrotForm());
    }
}
