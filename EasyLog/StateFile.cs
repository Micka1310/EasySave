using System.Text.Json;

namespace EasyLog;

public class WorkState
{
    public string WorkName { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public string Status { get; set; } = "Inactive";
    public int TotalFiles { get; set; }
    public long TotalSize { get; set; }
    public int RemainingFiles { get; set; }
    public long RemainingSize { get; set; }
    public int Progression { get; set; }
    public string CurrentSourceFile { get; set; } = "";
    public string CurrentDestinationFile { get; set; } = "";
}

public class StateFile
{
    private readonly string filePath;
    private static readonly object fileLock = new object();

    public StateFile()
    {
        filePath = Path.Combine(AppContext.BaseDirectory, "state.json");
    }

    public void WriteProcess(WorkState workState)
    {
        lock (fileLock)
        {
            List<WorkState> states = new List<WorkState>();

            if (File.Exists(filePath))
            {
                string existingContent = File.ReadAllText(filePath);
                states = JsonSerializer.Deserialize<List<WorkState>>(existingContent) ?? new List<WorkState>();
            }

            int index = states.FindIndex(s => s.WorkName == workState.WorkName);

            if (index >= 0)
            {
                states[index] = workState;
            }
            else
            {
                states.Add(workState);
            }

            string json = JsonSerializer.Serialize(states, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }

    public List<WorkState> ReadAllStates()
    {
        lock (fileLock)
        {
            if (!File.Exists(filePath))
            {
                return [];
            }

            string existingContent = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<WorkState>>(existingContent) ?? [];
        }
    }
}
