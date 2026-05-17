using System.Text.Json;
using System.Xml;
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
    private static readonly XmlSerializer LogDocumentSerializer = new(typeof(LogDocument));

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
        LogRoutingConfig routing = CentralLogRouting.Load();

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

        if (routing.WriteLocal)
        {
            string ext = LogFormatSettings.Current == LogFormat.Xml ? ".xml" : ".json";
            string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ext;
            string filePath = Path.Combine(logDirectory, fileName);

            lock (FileLock)
            {
                if (LogFormatSettings.Current == LogFormat.Xml)
                    AppendXml(filePath, entry);
                else
                    AppendJson(filePath, entry);
            }
        }

        if (routing.WriteCentral)
            CentralLogSender.TrySendInBackground(entry, routing.CentralBaseUrl);
    }

    private static void AppendJson(string filePath, LogEntry entry)
    {
        List<LogEntry> entries = [];

        if (File.Exists(filePath))
        {
            string existingContent = File.ReadAllText(filePath);
            entries = JsonSerializer.Deserialize<List<LogEntry>>(existingContent) ?? [];
        }

        entries.Add(entry);
        string json = JsonSerializer.Serialize(entries, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    private static void AppendXml(string filePath, LogEntry entry)
    {
        LogDocument doc = new LogDocument();

        if (File.Exists(filePath))
        {
            try
            {
                using FileStream fs = File.OpenRead(filePath);
                if (LogDocumentSerializer.Deserialize(fs) is LogDocument existing)
                    doc = existing;
            }
            catch
            {
                doc = new LogDocument();
            }
        }

        doc.Entries.Add(entry);

        var settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = false, Encoding = new System.Text.UTF8Encoding(false) };
        using var writer = XmlWriter.Create(filePath, settings);
        LogDocumentSerializer.Serialize(writer, doc);
    }
}
