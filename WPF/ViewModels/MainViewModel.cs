using System.Collections.ObjectModel;
using System.Windows.Input;
using Application = System.Windows.Application;
using EasyLog;
using EasySave.WPF.Services;
using LanguageFile;
using WorkFile;
using WorkListFile;

namespace EasySave.WPF.ViewModels;

/// <summary>
/// VM principal : gère la liste des travaux, leur exécution, la suppression,
/// la bascule de langue et expose les libellés traduits pour l'UI.
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly WorkList _workList;
    private readonly Language _lang;
    private readonly BackupService _backupService = new BackupService();

    public ObservableCollection<WorkItemViewModel> Works { get; } = [];

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (SetField(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(CanInteract));
                RefreshTransportBindings();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool CanInteract => !IsRunning;

    /// <summary>Barre d’actions principale (lancer / nouveau) visible seulement hors exécution.</summary>
    public bool ShowMainRunActions => !IsRunning;

    /// <summary>Pause / reprise / arrêt visibles pendant une série de sauvegardes v2.</summary>
    public bool ShowBackupTransportActions => IsRunning;

    private volatile bool _runPaused;

    /// <summary>État pause : lu par le filet d’exécution via callback vers <see cref="BackupService"/>.</summary>
    public bool IsRunPaused
    {
        get => _runPaused;
        private set
        {
            if (_runPaused == value) return;
            _runPaused = value;
            OnPropertyChanged(nameof(IsRunPaused));
            RefreshTransportBindings();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool ShowPauseButton => IsRunning && !IsRunPaused;
    public bool ShowResumeButton => IsRunning && IsRunPaused;

    private CancellationTokenSource? _runCts;
    private WorkItemViewModel? _currentRunningVm;

    private string _statusBanner = "";
    public string StatusBanner
    {
        get => _statusBanner;
        set { if (SetField(ref _statusBanner, value)) OnPropertyChanged(nameof(HasStatusBanner)); }
    }

    public bool HasStatusBanner => !string.IsNullOrEmpty(StatusBanner);

    private string _statusBannerKind = "info"; // "info" | "success" | "error" | "warning"
    public string StatusBannerKind
    {
        get => _statusBannerKind;
        set => SetField(ref _statusBannerKind, value);
    }

    private bool _isFrench;
    public bool IsFrench
    {
        get => _isFrench;
        set
        {
            if (SetField(ref _isFrench, value))
            {
                _lang.SetLanguage(value ? Lang.FR : Lang.EN);
                NotifyAllLabels();
            }
        }
    }

    public ICommand CreateWorkCommand { get; }
    public ICommand RunSelectedCommand { get; }
    public ICommand RunAllCommand { get; }
    public ICommand DeleteWorkCommand { get; }
    public ICommand PauseBackupCommand { get; }
    public ICommand ResumeBackupCommand { get; }
    public ICommand StopBackupCommand { get; }

    public Action<CreateWorkViewModel>? RequestShowCreateDialog { get; set; }

    public MainViewModel()
    {
        _workList = new WorkList();
        _workList.MaxWorkCount = int.MaxValue;

        _lang = Language.GetInstance();
        _isFrench = _lang.GetCurrentLanguage() == Lang.FR;

        foreach (Work w in _workList.GetWork())
        {
            Works.Add(new WorkItemViewModel(w));
        }

        CreateWorkCommand = new RelayCommand(_ => OpenCreateDialog(), _ => CanInteract && !_workList.IsFull());
        RunSelectedCommand = new RelayCommand(async _ => await RunWorksAsync(GetSelected()), _ => CanInteract && GetSelected().Any());
        RunAllCommand = new RelayCommand(async _ => await RunWorksAsync(Works.ToList()), _ => CanInteract && Works.Count > 0);
        DeleteWorkCommand = new RelayCommand(p => DeleteWork(p as WorkItemViewModel), _ => CanInteract);
        PauseBackupCommand = new RelayCommand(_ => PauseBackup(), _ => IsRunning && !IsRunPaused);
        ResumeBackupCommand = new RelayCommand(_ => ResumeBackup(), _ => IsRunning && IsRunPaused);
        StopBackupCommand = new RelayCommand(_ => StopBackup(), _ => IsRunning);
    }

    private void RefreshTransportBindings()
    {
        OnPropertyChanged(nameof(ShowMainRunActions));
        OnPropertyChanged(nameof(ShowBackupTransportActions));
        OnPropertyChanged(nameof(ShowPauseButton));
        OnPropertyChanged(nameof(ShowResumeButton));
    }

    private void PauseBackup()
    {
        IsRunPaused = true;
        if (_currentRunningVm != null)
            _currentRunningVm.StatusKey = "Paused";
    }

    private void ResumeBackup()
    {
        IsRunPaused = false;
        if (_currentRunningVm != null)
            _currentRunningVm.StatusKey = "Active";
    }

    private void StopBackup()
    {
        _runCts?.Cancel();
        IsRunPaused = false;
    }

    // --- Libellés traduits utilisés en binding direct ---
    public string LblAppTitle => "EasySave";
    public string LblAppSubtitle => IsFrench ? "Outil de sauvegarde — v2" : "Backup tool — v2";
    public string LblSectionMenu => _lang.GetString("wpf_section_menu");
    public string LblSectionLanguage => _lang.GetString("wpf_section_language");
    public string LblNavWorks => IsFrench ? "Travaux" : "Jobs";
    public string LblNavSettings => IsFrench ? "Paramètres" : "Settings";
    public string LblHeader => IsFrench ? "Mes travaux de sauvegarde" : "My backup jobs";
    public string LblCount => IsFrench
        ? $"{Works.Count} travaux"
        : $"{Works.Count} jobs";
    public string LblNew => IsFrench ? "Nouveau" : "New";
    public string LblRunSelected => IsFrench ? "Exécuter la sélection" : "Run selected";
    public string LblRunAll => IsFrench ? "Tout exécuter" : "Run all";
    public string LblPause => _lang.GetString("wpf_pause");
    public string LblResume => _lang.GetString("wpf_resume");
    public string LblStop => _lang.GetString("wpf_stop");
    public string LblEmptyTitle => IsFrench ? "Aucun travail pour le moment" : "No jobs yet";
    public string LblEmptySubtitle => IsFrench
        ? "Créez votre premier travail de sauvegarde pour commencer."
        : "Create your first backup job to get started.";
    public string LblCreateFirst => IsFrench ? "Créer un travail" : "Create a job";
    public string LblFull => _lang.GetString("backup_type_short_full");
    public string LblDiff => _lang.GetString("backup_type_short_diff");
    public string LblBackupFullTitle => _lang.GetString("wpf_backup_full_title");
    public string LblBackupFullDesc => _lang.GetString("wpf_backup_full_desc");
    public string LblBackupDiffTitle => _lang.GetString("wpf_backup_diff_title");
    public string LblBackupDiffDesc => _lang.GetString("wpf_backup_diff_desc");
    public string LblSource => "Source";
    public string LblDestination => IsFrench ? "Destination" : "Destination";
    public string LblDelete => IsFrench ? "Supprimer" : "Delete";
    public string LblDialogTitle => IsFrench ? "Nouveau travail" : "New job";
    public string LblFieldName => IsFrench ? "Nom du travail" : "Job name";
    public string LblFieldSource => IsFrench ? "Dossier source" : "Source folder";
    public string LblFieldDestination => IsFrench ? "Dossier destination" : "Destination folder";
    public string LblFieldType => IsFrench ? "Type de sauvegarde" : "Backup type";
    public string LblBrowse => IsFrench ? "Parcourir..." : "Browse...";
    public string LblSave => IsFrench ? "Enregistrer" : "Save";
    public string LblCancel => IsFrench ? "Annuler" : "Cancel";

    private void NotifyAllLabels()
    {
        OnPropertyChanged(nameof(LblAppSubtitle));
        OnPropertyChanged(nameof(LblSectionMenu));
        OnPropertyChanged(nameof(LblSectionLanguage));
        OnPropertyChanged(nameof(LblNavWorks));
        OnPropertyChanged(nameof(LblNavSettings));
        OnPropertyChanged(nameof(LblHeader));
        OnPropertyChanged(nameof(LblCount));
        OnPropertyChanged(nameof(LblNew));
        OnPropertyChanged(nameof(LblRunSelected));
        OnPropertyChanged(nameof(LblRunAll));
        OnPropertyChanged(nameof(LblPause));
        OnPropertyChanged(nameof(LblResume));
        OnPropertyChanged(nameof(LblStop));
        OnPropertyChanged(nameof(LblEmptyTitle));
        OnPropertyChanged(nameof(LblEmptySubtitle));
        OnPropertyChanged(nameof(LblCreateFirst));
        OnPropertyChanged(nameof(LblFull));
        OnPropertyChanged(nameof(LblDiff));
        OnPropertyChanged(nameof(LblBackupFullTitle));
        OnPropertyChanged(nameof(LblBackupFullDesc));
        OnPropertyChanged(nameof(LblBackupDiffTitle));
        OnPropertyChanged(nameof(LblBackupDiffDesc));
        OnPropertyChanged(nameof(LblSource));
        OnPropertyChanged(nameof(LblDestination));
        OnPropertyChanged(nameof(LblDelete));
        OnPropertyChanged(nameof(LblDialogTitle));
        OnPropertyChanged(nameof(LblFieldName));
        OnPropertyChanged(nameof(LblFieldSource));
        OnPropertyChanged(nameof(LblFieldDestination));
        OnPropertyChanged(nameof(LblFieldType));
        OnPropertyChanged(nameof(LblBrowse));
        OnPropertyChanged(nameof(LblSave));
        OnPropertyChanged(nameof(LblCancel));

        foreach (WorkItemViewModel w in Works)
            w.RefreshLocalization();
    }

    private List<WorkItemViewModel> GetSelected() => Works.Where(w => w.IsSelected).ToList();

    private void OpenCreateDialog()
    {
        CreateWorkViewModel vm = new CreateWorkViewModel(_workList);
        vm.CreateRequested += (_, _) =>
        {
            Works.Clear();
            foreach (Work w in _workList.GetWork())
            {
                Works.Add(new WorkItemViewModel(w));
            }
            OnPropertyChanged(nameof(LblCount));
            ShowBanner(IsFrench ? "Travail créé." : "Job created.", "success");
        };
        RequestShowCreateDialog?.Invoke(vm);
    }

    private void DeleteWork(WorkItemViewModel? item)
    {
        if (item is null) return;
        int idx = Works.IndexOf(item);
        if (idx < 0) return;
        if (_workList.RemoveWork(idx))
        {
            Works.RemoveAt(idx);
            OnPropertyChanged(nameof(LblCount));
            ShowBanner(IsFrench ? "Travail supprimé." : "Job deleted.", "info");
        }
    }

    private async Task RunWorksAsync(List<WorkItemViewModel> targets)
    {
        if (targets.Count == 0 || IsRunning) return;
        IsRunning = true;
        IsRunPaused = false;
        StatusBanner = "";

        using CancellationTokenSource cts = new CancellationTokenSource();
        _runCts = cts;
        List<string> errors = [];
        bool stoppedByUser = false;

        try
        {
            foreach (WorkItemViewModel vm in targets)
            {
                cts.Token.ThrowIfCancellationRequested();

                _currentRunningVm = vm;
                vm.Reset();
                vm.StatusKey = "Active";

                Progress<WorkState> reporter = new Progress<WorkState>(state =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        vm.UpdateFromState(state);
                        if (IsRunPaused)
                            vm.StatusKey = "Paused";
                    });
                });

                List<string> jobErrors = [];
                bool ok = await Task.Run(() => _backupService.ExecuteWork(
                    vm.Work,
                    reporter,
                    jobErrors,
                    cts.Token,
                    () => IsRunPaused));

                if (cts.IsCancellationRequested)
                {
                    vm.StatusKey = "Cancelled";
                    stoppedByUser = true;
                    break;
                }

                vm.StatusKey = ok ? "Done" : "Error";

                foreach (string e in jobErrors)
                    errors.Add($"[{vm.Name}] {e}");
            }
        }
        catch (OperationCanceledException)
        {
            stoppedByUser = true;
        }
        finally
        {
            _currentRunningVm = null;
            _runCts = null;
            IsRunPaused = false;
            IsRunning = false;
        }

        if (stoppedByUser)
        {
            ShowBanner(_lang.GetString("wpf_backup_stopped"), "warning");
            return;
        }

        if (errors.Count == 0)
        {
            ShowBanner(IsFrench ? "Sauvegarde terminée avec succès." : "Backup completed successfully.", "success");
        }
        else
        {
            string head = IsFrench
                ? $"Sauvegarde terminée avec {errors.Count} erreur(s)."
                : $"Backup finished with {errors.Count} error(s).";
            ShowBanner(head + "\n" + string.Join("\n", errors.Take(3)), "error");
        }
    }

    private void ShowBanner(string text, string kind)
    {
        StatusBannerKind = kind;
        StatusBanner = text;
    }
}
