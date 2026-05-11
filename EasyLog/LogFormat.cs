namespace EasyLog;

public enum LogFormat
{
    Json,
    Xml
}

/// <summary>
/// Format des fichiers journaliers (v1.1 console). Par défaut JSON.
/// </summary>
public static class LogFormatSettings
{
    private static LogFormat _current = LogFormat.Json;

    public static LogFormat Current
    {
        get => _current;
        set => _current = value;
    }

    /// <summary>Réinitialise au format JSON (tests).</summary>
    public static void ResetToDefault() => _current = LogFormat.Json;
}
