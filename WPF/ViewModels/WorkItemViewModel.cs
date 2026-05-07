using EasyLog;
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

    private string _status = "Idle";
    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

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

    public void UpdateFromState(WorkState state)
    {
        Status = state.Status;
        Progression = state.Progression;
        TotalFiles = state.TotalFiles;
        ProcessedFiles = state.TotalFiles - state.RemainingFiles;
        TotalSize = state.TotalSize;
        RemainingSize = state.RemainingSize;
        CurrentFile = state.CurrentSourceFile;
    }

    public void Reset()
    {
        Status = "Idle";
        Progression = 0;
        TotalFiles = 0;
        ProcessedFiles = 0;
        TotalSize = 0;
        RemainingSize = 0;
        CurrentFile = "";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 o";
        string[] units = ["o", "Ko", "Mo", "Go", "To"];
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
