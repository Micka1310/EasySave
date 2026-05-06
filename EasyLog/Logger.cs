using System.Text.Json;

namespace EasyLog;

public class LogEntry
{
    public string Timestamp { get; set; } = "";
    public string WorkName { get; set; } = "";
    public string SourceFile { get; set; } = "";
    public string DestinationFile { get; set; } = "";
    public long FileSize { get; set; }
    public long TransferTimeMs { get; set; }
    public bool Success { get; set; } = true;
    public string ErrorMessage { get; set; } = "";
}

public interface ILogger
{
    void WriteLogs(string workName, string sourceFile, string destinationFile, long fileSize, long transferTimeMs, bool success = true, string errorMessage = "");
}

public class Logger : ILogger
{
    private readonly string logDirectory;
    private static readonly object fileLock = new object();

    public Logger()
    {
        // Les fichiers log sont stockés dans C:\EasyLog sur la machine de l'utilisateur
        logDirectory = @"C:\EasyLog";
        // Créer le dossier s'il n'existe pas encore
        Directory.CreateDirectory(logDirectory);
    }

    public Logger(string directory)
    {
        logDirectory = directory;
    }

    public void WriteLogs(string workName, string sourceFile, string destinationFile, long fileSize, long transferTimeMs, bool success = true, string errorMessage = "")
    {
        LogEntry entry = new LogEntry
        {
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            WorkName = workName,
            SourceFile = sourceFile,
            DestinationFile = destinationFile,
            FileSize = fileSize,
            TransferTimeMs = transferTimeMs,
            Success = success,
            ErrorMessage = errorMessage
        };

        string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
        string filePath = Path.Combine(logDirectory, fileName);

        lock (fileLock)
        {
            List<LogEntry> entries = new List<LogEntry>();

            if (File.Exists(filePath))
            {
                string existingContent = File.ReadAllText(filePath);
                entries = JsonSerializer.Deserialize<List<LogEntry>>(existingContent) ?? new List<LogEntry>();
            }

            entries.Add(entry);

            string json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }
}
