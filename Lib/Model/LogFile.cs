using System.Text.Json;

namespace LogFileLib;

// Représente une entrée de log écrite après chaque transfert de fichier
public class LogEntry
{
    public string Timestamp { get; set; } = "";  // Date et heure de l'action
    public string WorkName { get; set; } = ""; // Nom du travail de sauvegarde
    public string SourceFile { get; set; } = ""; // Chemin complet du fichier source
    public string DestinationFile { get; set; } = ""; // Chemin complet du fichier de destination
    public long FileSize { get; set; } // Taille du fichier transféré en octets
    public long TransferTimeMs { get; set; } // Temps de transfert en millisecondes, négatif en cas d'erreur
}

// Gère l'écriture des entrées de log dans un fichier JSON journalier
public class LogFile
{
    // Les fichiers log sont stockés à côté de l'exécutable
    private string logDirectory;

    // Verrou pour éviter les accès simultanés au fichier (tests parallèles ou multi-thread)
    private static readonly object fileLock = new object();

    public LogFile()
    {
        logDirectory = AppContext.BaseDirectory;
    }

    // Écrit une entrée dans le fichier log du jour (ex : "2024-01-17.json")
    public void WriteLogs(string workName, string sourceFile, string destinationFile, long fileSize, long transferTimeMs)
    {
        // Construire l'entrée de log
        LogEntry entry = new LogEntry
        {
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            WorkName = workName,
            SourceFile = sourceFile,
            DestinationFile = destinationFile,
            FileSize = fileSize,
            TransferTimeMs = transferTimeMs
        };

        // Construire le chemin du fichier log du jour
        string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
        string filePath = Path.Combine(logDirectory, fileName);

        // Le lock garantit qu'un seul thread écrit à la fois
        lock (fileLock)
        {
            // Lire les entrées existantes si le fichier existe déjà
            List<LogEntry> entries = new List<LogEntry>();

            if (File.Exists(filePath))
            {
                string existingContent = File.ReadAllText(filePath);
                entries = JsonSerializer.Deserialize<List<LogEntry>>(existingContent) ?? new List<LogEntry>();
            }

            // Ajouter la nouvelle entrée
            entries.Add(entry);

            // Réécrire toutes les entrées avec indentation pour la lisibilité
            string json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }
}
