using WorkFile;

namespace WorkListFile;

public class WorkList
{
    public const int MaxWorks = 5;

    // Attributes
    private List<Work> works { get; }

    // Constructor
    public WorkList()
    {
        works = new List<Work>();
    }

    // Methods
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
    }
}
