using System.Text.Json;
using WorkFile;

namespace WorkListFile;

public class WorkList
{
    /// <summary>Limite par défaut (console, tests). L’app WPF peut augmenter <see cref="MaxWorkCount"/>.</summary>
    public const int MaxWorks = 5;

    private static readonly string StorageDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EasySave");
    private static readonly string FilePath = Path.Combine(StorageDirectory, "works.json");
    private static readonly object FileLock = new();

    private List<Work> works;

    /// <summary>Nombre maximal de travaux (<see cref="MaxWorks"/> pour la console ; WPF peut utiliser une valeur plus grande).</summary>
    public int MaxWorkCount { get; set; } = MaxWorks;

    public WorkList()
    {
        works = LoadFromFile();
    }

    public List<Work> GetWork()
    {
        return works;
    }

    public bool IsFull()
    {
        return works.Count >= MaxWorkCount;
    }

    public void AddWork(List<string> parameter)
    {
        Work newWork = new Work(parameter[0], parameter[1], parameter[2], parameter[3]);
        works.Add(newWork);
        SaveToFile();
    }

    public bool RemoveWork(int index)
    {
        if (index < 0 || index >= works.Count)
        {
            return false;
        }

        works.RemoveAt(index);
        SaveToFile();
        return true;
    }

    private void SaveToFile()
    {
        lock (FileLock)
        {
            Directory.CreateDirectory(StorageDirectory);

            var data = works.Select(w => new WorkDto
            {
                Name = w.GetName(),
                Source = w.GetSourceDirectory(),
                Destination = w.GetDestinationDirectory(),
                Type = w.GetWorkType()
            }).ToList();

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }

    private static List<Work> LoadFromFile()
    {
        lock (FileLock)
        {
            MigrateLegacyFileIfNeeded();

            if (!File.Exists(FilePath))
            {
                return [];
            }

            try
            {
                string content = File.ReadAllText(FilePath);
                var dtos = JsonSerializer.Deserialize<List<WorkDto>>(content) ?? [];
                return dtos
                    .Select(d => new Work(d.Name, d.Source, d.Destination, d.Type))
                    .ToList();
            }
            catch
            {
                return [];
            }
        }
    }

    private static void MigrateLegacyFileIfNeeded()
    {
        string legacyPath = Path.Combine(AppContext.BaseDirectory, "works.json");
        if (File.Exists(FilePath) || !File.Exists(legacyPath))
        {
            return;
        }

        Directory.CreateDirectory(StorageDirectory);
        File.Copy(legacyPath, FilePath, overwrite: false);
    }

    private class WorkDto
    {
        public string Name { get; set; } = "";
        public string Source { get; set; } = "";
        public string Destination { get; set; } = "";
        public string Type { get; set; } = "";
    }
}
