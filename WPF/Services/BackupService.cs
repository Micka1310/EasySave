using System.IO;
using EasyLog;
using WorkFile;

namespace EasySave.WPF.Services;

/// <summary>
/// Service de sauvegarde réutilisé par le ViewModel : exécute Full ou Differential
/// avec callback de progression et écrit dans EasyLog (logs journaliers + state.json).
/// Reproduit la logique de Console.ViewModel.Strategy.ExecuteWork3 pour rester
/// 100 % compatible avec la v1.
/// </summary>
public class BackupService
{
    private readonly Logger _logger = new Logger();
    private readonly StateFile _stateFile = new StateFile();

    public bool ExecuteWork(Work work, IProgress<WorkState> progress, List<string> errors)
    {
        return work.GetWorkType() == "1"
            ? ExecuteFullBackup(work, progress, errors)
            : ExecuteDifferentialBackup(work, progress, errors);
    }

    private bool ExecuteFullBackup(Work work, IProgress<WorkState> progress, List<string> errors)
    {
        if (!Directory.Exists(work.GetSourceDirectory()))
        {
            errors.Add($"Dossier source introuvable : {work.GetSourceDirectory()}");
            return false;
        }

        string[] files = Directory.GetFiles(work.GetSourceDirectory(), "*", SearchOption.AllDirectories);
        return CopyFiles(work, files, progress, errors);
    }

    private bool ExecuteDifferentialBackup(Work work, IProgress<WorkState> progress, List<string> errors)
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

        return CopyFiles(work, filesToCopy.ToArray(), progress, errors);
    }

    private bool CopyFiles(Work work, string[] files, IProgress<WorkState> progress, List<string> errors)
    {
        int totalFiles = files.Length;
        int remainingFiles = totalFiles;
        long totalSize = files.Sum(f => new FileInfo(f).Length);
        long remainingSize = totalSize;
        bool success = true;

        // Émet un état initial pour que l'UI affiche la barre/quantité dès le départ.
        progress.Report(new WorkState
        {
            WorkName = work.GetName(),
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Status = totalFiles > 0 ? "Active" : "Inactive",
            TotalFiles = totalFiles,
            TotalSize = totalSize,
            RemainingFiles = remainingFiles,
            RemainingSize = remainingSize,
            Progression = 0,
            CurrentSourceFile = "",
            CurrentDestinationFile = ""
        });

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
            int progression = totalFiles > 0 ? (int)((totalFiles - remainingFiles) * 100L / totalFiles) : 100;

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

            _stateFile.WriteProcess(state);
            progress.Report(state);
            _logger.WriteLogs(work.GetName(), sourceFile, destinationFile, fileSize, transferTime, fileSuccess, errorMsg);
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
        bool sizeDiffers = sourceInfo.Length != destinationInfo.Length;
        bool timestampDiffers = sourceInfo.LastWriteTimeUtc != destinationInfo.LastWriteTimeUtc;
        return sizeDiffers || timestampDiffers;
    }
}
