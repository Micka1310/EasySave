namespace ControllerFile;

using ConsoleStrategyFile;
using WorkListFile;

public class Controller
{
	// Attributes
	private List<IStrategy> strategiesList { get; }
	private WorkList workList { get; }

	// Constructor
	public Controller()
	{
		// Strategies created
        strategiesList = new List<IStrategy>
        {
            new DisplayWork1(),
			new CreateWork2(),
			new ExecuteWork3(),
			new ChangeLanguage4(),
			new DeleteWork5()
        };

        workList = new WorkList();
	}

	// Methods
	// Get all the options
	public List<string> GetOption()
	{
		List<string> listOption = new List<string>();

		foreach (IStrategy strategy in strategiesList)
		{
			listOption.Add(strategy.option);
		}

		return listOption;
	}

	// Get all the message for inputing parameters
	public List<string> GetParameterMessage(int input)
	{
		int realInput = input - 1;

		if (realInput + 1 > strategiesList.Count() || realInput < 0)
		{
			List<string> error = new List<string>();
			error.Add("error");

            return error;
		}

		return strategiesList[realInput].parameterMessage;
    }

	// Execute the option
	public string OptionExecuted(int input, List<string> parameter)
	{
        int realInput = input - 1;
		string result = strategiesList[realInput].Execution(parameter, workList);

		return result;
    }
}
