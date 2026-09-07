namespace MandelbrotViewer;

/// <summary>
/// Log eventi in memoria (ultime 200 righe con timestamp): errori dei benchmark
/// e altre segnalazioni, visibili nel dialog log/diagnostica (menu Aiuto).
/// </summary>
internal static class AppLog
{
    private static readonly object _gate = new();
    private static readonly Queue<string> _lines = new();
    private const int Capacity = 200;

    public static void Add(string message)
    {
        lock (_gate)
        {
            _lines.Enqueue($"{DateTime.Now:HH:mm:ss}  {message}");
            while (_lines.Count > Capacity) _lines.Dequeue();
        }
    }

    public static string GetText()
    {
        lock (_gate)
            return _lines.Count > 0 ? string.Join(Environment.NewLine, _lines) + Environment.NewLine : "(vuoto)" + Environment.NewLine;
    }
}
