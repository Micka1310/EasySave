using WorkFile;

namespace WorkListFile;

public class WorkList
{
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

    public void AddWork(List<string> parameter)
    {
        Work newWork = new Work(parameter[0], parameter[1], parameter[2], parameter[3]);
        works.Add(newWork);
    }

    public void DeleteWork(string workToDelete)
    {
        foreach (Work work in works)
        {
            if (work.GetName() == workToDelete)
            {
                works.Remove(work);
                return;
            }
        }
    }
}
