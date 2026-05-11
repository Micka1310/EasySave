using System.IO;
using System.Windows.Input;
using LanguageFile;
using WorkListFile;

namespace EasySave.WPF.ViewModels;

/// <summary>
/// VM du dialog de création d'un travail. Expose les champs liés au formulaire,
/// déclenche la validation côté Lib (chemins, doublons) et signale au parent
/// le succès via <see cref="CreateRequested"/>.
/// </summary>
public class CreateWorkViewModel : ViewModelBase
{
    private readonly WorkList _workList;
    private readonly Language _lang;

    public event EventHandler? CloseRequested;
    public event EventHandler? CreateRequested;

    private string _workName = "";
    public string WorkName
    {
        get => _workName;
        set { if (SetField(ref _workName, value)) ClearError(); }
    }

    private string _source = "";
    public string Source
    {
        get => _source;
        set { if (SetField(ref _source, value)) ClearError(); }
    }

    private string _destination = "";
    public string Destination
    {
        get => _destination;
        set { if (SetField(ref _destination, value)) ClearError(); }
    }

    private bool _isFullBackup = true;
    public bool IsFullBackup
    {
        get => _isFullBackup;
        set => SetField(ref _isFullBackup, value);
    }

    private string _errorMessage = "";
    public string ErrorMessage
    {
        get => _errorMessage;
        set { if (SetField(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError)); }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ICommand BrowseSourceCommand { get; }
    public ICommand BrowseDestinationCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public CreateWorkViewModel(WorkList workList)
    {
        _workList = workList;
        _lang = Language.GetInstance();

        BrowseSourceCommand = new RelayCommand(_ => BrowseFolder(s => Source = s));
        BrowseDestinationCommand = new RelayCommand(_ => BrowseFolder(s => Destination = s));
        SaveCommand = new RelayCommand(_ => Save());
        CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, EventArgs.Empty));
    }

    private static void BrowseFolder(Action<string> onPicked)
    {
        using System.Windows.Forms.FolderBrowserDialog dlg = new();
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            onPicked(dlg.SelectedPath);
        }
    }

    private void ClearError() => ErrorMessage = "";

    private void Save()
    {
        if (_workList.IsFull())
        {
            ErrorMessage = _lang.GetString("work_max_reached");
            return;
        }

        string name = (WorkName ?? "").Trim();
        string src = (Source ?? "").Trim();
        string dst = (Destination ?? "").Trim();

        if (string.IsNullOrWhiteSpace(name)) { ErrorMessage = _lang.GetString("error_empty_work_name"); return; }
        if (string.IsNullOrWhiteSpace(src)) { ErrorMessage = _lang.GetString("error_empty_source"); return; }
        if (string.IsNullOrWhiteSpace(dst)) { ErrorMessage = _lang.GetString("error_empty_destination"); return; }
        if (!Directory.Exists(src)) { ErrorMessage = _lang.GetString("error_source_not_found"); return; }
        if (!Directory.Exists(dst)) { ErrorMessage = _lang.GetString("error_destination_not_found"); return; }

        string srcFull = Path.GetFullPath(src).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string dstFull = Path.GetFullPath(dst).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.Equals(srcFull, dstFull, StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = _lang.GetString("error_same_source_destination");
            return;
        }

        string type = IsFullBackup ? "1" : "2";
        _workList.AddWork([name, src, dst, type]);

        CreateRequested?.Invoke(this, EventArgs.Empty);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
