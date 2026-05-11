namespace ChangeLogFormat6File;

using ConsoleStrategyFile;
using EasyLog;
using LanguageFile;
using WorkListFile;

/// <summary>Option 6 : format des fichiers journaux (JSON ou XML), v1.1.</summary>
public class ChangeLogFormat6 : IStrategy
{
    public string option => Language.GetInstance().GetString("option_log_format");
    public List<string> parameterMessage => [
        Language.GetInstance().GetString("log_format_choice")
    ];

    public string Execution(List<string> parameters, WorkList workList)
    {
        Language lang = Language.GetInstance();

        if (parameters.Count < 1)
        {
            return lang.GetString("invalid_option");
        }

        return parameters[0].Trim() switch
        {
            "1" => SetFormat(LogFormat.Json, lang.GetString("log_format_changed_json")),
            "2" => SetFormat(LogFormat.Xml, lang.GetString("log_format_changed_xml")),
            _ => lang.GetString("invalid_option"),
        };
    }

    private static string SetFormat(LogFormat format, string message)
    {
        LogFormatSettings.Current = format;
        return message;
    }
}
