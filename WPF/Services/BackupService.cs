using System.IO;
using System.Diagnostics;
using System.Globalization;
using CryptoSoft;
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
    private readonly GeneralSettingsService _settingsService = new GeneralSettingsService();

    public bool ExecuteWork(
        Work work,
        IProgress<WorkState> progress,
        List<string> errors,
        CancellationToken cancellationToken = default,
        Func<bool>? isPaused = null)
    {
        return work.GetWorkType() == "1"
            ? ExecuteFullBackup(work, progress, errors, cancellationToken, isPaused)
            : ExecuteDifferentialBackup(work, progress, errors, cancellationToken, isPaused);
    }

    private bool ExecuteFullBackup(
        Work work,
        IProgress<WorkState> progress,
        List<string> errors,
        CancellationToken cancellationToken,
        Func<bool>? isPaused)
    {
        if (!Directory.Exists(work.GetSourceDirectory()))
        {
            errors.Add($"Dossier source introuvable : {work.GetSourceDirectory()}");
            return false;
        }

        string[] files = Directory.GetFiles(work.GetSourceDirectory(), "*", SearchOption.AllDirectories);
        return CopyFiles(work, files, progress, errors, cancellationToken, isPaused);
    }

    private bool ExecuteDifferentialBackup(
        Work work,
        IProgress<WorkState> progress,
        List<string> errors,
        CancellationToken cancellationToken,
        Func<bool>? isPaused)
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

        return CopyFiles(work, filesToCopy.ToArray(), progress, errors, cancellationToken, isPaused);
    }

    private static void WaitWhilePaused(CancellationToken cancellationToken, Func<bool>? isPaused)
    {
        if (isPaused is null)
        {
            return;
        }

        while (isPaused())
        {
            cancellationToken.ThrowIfCancellationRequested();
            Thread.Sleep(50);
        }
    }

    private bool CopyFiles(
        Work work,
        string[] files,
        IProgress<WorkState> progress,
        List<string> errors,
        CancellationToken cancellationToken,
        Func<bool>? isPaused)
    {
        HashSet<string> encryptedExtensions = LoadEncryptedExtensions();
        int totalFiles = files.Length;
        int remainingFiles = totalFiles;
        long totalSize = files.Sum(f => new FileInfo(f).Length);
        long remainingSize = totalSize;
        bool success = true;

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

        try
        {
            foreach (string sourceFile in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                WaitWhilePaused(cancellationToken, isPaused);

                int progressionBeforeFile = totalFiles > 0 ? (int)((totalFiles - remainingFiles) * 100L / totalFiles) : 100;
                progress.Report(new WorkState
                {
                    WorkName = work.GetName(),
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = "Active",
                    TotalFiles = totalFiles,
                    TotalSize = totalSize,
                    RemainingFiles = remainingFiles,
                    RemainingSize = remainingSize,
                    Progression = progressionBeforeFile,
                    CurrentSourceFile = "",
                    CurrentDestinationFile = ""
                });

                string relativePath = Path.GetRelativePath(work.GetSourceDirectory(), sourceFile);
                string destinationFile = Path.Combine(work.GetDestinationDirectory(), relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

                long fileSize = new FileInfo(sourceFile).Length;
                long transferTime;
                long encryptionTime = 0;
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

                if (fileSuccess && ShouldEncrypt(sourceFile, encryptedExtensions))
                {
                    try
                    {
                        encryptionTime = EncryptWithCryptoSoft(destinationFile);
                    }
                    catch (CryptoSoftException ex)
                    {
                        encryptionTime = ex.ErrorCode;
                        fileSuccess = false;
                        errorMsg = string.IsNullOrWhiteSpace(errorMsg)
                            ? $"Erreur chiffrement : {ex.Message}"
                            : $"{errorMsg} | Erreur chiffrement : {ex.Message}";
                        errors.Add($"{Path.GetFileName(sourceFile)} : erreur chiffrement - {ex.Message}");
                        success = false;
                    }
                    catch (Exception ex)
                    {
                        encryptionTime = -1;
                        fileSuccess = false;
                        errorMsg = string.IsNullOrWhiteSpace(errorMsg)
                            ? $"Erreur chiffrement : {ex.Message}"
                            : $"{errorMsg} | Erreur chiffrement : {ex.Message}";
                        errors.Add($"{Path.GetFileName(sourceFile)} : erreur chiffrement - {ex.Message}");
                        success = false;
                    }
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
                _logger.WriteLogs(work.GetName(), sourceFile, destinationFile, fileSize, transferTime, encryptionTime, fileSuccess, errorMsg);
            }
        }
        catch (OperationCanceledException)
        {
            return false;
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

    private HashSet<string> LoadEncryptedExtensions()
    {
        GeneralSettings settings = _settingsService.Load();
        return settings.EncryptedExtensions
            .Select(NormalizeExtension)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool ShouldEncrypt(string sourceFile, HashSet<string> encryptedExtensions)
    {
        string extension = Path.GetExtension(sourceFile);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return encryptedExtensions.Contains(extension);
    }

    private static string NormalizeExtension(string value)
    {
        string trimmed = value.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return string.Empty;
        }

        return trimmed.StartsWith('.') ? trimmed : "." + trimmed;
    }

    private static long EncryptWithCryptoSoft(string destinationFile)
    {
        string baseDir = AppContext.BaseDirectory;
        string[] exeCandidates =
        [
            Path.Combine(baseDir, "CryptoSoft.exe"),
            Path.Combine(baseDir, "CryptoSoft", "CryptoSoft.exe")
        ];

        foreach (string exePath in exeCandidates)
        {
            if (!File.Exists(exePath))
            {
                continue;
            }

            return RunCryptoProcess(exePath, $"--encrypt \"{destinationFile}\"");
        }

        string[] dllCandidates =
        [
            Path.Combine(baseDir, "CryptoSoft.dll"),
            Path.Combine(baseDir, "CryptoSoft", "CryptoSoft.dll")
        ];

        foreach (string dllPath in dllCandidates)
        {
            if (!File.Exists(dllPath))
            {
                continue;
            }

            return RunCryptoProcess("dotnet", $"\"{dllPath}\" --encrypt \"{destinationFile}\"");
        }

        // Fallback de sécurité si le binaire externe n'est pas déployé.
        return CryptoEngine.EncryptFileInPlace(destinationFile);
    }

    private static long RunCryptoProcess(string fileName, string arguments)
    {
        using Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        if (!process.Start())
        {
            throw new CryptoSoftException("Impossible de démarrer CryptoSoft.", -100);
        }

        string stdOut = process.StandardOutput.ReadToEnd().Trim();
        string stdErr = process.StandardError.ReadToEnd().Trim();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            long errorCode = process.ExitCode > 0 ? -process.ExitCode : -1;
            throw new CryptoSoftException(string.IsNullOrWhiteSpace(stdErr)
                ? "CryptoSoft a retourné une erreur."
                : stdErr, errorCode);
        }

        if (long.TryParse(stdOut, NumberStyles.Integer, CultureInfo.InvariantCulture, out long elapsedMs))
        {
            return elapsedMs;
        }

        throw new CryptoSoftException("CryptoSoft n'a pas renvoyé de temps de chiffrement valide.", -101);
    }

    private sealed class CryptoSoftException : Exception
    {
        public CryptoSoftException(string message, long errorCode) : base(message)
        {
            ErrorCode = errorCode < 0 ? errorCode : -1;
        }

        public long ErrorCode { get; }
    }
}
