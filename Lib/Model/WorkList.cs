using System.Text.Json;
using WorkFile;

namespace WorkListFile;

public class WorkList
{
    public const int MaxWorks = 5;

    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "works.json");
    private static readonly object FileLock = new();

    private List<Work> works;

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
        return works.Count >= MaxWorks;
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

    private class WorkDto
    {
        public string Name { get; set; } = "";
        public string Source { get; set; } = "";
        public string Destination { get; set; } = "";
        public string Type { get; set; } = "";
    }
}
