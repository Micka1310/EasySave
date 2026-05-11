namespace CreateWork2File;

using ConsoleStrategyFile;
using System.Globalization;
using WorkListFile;
using WorkFile;
using LanguageFile;
using EasyLog;

// Option 2 : créer un nouveau travail de sauvegarde
public class CreateWork2 : IStrategy
{
    public string option => Language.GetInstance().GetString("option_create");
    public List<string> parameterMessage => [
        Language.GetInstance().GetString("create_name"),
        Language.GetInstance().GetString("create_source"),
        Language.GetInstance().GetString("create_destination"),
        Language.GetInstance().GetString("create_type")
    ];

    public string Execution(List<string> parameters, WorkList workList)
    {
        Language lang = Language.GetInstance();

        if (workList.IsFull())
        {
            return lang.GetString("work_max_reached");
        }

        if (parameters.Count < 4)
        {
            return lang.GetString("error_missing_create_parameters");
        }

        string name = parameters[0].Trim();
        string source = parameters[1].Trim();
        string destination = parameters[2].Trim();
        string typeRaw = parameters[3].Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return lang.GetString("error_empty_work_name");
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            return lang.GetString("error_empty_source");
        }

        if (string.IsNullOrWhiteSpace(destination))
        {
            return lang.GetString("error_empty_destination");
        }

        if (!Directory.Exists(source))
        {
            return lang.GetString("error_source_not_found");
        }

        if (!Directory.Exists(destination))
        {
            return lang.GetString("error_destination_not_found");
        }

        string sourceFullPath = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string destinationFullPath = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.Equals(sourceFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
        {
            return lang.GetString("error_same_source_destination");
        }

        if (!TryNormalizeBackupType(typeRaw, out string typeNormalized))
        {
            return lang.GetString("error_invalid_backup_type");
        }

        List<string> validated = [name, source, destination, typeNormalized];
        workList.AddWork(validated);

        StateFile stateFile = new StateFile();
        stateFile.WriteProcess(new WorkState
        {
            WorkName = name,
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            Status = "Inactive",
            TotalFiles = 0,
            TotalSize = 0,
            RemainingFiles = 0,
            RemainingSize = 0,
            Progression = 0,
            CurrentSourceFile = source,
            CurrentDestinationFile = destination
        });

        Logger logger = new Logger();
        logger.WriteLogs(name, source, destination, 0, 0, 0);

        return lang.GetString("work_saved");
    }

    private static bool TryNormalizeBackupType(string raw, out string normalized)
    {
        normalized = "";
        string s = raw.Trim();
        if (s == "1") { normalized = "1"; return true; }
        if (s == "2") { normalized = "2"; return true; }

        string lower = s.ToLowerInvariant();
        if (lower is "complet" or "full") { normalized = "1"; return true; }
        if (lower is "différentielle" or "differentielle" or "differential" or "diff") { normalized = "2"; return true; }
        if (lower.Contains("complet", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("full", StringComparison.OrdinalIgnoreCase)) { normalized = "1"; return true; }
        if (lower.Contains("différ", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("differ", StringComparison.OrdinalIgnoreCase)) { normalized = "2"; return true; }

        return false;
    }
}
