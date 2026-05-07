namespace ExecuteWork3File;

using ConsoleStrategyFile;
using System.Globalization;
using WorkListFile;
using WorkFile;
using LanguageFile;
using EasyLog;

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
        List<string> errors = [];

        foreach (int index in indexes)
        {
            Work work = workList.GetWork()[index];
            List<string> jobErrors = [];

            if (work.GetWorkType() == "1")
            {
                if (!ExecuteFullBackup(work, jobErrors)) success = false;
            }
            else
            {
                if (!ExecuteDifferentialBackup(work, jobErrors)) success = false;
            }

            foreach (string err in jobErrors)
            {
                errors.Add($"[{work.GetName()}] {err}");
            }
        }

        if (success)
        {
            return "true";
        }

        string summary = "false\n";
        foreach (string err in errors)
        {
            summary += $"  - {err}\n";
        }
        return summary.TrimEnd();
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

    private bool ExecuteFullBackup(Work work, List<string> errors)
    {
        if (!Directory.Exists(work.GetSourceDirectory()))
        {
            errors.Add($"Dossier source introuvable : {work.GetSourceDirectory()}");
            return false;
        }

        string[] files = Directory.GetFiles(work.GetSourceDirectory(), "*", SearchOption.AllDirectories);

        int totalFiles = files.Length;
        int remainingFiles = totalFiles;
        long totalSize = files.Sum(f => new FileInfo(f).Length);
        long remainingSize = totalSize;
        bool success = true;

        StateFile stateFile = new StateFile();
        Logger logger = new Logger();

        foreach (string sourceFile in files)
        {
            string relativePath = Path.GetRelativePath(work.GetSourceDirectory(), sourceFile);
            string destinationFile = Path.Combine(work.GetDestinationDirectory(), relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

            long fileSize = new FileInfo(sourceFile).Length;
            long transferTime;
            bool fileSuccess = true;
            string errorMsg = "";

            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                File.Copy(sourceFile, destinationFile, true);
                watch.Stop();
                transferTime = watch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                transferTime = -1;
                fileSuccess = false;
                errorMsg = ex.Message;
                errors.Add($"{Path.GetFileName(sourceFile)} : {ex.Message}");
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
            logger.WriteLogs(work.GetName(), sourceFile, destinationFile, fileSize, transferTime, fileSuccess, errorMsg);
        }

        return success;
    }

    private bool ExecuteDifferentialBackup(Work work, List<string> errors)
    {
        if (!Directory.Exists(work.GetSourceDirectory()))
        {
            errors.Add($"Dossier source introuvable : {work.GetSourceDirectory()}");
            return false;
        }

        string[] files = Directory.GetFiles(work.GetSourceDirectory(), "*", SearchOption.AllDirectories);
        List<string> filesToCopy = [];

        foreach (string sourceFile in files)
        {
            string relativePath = Path.GetRelativePath(work.GetSourceDirectory(), sourceFile);
            string destinationFile = Path.Combine(work.GetDestinationDirectory(), relativePath);

            if (ShouldCopyInDifferential(sourceFile, destinationFile))
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
        Logger logger = new Logger();

        foreach (string sourceFile in filesToCopy)
        {
            string relativePath = Path.GetRelativePath(work.GetSourceDirectory(), sourceFile);
            string destinationFile = Path.Combine(work.GetDestinationDirectory(), relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

            long fileSize = new FileInfo(sourceFile).Length;
            long transferTime;
            bool fileSuccess = true;
            string errorMsg = "";

            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                File.Copy(sourceFile, destinationFile, true);
                watch.Stop();
                transferTime = watch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                transferTime = -1;
                fileSuccess = false;
                errorMsg = ex.Message;
                errors.Add($"{Path.GetFileName(sourceFile)} : {ex.Message}");
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
            logger.WriteLogs(work.GetName(), sourceFile, destinationFile, fileSize, transferTime, fileSuccess, errorMsg);
        }

        return success;
    }

    private static bool ShouldCopyInDifferential(string sourceFile, string destinationFile)
    {
        if (!File.Exists(destinationFile))
        {
            return true;
        }

        FileInfo sourceInfo = new FileInfo(sourceFile);
        FileInfo destinationInfo = new FileInfo(destinationFile);

        // Differential mode should copy when source and destination differ.
        // Relying only on "source newer than destination" can miss valid changes
        // (for example, overwritten files with atypical timestamps).
        bool sizeDiffers = sourceInfo.Length != destinationInfo.Length;
        bool timestampDiffers = sourceInfo.LastWriteTimeUtc != destinationInfo.LastWriteTimeUtc;

        return sizeDiffers || timestampDiffers;
    }
}