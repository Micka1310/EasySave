using System.Text;
using System.Text.Json;
using System.Xml.Serialization;

namespace EasyLog;

public class LogEntry
{
    public string Timestamp { get; set; } = "";
    public string WorkName { get; set; } = "";
    public string SourceFile { get; set; } = "";
    public string DestinationFile { get; set; } = "";
    public long FileSize { get; set; }
    public long TransferTimeMs { get; set; }
    public long EncryptionTimeMs { get; set; }
    public bool Success { get; set; } = true;
    public string ErrorMessage { get; set; } = "";
}

public interface ILogger
{
    void WriteLogs(string workName, string sourceFile, string destinationFile, long fileSize, long transferTimeMs, long encryptionTimeMs = 0, bool success = true, string errorMessage = "");
}

public class Logger : ILogger
{
    private readonly string logDirectory;
    private static readonly object FileLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly XmlSerializer XmlDocSerializer = new(typeof(LogEntriesDocument));

    public Logger()
    {
        logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
        Directory.CreateDirectory(logDirectory);
    }

    public Logger(string directory)
    {
        logDirectory = directory;
    }

    public void WriteLogs(string workName, string sourceFile, string destinationFile, long fileSize, long transferTimeMs, long encryptionTimeMs = 0, bool success = true, string errorMessage = "")
    {
        LogEntry entry = new LogEntry
        {
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            WorkName = workName,
            SourceFile = sourceFile,
            DestinationFile = destinationFile,
            FileSize = fileSize,
            TransferTimeMs = transferTimeMs,
            EncryptionTimeMs = encryptionTimeMs,
            Success = success,
            ErrorMessage = errorMessage
        };

        string ext = LogSettings.Format == LogFormat.Xml ? ".xml" : ".json";
        string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ext;
        string filePath = Path.Combine(logDirectory, fileName);

        lock (FileLock)
        {
            List<LogEntry> entries = LoadEntries(filePath);
            entries.Add(entry);
            SaveEntries(filePath, entries);
        }
    }

    private List<LogEntry> LoadEntries(string filePath)
    {
        if (!File.Exists(filePath))
            return [];

        try
        {
            string content = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(content))
                return [];

            if (LogSettings.Format == LogFormat.Xml)
            {
                using StringReader reader = new StringReader(content);
                LogEntriesDocument? doc = XmlDocSerializer.Deserialize(reader) as LogEntriesDocument;
                return doc?.Entries ?? [];
            }

            return JsonSerializer.Deserialize<List<LogEntry>>(content) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static void SaveEntries(string filePath, List<LogEntry> entries)
    {
        if (LogSettings.Format == LogFormat.Xml)
        {
            LogEntriesDocument doc = new LogEntriesDocument { Entries = entries };
            using Utf8StringWriter sw = new Utf8StringWriter();
            XmlDocSerializer.Serialize(sw, doc);
            File.WriteAllText(filePath, sw.ToString(), Encoding.UTF8);
            return;
        }

        string json = JsonSerializer.Serialize(entries, JsonOptions);
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }

    /// <summary>StringWriter qui déclare UTF-8 pour l'en-tête XML.</summary>
    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
