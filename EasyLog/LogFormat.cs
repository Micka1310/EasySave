namespace EasyLog;

public enum LogFormat
{
    Json,
    Xml
}

/// <summary>Format des fichiers journaliers (v1.1 console). Défaut JSON.</summary>
public static class LogSettings
{
    private static readonly object Gate = new();
    private static LogFormat _format = LogFormat.Json;

    public static LogFormat Format
    {
        get
        {
            lock (Gate) return _format;
        }
        set
        {
            lock (Gate) _format = value;
        }
    }

    /// <summary>Réinitialise au JSON (tests).</summary>
    public static void Reset()
    {
        lock (Gate) _format = LogFormat.Json;
    }
}
