using System.Windows.Input;
using EasyLog;
using LanguageFile;
using WorkFile;

namespace EasySave.WPF.ViewModels;

public class WorkItemViewModel : ViewModelBase
{
    public Work Work { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public string Name => Work.GetName();
    public string Source => Work.GetSourceDirectory();
    public string Destination => Work.GetDestinationDirectory();
    public string TypeRaw => Work.GetWorkType();
    public bool IsFullBackup => TypeRaw == "1";

    private string _statusKey = "Inactive";
    public string StatusKey
    {
        get => _statusKey;
        set
        {
            if (SetField(ref _statusKey, value))
            {
                OnPropertyChanged(nameof(StatusDisplay));
                OnPropertyChanged(nameof(CanPause));
                OnPropertyChanged(nameof(CanResume));
                OnPropertyChanged(nameof(CanStop));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string StatusDisplay => LocalizeStatus(_statusKey);
    public bool CanPause => _statusKey == "Active";
    public bool CanResume => _statusKey == "Paused" && !_pausedByBusinessSoftware;
    public bool CanStop => _statusKey is "Active" or "Paused";

    private volatile bool _pauseRequested;
    public bool PauseRequested
    {
        get => _pauseRequested;
        set => _pauseRequested = value;
    }

    private volatile bool _pausedByUser;
    public bool PausedByUser => _pausedByUser;

    private volatile bool _pausedByBusinessSoftware;
    public bool PausedByBusinessSoftware
    {
        get => _pausedByBusinessSoftware;
        set
        {
            _pausedByBusinessSoftware = value;
            OnPropertyChanged(nameof(CanResume));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private CancellationTokenSource? _cts;
    public CancellationTokenSource? Cts
    {
        get => _cts;
        set => _cts = value;
    }

    public ICommand PauseCommand { get; }
    public ICommand ResumeCommand { get; }
    public ICommand StopCommand { get; }

    private int _progression;
    public int Progression
    {
        get => _progression;
        set => SetField(ref _progression, value);
    }

    private int _totalFiles;
    public int TotalFiles
    {
        get => _totalFiles;
        set => SetField(ref _totalFiles, value);
    }

    private int _processedFiles;
    public int ProcessedFiles
    {
        get => _processedFiles;
        set => SetField(ref _processedFiles, value);
    }

    private long _totalSize;
    public long TotalSize
    {
        get => _totalSize;
        set { if (SetField(ref _totalSize, value)) OnPropertyChanged(nameof(TotalSizeFormatted)); }
    }

    private long _remainingSize;
    public long RemainingSize
    {
        get => _remainingSize;
        set { if (SetField(ref _remainingSize, value)) OnPropertyChanged(nameof(RemainingSizeFormatted)); }
    }

    private string _currentFile = "";
    public string CurrentFile
    {
        get => _currentFile;
        set => SetField(ref _currentFile, value);
    }

    public string TotalSizeFormatted => FormatBytes(TotalSize);
    public string RemainingSizeFormatted => FormatBytes(RemainingSize);

    public WorkItemViewModel(Work work)
    {
        Work = work;
        PauseCommand = new RelayCommand(_ => RequestPause(), _ => CanPause);
        ResumeCommand = new RelayCommand(_ => RequestResume(), _ => CanResume);
        StopCommand = new RelayCommand(_ => RequestStop(), _ => CanStop);
    }

    public void RequestPause()
    {
        _pausedByUser = true;
        _pauseRequested = true;
        StatusKey = "Paused";
    }

    public void RequestResume()
    {
        _pausedByUser = false;
        _pausedByBusinessSoftware = false;
        _pauseRequested = false;
        StatusKey = "Active";
    }

    public void RequestStop()
    {
        _pausedByUser = false;
        _pausedByBusinessSoftware = false;
        _pauseRequested = false;
        _cts?.Cancel();
        StatusKey = "Cancelled";
    }

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(TotalSizeFormatted));
        OnPropertyChanged(nameof(RemainingSizeFormatted));
    }

    public void UpdateFromState(WorkState state)
    {
        if (StatusKey is "Paused" or "Cancelled") return;
        StatusKey = state.Status;
        Progression = state.Progression;
        TotalFiles = state.TotalFiles;
        ProcessedFiles = state.TotalFiles - state.RemainingFiles;
        TotalSize = state.TotalSize;
        RemainingSize = state.RemainingSize;
        CurrentFile = state.CurrentSourceFile;
    }

    public void Reset()
    {
        _pauseRequested = false;
        _pausedByUser = false;
        _pausedByBusinessSoftware = false;
        StatusKey = "Inactive";
        Progression = 0;
        TotalFiles = 0;
        ProcessedFiles = 0;
        TotalSize = 0;
        RemainingSize = 0;
        CurrentFile = "";
    }

    private static string LocalizeStatus(string key)
    {
        Language lang = Language.GetInstance();
        return key switch
        {
            "Done" => lang.GetString("progress_done"),
            "Error" => lang.GetString("progress_error"),
            "Active" => lang.GetString("wpf_status_active"),
            "Inactive" => lang.GetString("wpf_status_inactive"),
            "Idle" => lang.GetString("wpf_status_idle"),
            "Paused" => lang.GetString("wpf_status_paused"),
            "Cancelled" => lang.GetString("wpf_status_cancelled"),
            _ => key
        };
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
            return Language.GetInstance().GetCurrentLanguage() == Lang.FR ? "0 o" : "0 B";

        bool french = Language.GetInstance().GetCurrentLanguage() == Lang.FR;
        string[] units = french
            ? ["o", "Ko", "Mo", "Go", "To"]
            : ["B", "KB", "MB", "GB", "TB"];

        double v = bytes;
        int u = 0;
        while (v >= 1024 && u < units.Length - 1)
        {
            v /= 1024;
            u++;
        }

        return $"{v:0.##} {units[u]}";
    }
}
