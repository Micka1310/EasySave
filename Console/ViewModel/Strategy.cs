namespace ConsoleStrategyFile;

using WorkListFile;
using WorkFile;
using LanguageFile;

// Interface for creating strategies
public interface IStrategy
{
    // Attributs
    string option { get; }
    List<string> parameterMessage { get; }

    // Methods
    public string Execution(List<string> parameters, WorkList workList);
}

// Option 1 : display all the works
public class DisplayWork1 : IStrategy
{
    // Attributes
    public string option => Language.GetInstance().GetString("option_display");
    public List<string> parameterMessage => [];

    // Methods
    public string Execution(List<string> parameters, WorkList workList)
    {
        Language lang = Language.GetInstance();
        string displayString = "";
        int index = 0;

        List<Work> WorkList = workList.GetWork();

        foreach (Work elem in WorkList)
        {
            index = index + 1;
            displayString = displayString
                + lang.GetString("display_work_title") + $"{index} :\n"
                + lang.GetString("display_file_name") + elem.GetName() + "\n"
                + lang.GetString("display_source") + elem.GetSourceDirectory() + "\n"
                + lang.GetString("display_destination") + elem.GetDestinationDirectory() + "\n"
                + lang.GetString("display_type") + elem.GetWorkType() + "\n\n ";
        }

        return displayString;
    }
}

// option 2 : create a new work
public class CreateWork2 : IStrategy
{
    // Attributes
    public string option => Language.GetInstance().GetString("option_create");
    public List<string> parameterMessage => [
        Language.GetInstance().GetString("create_name"),
        Language.GetInstance().GetString("create_source"),
        Language.GetInstance().GetString("create_destination"),
        Language.GetInstance().GetString("create_type")
    ];

    // Methods
    public string Execution(List<string> parameters, WorkList workList)
    {
        workList.AddWork(parameters);

        return Language.GetInstance().GetString("work_saved");
    }
}

// option 3 : execute a work
public class ExecuteWork3 : IStrategy
{
    // Attributes
    public string option => Language.GetInstance().GetString("option_execute");
    public List<string> parameterMessage => [
        Language.GetInstance().GetString("execute_input")
    ];

    // Methods
    public string Execution(List<string> parameters, WorkList workList)
    {
        // Waiting for the others to commit their works...

        return "";
    }
}

// option 4 : change the language
public class ChangeLanguage4 : IStrategy
{
    // Attributes
    public string option => Language.GetInstance().GetString("option_language");
    public List<string> parameterMessage => [
        Language.GetInstance().GetString("language_choice")
    ];

    // Methods
    public string Execution(List<string> parameters, WorkList workList)
    {
        Language lang = Language.GetInstance();

        switch (parameters[0])
        {
            case "1":
                lang.SetLanguage(Lang.FR);
                return lang.GetString("language_changed");
            case "2":
                lang.SetLanguage(Lang.EN);
                return lang.GetString("language_changed");
            default:
                return lang.GetString("invalid_option");
        }
    }
}
