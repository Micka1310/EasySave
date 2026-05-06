namespace ConsoleStrategyFile;

using System.Globalization;
using WorkListFile;
using WorkFile;
using LanguageFile;
using LogFileLib;
using StateFileLib;

public interface IStrategy
{
    string option { get; }
    List<string> parameterMessage { get; }
    public string Execution(List<string> parameters, WorkList workList);
}

// Option 1 : display all the works
public class DisplayWork1 : IStrategy
{
    public string option => Language.GetInstance().GetString("option_display");
    public List<string> parameterMessage => [];

    public string Execution(List<string> parameters, WorkList workList)
    {
        Language lang = Language.GetInstance();
        string displayString = "";
        int index = 0;

        List<Work> WorkList = workList.GetWork();

        foreach (Work elem in WorkList)
        {
            index++;
            string typeLabel = elem.GetWorkType() == "1"
                ? lang.GetString("backup_type_short_full")
                : lang.GetString("backup_type_short_diff");

            displayString += lang.GetString("display_work_title") + $"{index} :\n"
                + lang.GetString("display_file_name") + elem.GetName() + "\n"
                + lang.GetString("display_source") + elem.GetSourceDirectory() + "\n"
                + lang.GetString("display_destination") + elem.GetDestinationDirectory() + "\n"
                + lang.GetString("display_type") + typeLabel + "\n\n ";
        }

        return displayString;
    }
}

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

        LogFile logFile = new LogFile();
        logFile.WriteLogs(name, source, destination, 0, 0);

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

// Option 3 : exécuter un ou plusieurs travaux de sauvegarde (avec progression temps réel)
public class ExecuteWork3 : IStrategy
{
    public string option => Language.GetInstance().GetString("option_execute");
    public List<string> parameterMessage => [
        Language.GetInstance().GetString("execute_input")
    ];

    /// <summary>Callback appelé à chaque fichier copié pour afficher la progression en console.</summary>
    public Action<WorkState>? OnProgress { get; set; }

    public string Execution(List<string> parameters, WorkList workList)
    {
        Language lang = Language.GetInstance();
        int workCount = workList.GetWork().Count;

        if (parameters.Count < 1)
        {
            return lang.GetString("error_empty_execute_input");
        }

        string rawInput = parameters[0].Trim();
        if (string.IsNullOrEmpty(rawInput))
        {
            return lang.GetString("error_empty_execute_input");
        }

        if (workCount == 0)
        {
            return lang.GetString("error_no_works_to_execute");
        }

        if (!TryParseIndexes(rawInput, workCount, out List<int> indexes, out string? errorKey))
        {
            return lang.GetString(errorKey!);
        }

        bool success = true;

        foreach (int index in indexes)
        {
            Work work = workList.GetWork()[index];

            if (work.GetWorkType() == "1")
            {
                if (!ExecuteFullBackup(work)) success = false;
            }
            else
            {
                if (!ExecuteDifferentialBackup(work)) success = false;
            }
        }

        return success.ToString().ToLowerInvariant();
    }

