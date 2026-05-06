namespace ControllerFile;

using ConsoleStrategyFile;
using LanguageFile;
using StateFileLib;
using System.Text;
using WorkFile;
using WorkListFile;

public class Controller
{
	private List<IStrategy> strategiesList { get; }
	private WorkList workList { get; }

	public Controller()
	{
        strategiesList =
        [
            new DisplayWork1(),
			new CreateWork2(),
			new ExecuteWork3(),
			new DeleteWork4(),
			new ChangeLanguage5()
        ];

        workList = new WorkList();
	}

    public string GetNumberedWorksSummary()
    {
        Language lang = Language.GetInstance();
        List<Work> works = workList.GetWork();

        if (works.Count == 0)
        {
            return "  " + lang.GetString("execute_no_jobs_yet");
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(lang.GetString("execute_jobs_header"));

        for (int i = 0; i < works.Count; i++)
        {
            Work w = works[i];
            string typeLabel = w.GetWorkType() == "1"
                ? lang.GetString("backup_type_short_full")
                : lang.GetString("backup_type_short_diff");
            sb.AppendLine($"  {i + 1}. {w.GetName()}  [{typeLabel}]");
            sb.AppendLine($"     ← {w.GetSourceDirectory()}");
            sb.AppendLine($"     → {w.GetDestinationDirectory()}");
        }

        return sb.ToString().TrimEnd();
    }

	public List<string> GetOption()
	{
		List<string> listOption = [];

		foreach (IStrategy strategy in strategiesList)
		{
			listOption.Add(strategy.option);
		}

		return listOption;
	}

	public List<string> GetParameterMessage(int input)
	{
		int realInput = input - 1;
		return strategiesList[realInput].parameterMessage;
    }

	public string OptionExecuted(int input, List<string> parameter)
	{
        int realInput = input - 1;
		return strategiesList[realInput].Execution(parameter, workList);
    }

    /// <summary>
    /// Permet au ConsoleView de brancher un callback de progression sur ExecuteWork3.
    /// </summary>
    public void SetProgressCallback(Action<WorkState>? callback)
    {
        foreach (IStrategy s in strategiesList)
        {
            if (s is ExecuteWork3 exec)
            {
                exec.OnProgress = callback;
            }
        }
    }
}
