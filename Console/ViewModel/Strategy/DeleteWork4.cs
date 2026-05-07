namespace DeleteWork4File;

using ConsoleStrategyFile;
using System.Globalization;
using WorkListFile;
using WorkFile;
using LanguageFile;

// Option 4 : supprimer un travail
public class DeleteWork4 : IStrategy
{
    public string option => Language.GetInstance().GetString("option_delete");
    public List<string> parameterMessage => [
        Language.GetInstance().GetString("delete_input")
    ];

    public string Execution(List<string> parameters, WorkList workList)
    {
        Language lang = Language.GetInstance();

        if (workList.GetWork().Count == 0)
        {
            return lang.GetString("delete_no_jobs");
        }

        if (parameters.Count < 1)
        {
            return lang.GetString("delete_invalid");
        }

        string raw = parameters[0].Trim();
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int num))
        {
            return lang.GetString("delete_invalid");
        }

        int index = num - 1;
        if (!workList.RemoveWork(index))
        {
            return lang.GetString("delete_invalid");
        }

        return lang.GetString("delete_success");
    }
}