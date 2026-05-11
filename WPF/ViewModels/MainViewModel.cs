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
    private readonly Logger _logger = new Logger();
    private readonly GeneralSettingsService _generalSettingsService = new GeneralSettingsService();
    private readonly XmlSerializer _logDocumentSerializer = new(typeof(LogDocument));
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

    private string _customBusinessSoftwareInput = "";
    public string CustomBusinessSoftwareInput
    {
        get => _customBusinessSoftwareInput;
        set => SetField(ref _customBusinessSoftwareInput, value);
    }

    public string MonitoredBusinessSoftwareDisplay => string.Join("; ", BusinessSoftwareNames);

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
    public string LblSettingsSectionBusinessSoftware => _lang.GetString("wpf_settings_section_business_software");
    public string LblSettingsBusinessSoftwareLabel => _lang.GetString("wpf_settings_business_software_label");
    public string LblSettingsBusinessSoftwareHint => _lang.GetString("wpf_settings_business_software_hint");
    public string LblSettingsBusinessSoftwareAddButton => _lang.GetString("wpf_settings_business_software_add_button");
    public string LblSettingsBusinessSoftwareList => _lang.GetString("wpf_settings_business_software_list");
    public string LblSettingsBusinessSoftwareResetDefaults => _lang.GetString("wpf_settings_business_software_reset_defaults");
    public string LblSettingsBusinessSoftwareClearAll => _lang.GetString("wpf_settings_business_software_clear_all");
    public string LblSettingsBusinessSoftwareEmpty => _lang.GetString("wpf_settings_business_software_empty");
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
        OnPropertyChanged(nameof(LblSettingsSectionBusinessSoftware));
        OnPropertyChanged(nameof(LblSettingsBusinessSoftwareLabel));
        OnPropertyChanged(nameof(LblSettingsBusinessSoftwareHint));
        OnPropertyChanged(nameof(LblSettingsBusinessSoftwareAddButton));
        OnPropertyChanged(nameof(LblSettingsBusinessSoftwareList));
        OnPropertyChanged(nameof(LblSettingsBusinessSoftwareResetDefaults));
        OnPropertyChanged(nameof(LblSettingsBusinessSoftwareClearAll));
        OnPropertyChanged(nameof(LblSettingsBusinessSoftwareEmpty));
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

                Progress<WorkState> reporter = new Progress<WorkState>(state =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        vm.UpdateFromState(state);
                    });
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

    private async Task MonitorBusinessSoftwareAsync(
        WorkItemViewModel vm,
        Func<bool> getPauseRequested,
        Action<bool> setPauseRequested,
        CancellationToken token)
    {
        bool loggedCurrentDetection = false;

        while (!token.IsCancellationRequested)
        {
            if (TryGetRunningBusinessSoftware(out string processName))
            {
                if (!getPauseRequested())
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        setPauseRequested(true);
                        vm.StatusKey = "Paused";
                        string text = $"{_lang.GetString("wpf_business_software_pause")} ({processName})";
                        ShowBanner(text, "warning");
                    });
                }

                if (!loggedCurrentDetection)
                {
                    LogBusinessSoftwareEvent(processName, _lang.GetString("wpf_business_software_log"));
                    loggedCurrentDetection = true;
                }
            }
            else
            {
                if (getPauseRequested())
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        setPauseRequested(false);
                        vm.StatusKey = "Active";
                        ShowBanner(_lang.GetString("wpf_business_software_resume"), "info");
                    });
                }

                loggedCurrentDetection = false;
            }

            try
            {
                await Task.Delay(400, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
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

    private void InitializeBusinessSoftware()
    {
        List<string> configured = _settings.BusinessSoftwareNames
            .Select(NormalizeProcessName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (configured.Count == 0)
        {
            configured = ["notepad", "calc"];
        }

        BusinessSoftwareNames.Clear();
        foreach (string name in configured.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            BusinessSoftwareNames.Add(name);
        }

        _settings.BusinessSoftwareNames = BusinessSoftwareNames.ToList();
        _generalSettingsService.Save(_settings);
        OnPropertyChanged(nameof(MonitoredBusinessSoftwareDisplay));
        OnPropertyChanged(nameof(HasBusinessSoftwareConfigured));
        CommandManager.InvalidateRequerySuggested();
    }

    private void AddBusinessSoftware()
    {
        string normalized = NormalizeProcessName(CustomBusinessSoftwareInput);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (!BusinessSoftwareNames.Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            BusinessSoftwareNames.Add(normalized);
        }

        List<string> ordered = BusinessSoftwareNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        BusinessSoftwareNames.Clear();
        foreach (string name in ordered)
        {
            BusinessSoftwareNames.Add(name);
        }

        CustomBusinessSoftwareInput = "";
        _settings.BusinessSoftwareNames = BusinessSoftwareNames.ToList();
        _generalSettingsService.Save(_settings);
        OnPropertyChanged(nameof(MonitoredBusinessSoftwareDisplay));
        OnPropertyChanged(nameof(HasBusinessSoftwareConfigured));
        CommandManager.InvalidateRequerySuggested();
    }

    private void RemoveBusinessSoftware(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return;
        }

        string normalized = NormalizeProcessName(processName);
        string? existing = BusinessSoftwareNames
            .FirstOrDefault(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            return;
        }

        BusinessSoftwareNames.Remove(existing);
        _settings.BusinessSoftwareNames = BusinessSoftwareNames.ToList();
        _generalSettingsService.Save(_settings);
        OnPropertyChanged(nameof(MonitoredBusinessSoftwareDisplay));
        OnPropertyChanged(nameof(HasBusinessSoftwareConfigured));
        CommandManager.InvalidateRequerySuggested();
    }

    private void ResetBusinessSoftwareDefaults()
    {
        BusinessSoftwareNames.Clear();
        BusinessSoftwareNames.Add("notepad");
        BusinessSoftwareNames.Add("calc");
        _settings.BusinessSoftwareNames = BusinessSoftwareNames.ToList();
        _generalSettingsService.Save(_settings);
        OnPropertyChanged(nameof(MonitoredBusinessSoftwareDisplay));
        OnPropertyChanged(nameof(HasBusinessSoftwareConfigured));
        CommandManager.InvalidateRequerySuggested();
    }

    private void ClearBusinessSoftware()
    {
        BusinessSoftwareNames.Clear();
        _settings.BusinessSoftwareNames = [];
        _generalSettingsService.Save(_settings);
        OnPropertyChanged(nameof(MonitoredBusinessSoftwareDisplay));
        OnPropertyChanged(nameof(HasBusinessSoftwareConfigured));
        CommandManager.InvalidateRequerySuggested();
    }

    public bool HasBusinessSoftwareConfigured => BusinessSoftwareNames.Count > 0;

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

        _settings.EncryptedExtensions = selected;
        _settings.BusinessSoftwareNames = BusinessSoftwareNames.ToList();
        _generalSettingsService.Save(_settings);
    }

    private bool TryGetRunningBusinessSoftware(out string processName)
    {
        processName = "";
        if (BusinessSoftwareNames.Count == 0)
        {
            return false;
        }

        HashSet<string> monitored = BuildExpandedProcessNames(BusinessSoftwareNames);

        foreach (Process process in Process.GetProcesses())
        {
            try
            {
                if (monitored.Contains(process.ProcessName))
                {
                    processName = process.ProcessName;
                    return true;
                }
            }
            catch
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        return false;
    }

    private static HashSet<string> BuildExpandedProcessNames(IEnumerable<string> names)
    {
        HashSet<string> expanded = new(StringComparer.OrdinalIgnoreCase);

        foreach (string raw in names.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            string name = raw.Trim();
            expanded.Add(name);

            if (string.Equals(name, "calc", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "calculatrice", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "calculator", StringComparison.OrdinalIgnoreCase))
            {
                expanded.Add("calc");
                expanded.Add("CalculatorApp");
                expanded.Add("calculator");
                expanded.Add("Win32Calc");
            }
        }

        return expanded;
    }

    private void LogBusinessSoftwareEvent(string processName, string message)
    {
        _logger.WriteLogs(
            "BusinessSoftwareGuard",
            processName,
            "",
            0,
            0,
            0,
            success: false,
            errorMessage: message);
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

    private static string NormalizeProcessName(string value)
    {
        string v = value.Trim();
        if (string.IsNullOrWhiteSpace(v))
        {
            return string.Empty;
        }

        if (v.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            v = v[..^4];
        }

        return v.ToLowerInvariant();
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
