namespace ConsoleStrategyFile;

using WorkListFile;

public interface IStrategy
{
    string option { get; }
    List<string> parameterMessage { get; }
    public string Execution(List<string> parameters, WorkList workList);
}