using System.IO;
using System.Diagnostics;
using System.Globalization;
using CryptoSoft;
using EasyLog;
using WorkFile;

namespace EasySave.WPF.Services;

public class BackupService
{
    private readonly Logger _logger = new Logger();
    private readonly StateFile _stateFile = new StateFile();
    private readonly GeneralSettingsService _settingsService = new GeneralSettingsService();
    private readonly object _ioWriteLock = new();

    // Barrière inter-travaux pour la phase prioritaire. Chaque travail
    // décrémente le compteur quand il a terminé ses fichiers prioritaires.
    // Quand le compteur atteint 0, la barrière s'ouvre et les fichiers
    // non prioritaires peuvent commencer sur TOUS les travaux.
    private readonly ManualResetEventSlim _priorityPhaseCompleted = new(true);
    private int _remainingPriorityWorkers;

    // Verrou bande passante : un seul gros fichier (taille > seuil) peut
    // être transféré à la fois, tous travaux confondus.
    private readonly SemaphoreSlim _largeFileGate = new(1, 1);

    public void ConfigureParallelRun(int workCount)
    {
        if (workCount <= 1)
        {
            Interlocked.Exchange(ref _remainingPriorityWorkers, 0);
            _priorityPhaseCompleted.Set();
            return;
        }

        Interlocked.Exchange(ref _remainingPriorityWorkers, workCount);
        _priorityPhaseCompleted.Reset();
    }

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

    // ================================================================
    // CopyFiles — Orchestration principale
    // ================================================================

    private bool CopyFiles(
        Work work,
        string[] files,
        IProgress<WorkState> progress,
        List<string> errors,
        CancellationToken cancellationToken,
        Func<bool>? isPaused)
    {
        HashSet<string> encryptedExtensions = LoadEncryptedExtensions();
        HashSet<string> priorityExtensions = LoadPriorityExtensions();
        long largeFileThresholdBytes = LoadLargeFileThresholdBytes();

        string[] priorityFiles = files.Where(f => IsPriorityFile(f, priorityExtensions)).ToArray();
        string[] nonPriorityFiles = files.Where(f => !IsPriorityFile(f, priorityExtensions)).ToArray();

        int totalFiles = files.Length;
        int remainingFiles = totalFiles;
        long totalSize = files.Sum(f => SafeFileSize(f));
        long remainingSize = totalSize;
        bool success = true;
        bool priorityPhaseSignaled = false;

        progress.Report(MakeState(work, totalFiles, totalSize, remainingFiles, remainingSize, 0,
            totalFiles > 0 ? "Active" : "Inactive", "", ""));

        try
        {
            // ── Phase 1 : fichiers prioritaires ──
            // Traités en mode opportuniste (gros fichiers différés si le
            // verrou bande passante est occupé, petits copiés en attendant).
            success &= ProcessPhaseOpportunistic(
                work, priorityFiles, encryptedExtensions, largeFileThresholdBytes,
                totalFiles, totalSize, ref remainingFiles, ref remainingSize,
                progress, errors, cancellationToken, isPaused);

            // Signale que CE travail a terminé ses fichiers prioritaires.
            CompletePriorityPhase();
            priorityPhaseSignaled = true;

            // Attend que TOUS les travaux aient terminé leur phase prioritaire.
            // Tant qu'il reste des fichiers prioritaires sur au moins un travail,
            // aucun non-prioritaire ne peut être copié.
            _priorityPhaseCompleted.Wait(cancellationToken);

            // ── Phase 2 : fichiers non prioritaires ──
            success &= ProcessPhaseOpportunistic(
                work, nonPriorityFiles, encryptedExtensions, largeFileThresholdBytes,
                totalFiles, totalSize, ref remainingFiles, ref remainingSize,
                progress, errors, cancellationToken, isPaused);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            if (!priorityPhaseSignaled)
            {
                CompletePriorityPhase();
            }
        }

        return success;
    }

    // ================================================================
    // ProcessPhaseOpportunistic — Copie avec gestion bande passante
    // ================================================================
    //
    // Parcourt les fichiers dans l'ordre d'origine.
    //  • Petit fichier (≤ seuil) → copié immédiatement en parallèle.
    //  • Gros fichier (> seuil) :
    //      – Si le verrou bande passante est libre → copié maintenant.
    //      – Sinon → différé et le travail continue avec les fichiers
    //        suivants (petits) pour ne pas gaspiller le temps d'attente.
    // En fin de passe, les gros fichiers différés sont traités en
    // attendant le verrou normalement.

    private bool ProcessPhaseOpportunistic(
        Work work,
        string[] files,
        HashSet<string> encryptedExtensions,
        long largeFileThresholdBytes,
        int totalFiles,
        long totalSize,
        ref int remainingFiles,
        ref long remainingSize,
        IProgress<WorkState> progress,
        List<string> errors,
        CancellationToken cancellationToken,
        Func<bool>? isPaused)
    {
        bool success = true;
        List<string> deferredLargeFiles = new();

        // ── Passe 1 : ordre d'origine, gros différés si verrou occupé ──
        foreach (string sourceFile in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WaitWhilePaused(cancellationToken, isPaused);

            long fileSize = SafeFileSize(sourceFile);
            bool isLarge = largeFileThresholdBytes > 0 && fileSize > largeFileThresholdBytes;

            if (isLarge && !_largeFileGate.Wait(0))
            {
                deferredLargeFiles.Add(sourceFile);
                continue;
            }

            try
            {
                if (!CopyAndReportSingleFile(
                        work, sourceFile, fileSize, totalFiles, totalSize,
                        ref remainingFiles, ref remainingSize,
                        encryptedExtensions, progress, errors))
                {
                    success = false;
                }
            }
            finally
            {
                if (isLarge) _largeFileGate.Release();
            }
        }

        // ── Passe 2 : gros fichiers différés, attente bloquante ──
        foreach (string sourceFile in deferredLargeFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WaitWhilePaused(cancellationToken, isPaused);

            long fileSize = SafeFileSize(sourceFile);
            _largeFileGate.Wait(cancellationToken);
            try
            {
                if (!CopyAndReportSingleFile(
                        work, sourceFile, fileSize, totalFiles, totalSize,
                        ref remainingFiles, ref remainingSize,
                        encryptedExtensions, progress, errors))
                {
                    success = false;
                }
            }
            finally
            {
                _largeFileGate.Release();
            }
        }

        return success;
    }

