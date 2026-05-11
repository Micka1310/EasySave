using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
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
/// la bascule de langue, la navigation Travaux / Paramètres et la pagination.
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly WorkList _workList;
    private readonly Language _lang;
    private readonly BackupService _backupService = new BackupService();
    private readonly GeneralSettingsService _generalSettingsService = new GeneralSettingsService();
    private readonly XmlSerializer _logDocumentSerializer = new(typeof(LogDocument));
    private GeneralSettings _settings = new();

    public ObservableCollection<WorkItemViewModel> Works { get; } = [];
    public ObservableCollection<WorkItemViewModel> PagedWorks { get; } = [];
    public ObservableCollection<ExtensionOptionViewModel> EncryptionExtensionOptions { get; } = [];

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
            if (Works.Count == 0)
                return "";
            int start = (_currentPage - 1) * _pageSize + 1;
            int end = Math.Min(_currentPage * _pageSize, Works.Count);
            return IsFrench
                ? $"{start}–{end} sur {Works.Count}"
                : $"{start}–{end} of {Works.Count}";
        }
    }

    public string LblPaginationPages => IsFrench
        ? $"Page {_currentPage} / {TotalPages}"
        : $"Page {_currentPage} / {TotalPages}";

    public string DisplayWorksJsonPath => Path.Combine(AppContext.BaseDirectory, "works.json");
    public string DisplayEasyLogFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
    public string DisplayLogFilePath => _displayLogFilePath;

    private string _displayLogFilePath = "";
    private string _logPreview = "";

    public string LogPreview
    {
        get => _logPreview;
        private set => SetField(ref _logPreview, value);
    }

    public bool HasLogPreview => !string.IsNullOrWhiteSpace(_logPreview);

    private bool _isLogFormatJson = true;
    public bool IsLogFormatJson
    {
        get => _isLogFormatJson;
        set
        {
            if (!SetField(ref _isLogFormatJson, value))
            {
                return;
            }

            ApplyLogFormatPreference();
        }
    }

    private string _customEncryptionExtensionInput = "";
    public string CustomEncryptionExtensionInput
    {
        get => _customEncryptionExtensionInput;
        set => SetField(ref _customEncryptionExtensionInput, value);
    }

    public string SelectedEncryptionExtensionsDisplay => string.Join("; ",
        EncryptionExtensionOptions
            .Where(x => x.IsSelected)
            .Select(x => x.Extension)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

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

    public bool ShowMainRunActions => !IsRunning;

    public bool ShowBackupTransportActions => IsRunning;

    private volatile bool _runPaused;

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

    private string _statusBannerKind = "info";
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
    public ICommand PrevPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand AddCustomEncryptionExtensionCommand { get; }
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

        _isLogFormatJson = !string.Equals(_settings.LogFormat, "xml", StringComparison.OrdinalIgnoreCase);
        LogFormatSettings.Current = _isLogFormatJson ? LogFormat.Json : LogFormat.Xml;

        InitializeEncryptionExtensions();

        foreach (Work w in _workList.GetWork())
        {
            Works.Add(new WorkItemViewModel(w));
        }

        Works.CollectionChanged += OnWorksCollectionChanged;
        RebuildPagedWorks();

        CreateWorkCommand = new RelayCommand(_ => OpenCreateDialog(), _ => CanInteract && !_workList.IsFull());
        RunSelectedCommand = new RelayCommand(async _ => await RunWorksAsync(GetSelected()), _ => CanInteract && GetSelected().Any());
        RunAllCommand = new RelayCommand(async _ => await RunWorksAsync(Works.ToList()), _ => CanInteract && Works.Count > 0);
        DeleteWorkCommand = new RelayCommand(p => DeleteWork(p as WorkItemViewModel), _ => CanInteract);
        PauseBackupCommand = new RelayCommand(_ => PauseBackup(), _ => IsRunning && !IsRunPaused);
        ResumeBackupCommand = new RelayCommand(_ => ResumeBackup(), _ => IsRunning && IsRunPaused);
        StopBackupCommand = new RelayCommand(_ => StopBackup(), _ => IsRunning);
        PrevPageCommand = new RelayCommand(_ => CurrentPage -= 1, _ => CurrentPage > 1);
        NextPageCommand = new RelayCommand(_ => CurrentPage += 1, _ => CurrentPage < TotalPages);
        AddCustomEncryptionExtensionCommand = new RelayCommand(_ => AddCustomEncryptionExtension(), _ => true);
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

    public string LblPrimaryHeader => ShowWorksPanel ? LblHeader : LblSettingsTitle;
    public string LblPrimarySubtitle => ShowWorksPanel ? LblCount : LblSettingsSubtitle;

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

    public string LblSettingsTitle => _lang.GetString("wpf_settings_title");
    public string LblSettingsSubtitle => _lang.GetString("wpf_settings_subtitle");
    public string LblSettingsSectionDisplay => _lang.GetString("wpf_settings_section_display");
    public string LblSectionLogFormat => _lang.GetString("log_format_header_label");
    public string LblLogJson => _lang.GetString("log_format_json");
    public string LblLogXml => _lang.GetString("log_format_xml");
    public string LblSettingsPageSize => _lang.GetString("wpf_settings_page_size");
    public string LblSettingsPageSizeHint => _lang.GetString("wpf_settings_page_size_hint");
    public string LblSettingsSectionData => _lang.GetString("wpf_settings_section_data");
    public string LblSettingsDataFolder => _lang.GetString("wpf_settings_data_folder");
    public string LblSettingsWorksFile => _lang.GetString("wpf_settings_works_file");
    public string LblSettingsLogsFile => _lang.GetString("wpf_settings_logs_file");
    public string LblSettingsLogsPreview => _lang.GetString("wpf_settings_logs_preview");
    public string LblSettingsLogsRefresh => _lang.GetString("wpf_settings_logs_refresh");
    public string LblSettingsOpenLogsFolder => _lang.GetString("wpf_settings_open_logs_folder");
    public string LblSettingsSectionEncryption => _lang.GetString("wpf_settings_section_encryption");
    public string LblSettingsEncryptionExtensions => _lang.GetString("wpf_settings_encryption_extensions");
    public string LblSettingsEncryptionSelected => _lang.GetString("wpf_settings_encryption_selected");
    public string LblSettingsEncryptionAdd => _lang.GetString("wpf_settings_encryption_add");
    public string LblSettingsEncryptionAddButton => _lang.GetString("wpf_settings_encryption_add_button");
    public string LblSettingsEncryptionHint => _lang.GetString("wpf_settings_encryption_hint");
    public string LblSettingsCryptoSoftFolder => _lang.GetString("wpf_settings_cryptosoft_folder");
    public string DisplayCryptoSoftFolder => Path.Combine(AppContext.BaseDirectory, "CryptoSoft");
    public string LblSettingsSectionAbout => _lang.GetString("wpf_settings_section_about");
    public string LblSettingsAboutBody => _lang.GetString("wpf_settings_about_body");
    public string LblPagePrev => _lang.GetString("wpf_page_prev");
    public string LblPageNext => _lang.GetString("wpf_page_next");

    private void NotifyAllLabels()
    {
        OnPropertyChanged(nameof(LblAppSubtitle));
        OnPropertyChanged(nameof(LblSectionMenu));
        OnPropertyChanged(nameof(LblSectionLanguage));
        OnPropertyChanged(nameof(LblSectionLogFormat));
        OnPropertyChanged(nameof(LblLogJson));
        OnPropertyChanged(nameof(LblLogXml));
        OnPropertyChanged(nameof(LblNavWorks));
        OnPropertyChanged(nameof(LblNavSettings));
        OnPropertyChanged(nameof(LblHeader));
        OnPropertyChanged(nameof(LblCount));
        OnPropertyChanged(nameof(LblPrimaryHeader));
        OnPropertyChanged(nameof(LblPrimarySubtitle));
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
        OnPropertyChanged(nameof(LblSettingsTitle));
        OnPropertyChanged(nameof(LblSettingsSubtitle));
        OnPropertyChanged(nameof(LblSettingsSectionDisplay));
        OnPropertyChanged(nameof(LblSettingsPageSize));
        OnPropertyChanged(nameof(LblSettingsPageSizeHint));
        OnPropertyChanged(nameof(LblSettingsSectionData));
        OnPropertyChanged(nameof(LblSettingsDataFolder));
        OnPropertyChanged(nameof(LblSettingsWorksFile));
        OnPropertyChanged(nameof(LblSettingsLogsFile));
        OnPropertyChanged(nameof(LblSettingsLogsPreview));
        OnPropertyChanged(nameof(LblSettingsLogsRefresh));
        OnPropertyChanged(nameof(LblSettingsOpenLogsFolder));
        OnPropertyChanged(nameof(LblSettingsSectionEncryption));
        OnPropertyChanged(nameof(LblSettingsEncryptionExtensions));
        OnPropertyChanged(nameof(LblSettingsEncryptionSelected));
        OnPropertyChanged(nameof(LblSettingsEncryptionAdd));
        OnPropertyChanged(nameof(LblSettingsEncryptionAddButton));
        OnPropertyChanged(nameof(LblSettingsEncryptionHint));
        OnPropertyChanged(nameof(LblSettingsCryptoSoftFolder));
        OnPropertyChanged(nameof(LblSettingsSectionAbout));
        OnPropertyChanged(nameof(LblSettingsAboutBody));
        OnPropertyChanged(nameof(LblPagePrev));
        OnPropertyChanged(nameof(LblPageNext));
        OnPropertyChanged(nameof(LblPaginationDetail));
        OnPropertyChanged(nameof(LblPaginationPages));

        foreach (WorkItemViewModel w in Works)
            w.RefreshLocalization();

        RefreshLogPreview();
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

    private void InitializeEncryptionExtensions()
    {
        string[] defaults = [".txt", ".docx", ".pdf", ".xlsx", ".pptx", ".zip", ".json", ".xml"];
        HashSet<string> selected = _settings.EncryptedExtensions
            .Select(NormalizeExtension)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<string> allExtensions = defaults
            .Concat(selected)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        EncryptionExtensionOptions.Clear();
        foreach (string extension in allExtensions)
        {
            var option = new ExtensionOptionViewModel(extension, selected.Contains(extension));
            option.PropertyChanged += OnEncryptionOptionPropertyChanged;
            EncryptionExtensionOptions.Add(option);
        }

        SaveEncryptionExtensionsFromOptions();
    }

    private void AddCustomEncryptionExtension()
    {
        string normalized = NormalizeExtension(CustomEncryptionExtensionInput);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        ExtensionOptionViewModel? existing = EncryptionExtensionOptions
            .FirstOrDefault(x => string.Equals(x.Extension, normalized, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            existing = new ExtensionOptionViewModel(normalized, true);
            existing.PropertyChanged += OnEncryptionOptionPropertyChanged;
            EncryptionExtensionOptions.Add(existing);
        }
        else
        {
            existing.IsSelected = true;
        }

        CustomEncryptionExtensionInput = "";
        SaveEncryptionExtensionsFromOptions();
        OnPropertyChanged(nameof(SelectedEncryptionExtensionsDisplay));
    }

    private void OnEncryptionOptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ExtensionOptionViewModel.IsSelected))
        {
            return;
        }

        SaveEncryptionExtensionsFromOptions();
        OnPropertyChanged(nameof(SelectedEncryptionExtensionsDisplay));
    }

    private void SaveEncryptionExtensionsFromOptions()
    {
        List<string> selected = EncryptionExtensionOptions
            .Where(x => x.IsSelected)
            .Select(x => NormalizeExtension(x.Extension))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _generalSettingsService.Save(new GeneralSettings
        {
            EncryptedExtensions = selected,
            LogFormat = _settings.LogFormat
        });

        _settings.EncryptedExtensions = selected;
    }

    private static string NormalizeExtension(string value)
    {
        string v = value.Trim();
        if (string.IsNullOrWhiteSpace(v))
        {
            return string.Empty;
        }

        return v.StartsWith('.') ? v : "." + v;
    }

    private void ApplyLogFormatPreference()
    {
        LogFormatSettings.Current = IsLogFormatJson ? LogFormat.Json : LogFormat.Xml;
        _settings.LogFormat = IsLogFormatJson ? "json" : "xml";
        _generalSettingsService.Save(_settings);
        RefreshLogPreview();
    }

    private void RefreshLogPreview()
    {
        string extension = IsLogFormatJson ? ".json" : ".xml";
        _displayLogFilePath = "";
        OnPropertyChanged(nameof(DisplayLogFilePath));

        if (!Directory.Exists(DisplayEasyLogFolder))
        {
            LogPreview = IsFrench
                ? "Le dossier de logs n'existe pas encore."
                : "The log folder does not exist yet.";
            OnPropertyChanged(nameof(HasLogPreview));
            return;
        }

        FileInfo? latest = new DirectoryInfo(DisplayEasyLogFolder)
            .GetFiles("*" + extension, SearchOption.TopDirectoryOnly)
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault();

        if (latest is null)
        {
            LogPreview = IsFrench
                ? $"Aucun log {extension} disponible pour le moment."
                : $"No {extension} log available yet.";
            OnPropertyChanged(nameof(HasLogPreview));
            return;
        }

        _displayLogFilePath = latest.FullName;
        OnPropertyChanged(nameof(DisplayLogFilePath));

        try
        {
            List<LogEntry> entries = IsLogFormatJson
                ? ReadJsonLogEntries(latest.FullName)
                : ReadXmlLogEntries(latest.FullName);
            LogPreview = BuildLogPreview(entries);
        }
        catch
        {
            LogPreview = IsFrench
                ? "Impossible de lire ce fichier de log. Cliquez sur rafraichir ou ouvrez le dossier."
                : "Unable to read this log file. Try refresh or open the folder.";
        }

        OnPropertyChanged(nameof(HasLogPreview));
    }

    private static List<LogEntry> ReadJsonLogEntries(string filePath)
    {
        string content = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<LogEntry>>(content) ?? [];
    }

    private List<LogEntry> ReadXmlLogEntries(string filePath)
    {
        using FileStream stream = File.OpenRead(filePath);
        LogDocument? doc = _logDocumentSerializer.Deserialize(stream) as LogDocument;
        return doc?.Entries ?? [];
    }

    private string BuildLogPreview(List<LogEntry> entries)
    {
        if (entries.Count == 0)
        {
            return IsFrench
                ? "Le fichier de log est vide."
                : "The log file is empty.";
        }

        StringBuilder sb = new StringBuilder();
        IEnumerable<LogEntry> selected = entries.TakeLast(25);
        foreach (LogEntry entry in selected)
        {
            string status = entry.Success
                ? (IsFrench ? "OK" : "OK")
                : (IsFrench ? "ERREUR" : "ERROR");
            sb.Append(entry.Timestamp);
            sb.Append(" | ");
            sb.Append(entry.WorkName);
            sb.Append(" | ");
            sb.Append(status);
            sb.Append(" | ");
            sb.Append(Path.GetFileName(entry.SourceFile));
            sb.Append(" -> ");
            sb.Append(Path.GetFileName(entry.DestinationFile));
            sb.Append(" | ");
            sb.Append(entry.TransferTimeMs);
            sb.Append("ms");

            if (!entry.Success && !string.IsNullOrWhiteSpace(entry.ErrorMessage))
            {
                sb.Append(" | ");
                sb.Append(entry.ErrorMessage);
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private void OpenLogsFolder()
    {
        try
        {
            Directory.CreateDirectory(DisplayEasyLogFolder);
            Process.Start(new ProcessStartInfo
            {
                FileName = DisplayEasyLogFolder,
                UseShellExecute = true
            });
        }
        catch
        {
            ShowBanner(IsFrench ? "Impossible d'ouvrir le dossier des logs." : "Unable to open log folder.", "warning");
        }
    }
}
