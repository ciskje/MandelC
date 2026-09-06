namespace MandelbrotViewer;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // Diagnostica e benchmark senza UI: --diag-dx [scheda] | --diag-gpu | --bench-dx [scheda] (vedi Diagnostics.cs).
        if (args.Length > 0 && args[0] == "--diag-dx")
        {
            Diagnostics.DiagDx(args.Length > 1 ? args[1] : null);
            return;
        }
        if (args.Length > 0 && args[0] == "--bench-dx")
        {
            Diagnostics.BenchDx(args.Length > 1 ? args[1] : null,
                runs: 3, budget: BenchmarkStandard.Budget);
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
