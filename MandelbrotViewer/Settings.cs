using System.Text.Json;

namespace MandelbrotViewer;

/// <summary>
/// Impostazioni persistite tra un lancio e l'altro
/// (%APPDATA%\MandelbrotViewer\settings.json).
/// </summary>
public sealed class AppSettings
{
    public bool IterAuto { get; set; }
    public int MaxIter { get; set; } = 256;
    public int Palette { get; set; }
    public int AaIndex { get; set; }
    public string Engine { get; set; } = nameof(RenderEngine.Cuda);
    /// <summary>Scheda video scelta ("" = auto).</summary>
    public string Gpu { get; set; } = "";
    public int WinX { get; set; }
    public int WinY { get; set; }
    public int WinW { get; set; }
    public int WinH { get; set; }
    public bool Maximized { get; set; }

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MandelbrotViewer", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            string path = FilePath;
            if (!File.Exists(path)) return new AppSettings();
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings(); // file mancante o corrotto: default
        }
    }

    public void Save()
    {
        // Validazione minima: mai persistere valori assurdi.
        MaxIter = Math.Clamp(MaxIter, 50, 50000);

        string path = FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
