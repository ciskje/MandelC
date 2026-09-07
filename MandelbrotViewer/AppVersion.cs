namespace MandelbrotViewer;

/// <summary>
/// Versione dell'applicazione in formato X.Y.Z (Semantic Versioning semplificato).
/// Bump: X = breaking change, Y = nuova funzionalità, Z = fix/refactor/docs.
/// La UI mostra <see cref="Display"/>: se Z è 0 si usa la notazione breve "X.Y".
/// </summary>
public static class AppVersion
{
    public const int Major = 2;
    public const int Minor = 17;
    public const int Patch = 3;

    public static string Full => $"{Major}.{Minor}.{Patch}";

    public static string Display => Patch == 0 ? $"{Major}.{Minor}" : Full;
}