    // ================================================================
    // CopyAndReportSingleFile — Copie + chiffrement + reporting
    // ================================================================

    private bool CopyAndReportSingleFile(
        Work work,
        string sourceFile,
        long fileSize,
        int totalFiles,
        long totalSize,
        ref int remainingFiles,
        ref long remainingSize,
        HashSet<string> encryptedExtensions,
        IProgress<WorkState> progress,
        List<string> errors)
    {
        int progressionBefore = totalFiles > 0
            ? (int)((totalFiles - remainingFiles) * 100L / totalFiles)
            : 100;

        progress.Report(MakeState(work, totalFiles, totalSize, remainingFiles, remainingSize,
            progressionBefore, "Active", "", ""));

        string relativePath = Path.GetRelativePath(work.GetSourceDirectory(), sourceFile);
        string destinationFile = Path.Combine(work.GetDestinationDirectory(), relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

        long transferTime;
        long encryptionTime = 0;
        bool fileSuccess = true;
        string errorMsg = "";

        try
        {
            Stopwatch watch = Stopwatch.StartNew();
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
            }
            catch (Exception ex)
            {
                encryptionTime = -1;
                fileSuccess = false;
                errorMsg = string.IsNullOrWhiteSpace(errorMsg)
                    ? $"Erreur chiffrement : {ex.Message}"
                    : $"{errorMsg} | Erreur chiffrement : {ex.Message}";
                errors.Add($"{Path.GetFileName(sourceFile)} : erreur chiffrement - {ex.Message}");
            }
        }

        remainingFiles--;
        remainingSize -= fileSize;
        int progression = totalFiles > 0 ? (int)((totalFiles - remainingFiles) * 100L / totalFiles) : 100;

        WorkState state = MakeState(work, totalFiles, totalSize, remainingFiles, remainingSize,
            progression, remainingFiles > 0 ? "Active" : "Inactive", sourceFile, destinationFile);

        lock (_ioWriteLock)
        {
            _stateFile.WriteProcess(state);
        }
        progress.Report(state);
        lock (_ioWriteLock)
        {
            _logger.WriteLogs(work.GetName(), sourceFile, destinationFile, fileSize,
                transferTime, encryptionTime, fileSuccess, errorMsg);
        }

        return fileSuccess;
    }

    // ================================================================
    // Helpers
    // ================================================================

    private static WorkState MakeState(Work work, int totalFiles, long totalSize,
        int remainingFiles, long remainingSize, int progression,
        string status, string currentSource, string currentDest)
    {
        return new WorkState
        {
            WorkName = work.GetName(),
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Status = status,
            TotalFiles = totalFiles,
            TotalSize = totalSize,
            RemainingFiles = remainingFiles,
            RemainingSize = remainingSize,
            Progression = progression,
            CurrentSourceFile = currentSource,
            CurrentDestinationFile = currentDest
        };
    }

    private void CompletePriorityPhase()
    {
        int remaining = Interlocked.Decrement(ref _remainingPriorityWorkers);
        if (remaining <= 0)
        {
            _priorityPhaseCompleted.Set();
        }
    }

    private static long SafeFileSize(string path)
    {
        try { return new FileInfo(path).Length; }
        catch { return 0; }
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

    private HashSet<string> LoadPriorityExtensions()
    {
        GeneralSettings settings = _settingsService.Load();
        return settings.PriorityExtensions
            .Select(NormalizeExtension)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private long LoadLargeFileThresholdBytes()
    {
        GeneralSettings settings = _settingsService.Load();
        int kb = settings.LargeFileThresholdKB;
        return kb > 0 ? (long)kb * 1024L : 0L;
    }

    private static bool IsPriorityFile(string sourceFile, HashSet<string> priorityExtensions)
    {
        if (priorityExtensions.Count == 0)
        {
            return false;
        }

        string extension = Path.GetExtension(sourceFile);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return priorityExtensions.Contains(extension);
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

        return CryptoEngine.EncryptFileInPlace(destinationFile);
    }

    private const int CryptoSoftBusyExitCode = 4;
    private const int CryptoSoftMaxRetries = 10;
    private const int CryptoSoftRetryDelayMs = 500;

    private static long RunCryptoProcess(string fileName, string arguments)
    {
        for (int attempt = 0; attempt <= CryptoSoftMaxRetries; attempt++)
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

            if (process.ExitCode == CryptoSoftBusyExitCode)
            {
                if (attempt < CryptoSoftMaxRetries)
                {
                    Thread.Sleep(CryptoSoftRetryDelayMs);
                    continue;
                }

                throw new CryptoSoftException(
                    "CryptoSoft est déjà en cours d'exécution (mono-instance). Nombre maximal de tentatives atteint.",
                    -CryptoSoftBusyExitCode);
            }

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

        throw new CryptoSoftException("CryptoSoft mono-instance : échec après toutes les tentatives.", -CryptoSoftBusyExitCode);
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
