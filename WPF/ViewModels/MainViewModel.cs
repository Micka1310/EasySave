using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Application = System.Windows.Application;
using EasyLog;
using EasySave.WPF.Services;
using LanguageFile;
using WorkFile;
using WorkListFile;

namespace EasySave.WPF.ViewModels;

/// <summary>
/// VM principal : orchestration globale UI + exécution.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly WorkList _workList;
    private readonly Language _lang;
    private readonly BackupService _backupService = new();
    private readonly Logger _logger = new();
    private readonly GeneralSettingsService _generalSettingsService = new();
    private GeneralSettings _settings = new();

    public ObservableCollection<WorkItemViewModel> Works { get; } = [];
    public ObservableCollection<WorkItemViewModel> PagedWorks { get; } = [];
    public ObservableCollection<ExtensionOptionViewModel> EncryptionExtensionOptions { get; } = [];
    public ObservableCollection<string> BusinessSoftwareNames { get; } = [];

    public IReadOnlyList<int> PageSizeChoices { get; } = [5, 10, 20, 50];

    private bool _showWorksPanel = true;
    public bool ShowWorksPanel
    {
        get => _showWorksPanel;
        set
        {
            if (!SetField(ref _showWorksPanel, value)) return;
            OnPropertyChanged(nameof(ShowSettingsPanel));
            OnPropertyChanged(nameof(LblPrimaryHeader));
            OnPropertyChanged(nameof(LblPrimarySubtitle));
        }
    }

    public bool ShowSettingsPanel => !_showWorksPanel;

    private int _pageSize = 10;
    public int PageSize
    {
        get => _pageSize;
        set
        {
            int v = PageSizeChoices.Contains(value) ? value : 10;
            if (_pageSize == v) return;
            _pageSize = v;
            OnPropertyChanged(nameof(PageSize));
            _currentPage = 1;
            RebuildPagedWorks();
        }
    }

    private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            int max = TotalPages;
            int v = Math.Clamp(value, 1, Math.Max(1, max));
            if (_currentPage == v) return;
            _currentPage = v;
            OnPropertyChanged(nameof(CurrentPage));
            RebuildPagedWorks();
        }
    }

    public int TotalPages => Works.Count == 0
        ? 1
        : Math.Max(1, (int)Math.Ceiling(Works.Count / (double)Math.Max(1, _pageSize)));

    public bool ShowPaginationBar => Works.Count > 0 && TotalPages > 1;

    public string LblPaginationDetail
    {
        get
        {
            if (Works.Count == 0) return "";
            int start = (_currentPage - 1) * _pageSize + 1;
            int end = Math.Min(_currentPage * _pageSize, Works.Count);
            return IsFrench ? $"{start}–{end} sur {Works.Count}" : $"{start}–{end} of {Works.Count}";
        }
    }

    public string LblPaginationPages => $"Page {_currentPage} / {TotalPages}";

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (SetField(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(CanInteract));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool CanInteract => !IsRunning;

    private string _statusBanner = "";
    public string StatusBanner
    {
        get => _statusBanner;
        set { if (SetField(ref _statusBanner, value)) OnPropertyChanged(nameof(HasStatusBanner)); }
    }

    public bool HasStatusBanner => !string.IsNullOrEmpty(StatusBanner);

    private string _statusBannerKind = "info";
    public string StatusBannerKind
    {
        get => _statusBannerKind;
        set => SetField(ref _statusBannerKind, value);
    }

    public ICommand CreateWorkCommand { get; }
    public ICommand RunSelectedCommand { get; }
    public ICommand RunAllCommand { get; }
    public ICommand DeleteWorkCommand { get; }
    public ICommand PrevPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand AddCustomEncryptionExtensionCommand { get; }
    public ICommand AddBusinessSoftwareCommand { get; }
    public ICommand RemoveBusinessSoftwareCommand { get; }
    public ICommand ResetBusinessSoftwareDefaultsCommand { get; }
    public ICommand ClearBusinessSoftwareCommand { get; }
    public ICommand RefreshLogsCommand { get; }
    public ICommand OpenLogsFolderCommand { get; }

    public Action<CreateWorkViewModel>? RequestShowCreateDialog { get; set; }

    public MainViewModel()
    {
        _workList = new WorkList();
        _workList.MaxWorkCount = int.MaxValue;

        _lang = Language.GetInstance();
        _isFrench = _lang.GetCurrentLanguage() == Lang.FR;
        _settings = _generalSettingsService.Load();

        InitializeBusinessSoftware();
        InitializeLogFormatFromSettings();
        InitializeEncryptionExtensions();

        foreach (Work w in _workList.GetWork())
            Works.Add(new WorkItemViewModel(w));

        Works.CollectionChanged += OnWorksCollectionChanged;
        RebuildPagedWorks();

        CreateWorkCommand = new RelayCommand(_ => OpenCreateDialog(), _ => CanInteract && !_workList.IsFull());
        RunSelectedCommand = new RelayCommand(async _ => await RunWorksAsync(GetSelected()), _ => CanInteract && GetSelected().Any());
        RunAllCommand = new RelayCommand(async _ => await RunWorksAsync(Works.ToList()), _ => CanInteract && Works.Count > 0);
        DeleteWorkCommand = new RelayCommand(p => DeleteWork(p as WorkItemViewModel), _ => CanInteract);
        PrevPageCommand = new RelayCommand(_ => CurrentPage -= 1, _ => CurrentPage > 1);
        NextPageCommand = new RelayCommand(_ => CurrentPage += 1, _ => CurrentPage < TotalPages);
        AddCustomEncryptionExtensionCommand = new RelayCommand(_ => AddCustomEncryptionExtension(), _ => true);
        AddBusinessSoftwareCommand = new RelayCommand(_ => AddBusinessSoftware(), _ => true);
        RemoveBusinessSoftwareCommand = new RelayCommand(p => RemoveBusinessSoftware(p as string), _ => true);
        ResetBusinessSoftwareDefaultsCommand = new RelayCommand(_ => ResetBusinessSoftwareDefaults(), _ => true);
        ClearBusinessSoftwareCommand = new RelayCommand(_ => ClearBusinessSoftware(), _ => BusinessSoftwareNames.Count > 0);
        RefreshLogsCommand = new RelayCommand(_ => RefreshLogPreview(), _ => true);
        OpenLogsFolderCommand = new RelayCommand(_ => OpenLogsFolder(), _ => true);

        RefreshLogPreview();
    }

    private void OnWorksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(LblCount));
        RebuildPagedWorks();
    }

    private void RebuildPagedWorks()
    {
        PagedWorks.Clear();
        if (Works.Count == 0)
        {
            OnPropertyChanged(nameof(LblPaginationDetail));
            OnPropertyChanged(nameof(LblPaginationPages));
            OnPropertyChanged(nameof(ShowPaginationBar));
            CommandManager.InvalidateRequerySuggested();
            return;
        }

        int pageSize = Math.Max(1, _pageSize);
        int totalPages = Math.Max(1, (int)Math.Ceiling(Works.Count / (double)pageSize));
        if (_currentPage > totalPages)
        {
            _currentPage = totalPages;
            OnPropertyChanged(nameof(CurrentPage));
        }

        int skip = (_currentPage - 1) * pageSize;
        foreach (WorkItemViewModel w in Works.Skip(skip).Take(pageSize))
            PagedWorks.Add(w);

        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(LblPaginationDetail));
        OnPropertyChanged(nameof(LblPaginationPages));
        OnPropertyChanged(nameof(ShowPaginationBar));
        CommandManager.InvalidateRequerySuggested();
    }

    private List<WorkItemViewModel> GetSelected() => Works.Where(w => w.IsSelected).ToList();

    private void OpenCreateDialog()
    {
        CreateWorkViewModel vm = new(_workList);
        vm.CreateRequested += (_, _) =>
        {
            Works.Clear();
            foreach (Work w in _workList.GetWork())
                Works.Add(new WorkItemViewModel(w));

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
        if (TryGetRunningBusinessSoftware(out string startupBlockingProcess))
        {
            string blockedText = $"{_lang.GetString("wpf_business_software_blocked_start")} ({startupBlockingProcess})";
            ShowBanner(blockedText, "warning");
            LogBusinessSoftwareEvent(startupBlockingProcess, blockedText);
            return;
        }

        IsRunning = true;
        StatusBanner = "";
        List<string> errors = [];

        try
        {
            foreach (WorkItemViewModel vm in targets)
            {
                vm.Reset();
                vm.StatusKey = "Active";

                Progress<WorkState> reporter = new(state =>
                {
                    Application.Current.Dispatcher.Invoke(() => vm.UpdateFromState(state));
                });

                using CancellationTokenSource monitorCts = new();
                bool pauseRequestedByBusinessSoftware = false;
                Task monitorTask = MonitorBusinessSoftwareAsync(
                    vm,
                    () => pauseRequestedByBusinessSoftware,
                    v => pauseRequestedByBusinessSoftware = v,
                    monitorCts.Token);

                List<string> jobErrors = [];
                bool ok = await Task.Run(() => _backupService.ExecuteWork(
                    vm.Work,
                    reporter,
                    jobErrors,
                    CancellationToken.None,
                    () => pauseRequestedByBusinessSoftware));

                monitorCts.Cancel();
                await monitorTask;

                vm.StatusKey = ok ? "Done" : "Error";
                foreach (string e in jobErrors)
                    errors.Add($"[{vm.Name}] {e}");
            }
        }
        finally
        {
            IsRunning = false;
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
            ShowBanner(head + "\n" + string.Join("\n", errors), "error");
        }
    }

    private void ShowBanner(string text, string kind)
    {
        StatusBannerKind = kind;
        StatusBanner = text;
    }
}