    private static bool TryParseIndexes(string input, int workCount, out List<int> indexes, out string? errorKey)
    {
        indexes = [];
        errorKey = null;
        input = input.Trim();

        if (string.IsNullOrEmpty(input))
        {
            errorKey = "error_empty_execute_input";
            return false;
        }

        string[] tokens = input.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string token in tokens)
        {
            if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int num))
            {
                errorKey = "error_invalid_execute_format";
                return false;
            }

            int i = num - 1;
            if (i >= 0 && i < workCount)
            {
                indexes.Add(i);
            }
        }

        indexes = indexes.Distinct().ToList();

        if (indexes.Count == 0)
        {
            errorKey = "error_invalid_work_selection";
            return false;
        }

        return true;
    }

    private bool ExecuteFullBackup(Work work)
    {
        if (!Directory.Exists(work.GetSourceDirectory()))
        {
            return false;
        }

        string[] files = Directory.GetFiles(work.GetSourceDirectory(), "*", SearchOption.AllDirectories);

        int totalFiles = files.Length;
        int remainingFiles = totalFiles;
        long totalSize = files.Sum(f => new FileInfo(f).Length);
        long remainingSize = totalSize;
        bool success = true;

        StateFile stateFile = new StateFile();
        LogFile logFile = new LogFile();

        foreach (string sourceFile in files)
        {
            string relativePath = Path.GetRelativePath(work.GetSourceDirectory(), sourceFile);
            string destinationFile = Path.Combine(work.GetDestinationDirectory(), relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

            long fileSize = new FileInfo(sourceFile).Length;
            long transferTime;

            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                File.Copy(sourceFile, destinationFile, true);
                watch.Stop();
                transferTime = watch.ElapsedMilliseconds;
            }
            catch
            {
                transferTime = -1;
                success = false;
            }

            remainingFiles--;
            remainingSize -= fileSize;
            int progression = totalFiles > 0 ? (int)((totalFiles - remainingFiles) * 100 / totalFiles) : 100;

            WorkState state = new WorkState
            {
                WorkName = work.GetName(),
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = remainingFiles > 0 ? "Active" : "Inactive",
                TotalFiles = totalFiles,
                TotalSize = totalSize,
                RemainingFiles = remainingFiles,
                RemainingSize = remainingSize,
                Progression = progression,
                CurrentSourceFile = sourceFile,
                CurrentDestinationFile = destinationFile
            };

            stateFile.WriteProcess(state);
            OnProgress?.Invoke(state);
            logFile.WriteLogs(work.GetName(), sourceFile, destinationFile, fileSize, transferTime);
        }

        return success;
    }

    private bool ExecuteDifferentialBackup(Work work)
    {
        if (!Directory.Exists(work.GetSourceDirectory()))
        {
            return false;
        }

        string[] files = Directory.GetFiles(work.GetSourceDirectory(), "*", SearchOption.AllDirectories);
        List<string> filesToCopy = [];

        foreach (string sourceFile in files)
        {
            string relativePath = Path.GetRelativePath(work.GetSourceDirectory(), sourceFile);
            string destinationFile = Path.Combine(work.GetDestinationDirectory(), relativePath);

            if (!File.Exists(destinationFile) ||
                File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(destinationFile))
            {
                filesToCopy.Add(sourceFile);
            }
        }

        int totalFiles = filesToCopy.Count;
        int remainingFiles = totalFiles;
        long totalSize = filesToCopy.Sum(f => new FileInfo(f).Length);
        long remainingSize = totalSize;
        bool success = true;

        StateFile stateFile = new StateFile();
        LogFile logFile = new LogFile();

        foreach (string sourceFile in filesToCopy)
        {
            string relativePath = Path.GetRelativePath(work.GetSourceDirectory(), sourceFile);
            string destinationFile = Path.Combine(work.GetDestinationDirectory(), relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

            long fileSize = new FileInfo(sourceFile).Length;
            long transferTime;

            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                File.Copy(sourceFile, destinationFile, true);
                watch.Stop();
                transferTime = watch.ElapsedMilliseconds;
            }
            catch
            {
                transferTime = -1;
                success = false;
            }

            remainingFiles--;
            remainingSize -= fileSize;
            int progression = totalFiles > 0 ? (int)((totalFiles - remainingFiles) * 100 / totalFiles) : 100;

            WorkState state = new WorkState
            {
                WorkName = work.GetName(),
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = remainingFiles > 0 ? "Active" : "Inactive",
                TotalFiles = totalFiles,
                TotalSize = totalSize,
                RemainingFiles = remainingFiles,
                RemainingSize = remainingSize,
                Progression = progression,
                CurrentSourceFile = sourceFile,
                CurrentDestinationFile = destinationFile
            };

            stateFile.WriteProcess(state);
            OnProgress?.Invoke(state);
            logFile.WriteLogs(work.GetName(), sourceFile, destinationFile, fileSize, transferTime);
        }

        return success;
    }
}

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
