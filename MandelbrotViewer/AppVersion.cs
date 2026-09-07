namespace MandelbrotViewer;

/// <summary>
/// Application version in X.Y.Z format (simplified Semantic Versioning).
/// Bump: X = breaking change, Y = new feature, Z = fix/refactor/docs.
/// The UI shows <see cref="Display"/>: if Z is 0 the short notation "X.Y" is used.
/// </summary>
public static class AppVersion
{
    public const int Major = 2;
    public const int Minor = 17;
    public const int Patch = 6;

    public static string Full => $"{Major}.{Minor}.{Patch}";

    public static string Display => Patch == 0 ? $"{Major}.{Minor}" : Full;
}
