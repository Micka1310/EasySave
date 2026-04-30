namespace WorkFile;

public class Work
{
    // Attributes
    private string name { get; }
    private string sourceDirectory { get; }
    private string destinationDirectory { get; }
    private string type { get; }

    // Constructor
    public Work(string Name, string SourceDirectory, string DestinationDirectory, string Type)
    {
        name = Name;
        sourceDirectory = SourceDirectory;
        destinationDirectory = DestinationDirectory;
        type = Type;
    }

    // Methods
    public string GetName()
    {
        return name;
    }

    public string GetSourceDirectory()
    {
        return sourceDirectory;
    }

    public string GetDestinationDirectory()
    {
        return destinationDirectory;
    }

    public string GetWorkType()
    {
        return type;
    }
}