namespace MandelbrotViewer;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // Diagnostica e benchmark senza UI: --diag-dx [scheda] | --diag-gpu |
        // --bench-dx [scheda] | --bench-cuda [device] | --bench-cpu [--csv file]
        // (vedi Diagnostics.cs). `--csv file` accoda una riga per run al CSV.
        if (args.Length > 0 && args[0] == "--diag-dx")
        {
            Diagnostics.DiagDx(args.Length > 1 ? args[1] : null);
            return;
        }
        if (args.Length > 0 && args[0] == "--bench-dx")
        {
            var (device, csv) = ParseBenchArgs(args);
            Diagnostics.BenchDx(device, runs: 3, budget: BenchmarkStandard.Budget, csvPath: csv);
            return;
        }
        if (args.Length > 0 && args[0] == "--bench-cuda")
        {
            var (device, csv) = ParseBenchArgs(args);
            Diagnostics.BenchCuda(device, runs: 3, budget: BenchmarkStandard.Budget, csvPath: csv);
            return;
        }
        if (args.Length > 0 && args[0] == "--bench-cpu")
        {
            var (_, csv) = ParseBenchArgs(args);
            Diagnostics.BenchCpu(runs: 3, budget: BenchmarkStandard.Budget, csvPath: csv);
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

    /// <summary>
    /// Icona dell'app (app.ico incorporata) sulla finestra data; facoltativa,
    /// mai bloccante.
    /// </summary>
    internal static void ApplyIcon(Form form)
    {
        try
        {
            using var s = typeof(Program).Assembly
                .GetManifestResourceStream("MandelbrotViewer.app.ico");
            if (s != null) form.Icon = new Icon(s);
        }
        catch
        {
            // Senza icona si avvia comunque.
        }
    }

    /// <summary>
    /// Argomenti dei comandi --bench-*: primo posizionale = scheda/device,
    /// `--csv file` ovunque dopo il flag. Ritorna (device, csvPath).
    /// </summary>
    private static (string? Device, string? Csv) ParseBenchArgs(string[] args)
    {
        string? device = null, csv = null;
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--csv" && i + 1 < args.Length) csv = args[++i];
            else if (device == null) device = args[i];
        }
        return (device, csv);
    }
}
