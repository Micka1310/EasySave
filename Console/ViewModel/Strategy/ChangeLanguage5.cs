namespace ChangeLanguage5File;

using ConsoleStrategyFile;
using LanguageFile;
using EasyLog;

// Option 5 : change the language
public class ChangeLanguage5 : IStrategy
{
    public string option => Language.GetInstance().GetString("option_language");
    public List<string> parameterMessage => [
        Language.GetInstance().GetString("language_choice")
    ];

    public string Execution(List<string> parameters, WorkList workList)
    {
        Language lang = Language.GetInstance();

        if (parameters.Count < 1)
        {
            return lang.GetString("invalid_option");
        }

        switch (parameters[0].Trim())
        {
            case "1":
                lang.SetLanguage(Lang.FR);
                return lang.GetString("language_changed_to_fr");
            case "2":
                lang.SetLanguage(Lang.EN);
                return lang.GetString("language_changed_to_en");
            default:
                return lang.GetString("invalid_option");
        }
    }
}

public static class BackupProgressHelper
{
    public static string FormatBytes(long bytes, Lang lang)
    {
        if (bytes < 0) bytes = 0;

        string[] units = lang == Lang.FR
            ? ["o", "Ko", "Mo", "Go", "To"]
            : ["B", "KB", "MB", "GB", "TB"];

        double v = bytes;
        int u = 0;
        while (v >= 1024 && u < units.Length - 1)
        {
            v /= 1024;
            u++;
        }

        return $"{v.ToString("0.##", CultureInfo.InvariantCulture)} {units[u]}";
    }
}
