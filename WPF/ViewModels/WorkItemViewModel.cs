using EasyLog;
using LanguageFile;
using WorkFile;

namespace EasySave.WPF.ViewModels;

/// <summary>
/// Wrapper observable d'un Work : expose la progression en temps réel pour le binding XAML.
/// </summary>
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
    /// <summary>Valeur technique (Active, Inactive, Done, Error, Idle) pour les couleurs.</summary>
    public string StatusKey
    {
        get => _statusKey;
        set
        {
            if (SetField(ref _statusKey, value))
                OnPropertyChanged(nameof(StatusDisplay));
        }
    }

    /// <summary>Libellé affiché selon la langue courante.</summary>
    public string StatusDisplay => LocalizeStatus(_statusKey);

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
    }

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(TotalSizeFormatted));
        OnPropertyChanged(nameof(RemainingSizeFormatted));
    }

    public void UpdateFromState(WorkState state)
    {
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
