using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using EasyLog;

namespace EasySave.WPF.ViewModels;

public partial class MainViewModel
{
    private readonly XmlSerializer _logDocumentSerializer = new(typeof(LogDocument));

    public string DisplayWorksJsonPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EasySave",
        "works.json");
    public string DisplayEasyLogFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
    public string DisplayLogFilePath => _displayLogFilePath;
    public string DisplayCryptoSoftFolder => Path.Combine(AppContext.BaseDirectory, "CryptoSoft");

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
            if (!SetField(ref _isLogFormatJson, value)) return;
            ApplyLogFormatPreference();
        }
    }

    public string[] LogDestinationChoices { get; } = ["Local", "Central", "Both"];

    private string _logDestination = "Local";
    public string LogDestination
    {
        get => _logDestination;
        set
        {
            string v = (value ?? "Local").Trim();
            if (v is not ("Local" or "Central" or "Both"))
                v = "Local";
            if (!SetField(ref _logDestination, v)) return;
            _settings.LogDestination = v;
            EnsureCentralUrlSavedIfNeeded();
            _generalSettingsService.Save(_settings);
            RefreshLogPreview();
        }
    }

    private const string DefaultCentralLogUrl = "http://localhost:5088";

    private string _centralLogBaseUrl = DefaultCentralLogUrl;
    public string CentralLogBaseUrl
    {
        get => _centralLogBaseUrl;
        set
        {
            if (!SetField(ref _centralLogBaseUrl, value ?? "")) return;
            _settings.CentralLogBaseUrl = string.IsNullOrWhiteSpace(_centralLogBaseUrl)
                ? DefaultCentralLogUrl
                : _centralLogBaseUrl.Trim();
            _centralLogBaseUrl = _settings.CentralLogBaseUrl;
            OnPropertyChanged(nameof(CentralLogBaseUrl));
            _generalSettingsService.Save(_settings);
        }
    }

    private void InitializeLogRoutingFromSettings()
    {
        string d = (_settings.LogDestination ?? "Local").Trim();
        if (d is not ("Local" or "Central" or "Both"))
            d = "Local";
        _logDestination = d;
        _centralLogBaseUrl = string.IsNullOrWhiteSpace(_settings.CentralLogBaseUrl)
            ? DefaultCentralLogUrl
            : _settings.CentralLogBaseUrl.Trim();
        EnsureCentralUrlSavedIfNeeded();
        _generalSettingsService.Save(_settings);
        OnPropertyChanged(nameof(LogDestination));
        OnPropertyChanged(nameof(CentralLogBaseUrl));
        OnPropertyChanged(nameof(DisplayCentralLogFolder));
    }

    private void EnsureCentralUrlSavedIfNeeded()
    {
        if (_logDestination is not ("Central" or "Both"))
            return;
        if (!string.IsNullOrWhiteSpace(_settings.CentralLogBaseUrl))
            return;

        _settings.CentralLogBaseUrl = DefaultCentralLogUrl;
        _centralLogBaseUrl = DefaultCentralLogUrl;
        OnPropertyChanged(nameof(CentralLogBaseUrl));
    }

    public string DisplayCentralLogFolder => ResolveCentralLogFolder();

    private static string ResolveCentralLogFolder()
    {
        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10 && !string.IsNullOrEmpty(dir); i++)
        {
            string candidate = Path.Combine(dir, "central-logs");
            if (Directory.Exists(candidate))
                return Path.GetFullPath(candidate);
            dir = Directory.GetParent(dir)?.FullName;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Pictures",
            "EasySave-ConsoleStrategy",
            "central-logs");
    }

    private string _customEncryptionExtensionInput = "";
    public string CustomEncryptionExtensionInput
    {
        get => _customEncryptionExtensionInput;
        set => SetField(ref _customEncryptionExtensionInput, value);
    }

    private string _customPriorityExtensionInput = "";
    public string CustomPriorityExtensionInput
    {
        get => _customPriorityExtensionInput;
        set => SetField(ref _customPriorityExtensionInput, value);
    }

    public ObservableCollection<ThresholdUnitOption> LargeFileThresholdUnits { get; } = new()
    {
        new ThresholdUnitOption("KB", "Ko", 1L),
        new ThresholdUnitOption("MB", "Mo", 1024L),
        new ThresholdUnitOption("GB", "Go", 1024L * 1024L),
    };

    private ThresholdUnitOption? _selectedLargeFileThresholdUnit;
    public ThresholdUnitOption? SelectedLargeFileThresholdUnit
    {
        get => _selectedLargeFileThresholdUnit;
        set
        {
            if (value is null) return;
            if (!SetField(ref _selectedLargeFileThresholdUnit, value)) return;
            _settings.LargeFileThresholdUnit = value.Code;
            ApplyLargeFileThresholdInput();
        }
    }

    private string _largeFileThresholdValueInput = "0";
    public string LargeFileThresholdValueInput
    {
        get => _largeFileThresholdValueInput;
        set
        {
            string sanitized = (value ?? "").Trim();
            if (!SetField(ref _largeFileThresholdValueInput, sanitized)) return;
            ApplyLargeFileThresholdInput();
        }
    }

    public bool IsLargeFileThresholdEnabled => _settings.LargeFileThresholdKB > 0;

    public string LargeFileThresholdSummary
    {
        get
        {
            if (_settings.LargeFileThresholdKB <= 0)
            {
                return IsFrench ? "Règle désactivée." : "Rule disabled.";
            }

            string pretty = FormatThresholdPretty(_settings.LargeFileThresholdKB);
            return IsFrench
                ? $"Pas de transfert simultané de fichiers > {pretty}"
                : $"No parallel transfer of files > {pretty}";
        }
    }

    public sealed class ThresholdUnitOption
    {
        public ThresholdUnitOption(string code, string label, long factorKB)
        {
            Code = code;
            Label = label;
            FactorKB = factorKB;
        }

        public string Code { get; }
        public string Label { get; }
        public long FactorKB { get; }
    }

    private static string FormatThresholdPretty(int kb)
    {
        if (kb >= 1024 * 1024 && kb % (1024 * 1024) == 0) return $"{kb / (1024 * 1024)} Go";
        if (kb >= 1024 && kb % 1024 == 0) return $"{kb / 1024} Mo";
        return $"{kb} Ko";
    }

    public string SelectedEncryptionExtensionsDisplay => string.Join("; ",
        EncryptionExtensionOptions.Where(x => x.IsSelected).Select(x => x.Extension).OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

    public string SelectedPriorityExtensionsDisplay => string.Join("; ",
        PriorityExtensionOptions.Where(x => x.IsSelected).Select(x => x.Extension).OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

    private void InitializeLogFormatFromSettings()
    {
        _isLogFormatJson = !string.Equals(_settings.LogFormat, "xml", StringComparison.OrdinalIgnoreCase);
        LogFormatSettings.Current = _isLogFormatJson ? LogFormat.Json : LogFormat.Xml;
    }

    private bool _suppressLargeFileThresholdApply;

    private void InitializeLargeFileThresholdFromSettings()
    {
        int kb = Math.Max(0, _settings.LargeFileThresholdKB);
        _settings.LargeFileThresholdKB = kb;

        ThresholdUnitOption unit = LargeFileThresholdUnits.FirstOrDefault(u =>
            string.Equals(u.Code, _settings.LargeFileThresholdUnit, StringComparison.OrdinalIgnoreCase))
            ?? PickBestUnit(kb);

        _selectedLargeFileThresholdUnit = unit;
        _settings.LargeFileThresholdUnit = unit.Code;

        _suppressLargeFileThresholdApply = true;
        try
        {
            _largeFileThresholdValueInput = ConvertKBToUnitDisplay(kb, unit);
        }
        finally
        {
            _suppressLargeFileThresholdApply = false;
        }

        OnPropertyChanged(nameof(SelectedLargeFileThresholdUnit));
        OnPropertyChanged(nameof(LargeFileThresholdValueInput));
        OnPropertyChanged(nameof(IsLargeFileThresholdEnabled));
        OnPropertyChanged(nameof(LargeFileThresholdSummary));
    }

    private void ApplyLargeFileThresholdInput()
    {
        if (_suppressLargeFileThresholdApply) return;

        long valueInUnit = 0;
        if (!string.IsNullOrWhiteSpace(_largeFileThresholdValueInput))
        {
            if (!long.TryParse(_largeFileThresholdValueInput, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out valueInUnit) || valueInUnit < 0)
            {
                valueInUnit = 0;
            }
        }

        long factor = _selectedLargeFileThresholdUnit?.FactorKB ?? 1L;
        long totalKB = valueInUnit * factor;
        if (totalKB > int.MaxValue) totalKB = int.MaxValue;

        int kb = (int)totalKB;
        bool changed = _settings.LargeFileThresholdKB != kb
                       || !string.Equals(_settings.LargeFileThresholdUnit, _selectedLargeFileThresholdUnit?.Code, StringComparison.OrdinalIgnoreCase);

        _settings.LargeFileThresholdKB = kb;
        if (_selectedLargeFileThresholdUnit is not null)
            _settings.LargeFileThresholdUnit = _selectedLargeFileThresholdUnit.Code;

        if (changed)
        {
            _generalSettingsService.Save(_settings);
        }

        OnPropertyChanged(nameof(IsLargeFileThresholdEnabled));
        OnPropertyChanged(nameof(LargeFileThresholdSummary));
    }

    public void IncreaseLargeFileThreshold() => StepLargeFileThreshold(+1);
    public void DecreaseLargeFileThreshold() => StepLargeFileThreshold(-1);

    private void StepLargeFileThreshold(int delta)
    {
        long current = 0;
        long.TryParse(_largeFileThresholdValueInput, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out current);
        long next = Math.Max(0, current + delta);
        LargeFileThresholdValueInput = next.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static ThresholdUnitOption PickBestUnit(int kb)
    {
        if (kb >= 1024 * 1024 && kb % (1024 * 1024) == 0)
            return new ThresholdUnitOption("GB", "Go", 1024L * 1024L);
        if (kb >= 1024 && kb % 1024 == 0)
            return new ThresholdUnitOption("MB", "Mo", 1024L);
        return new ThresholdUnitOption("KB", "Ko", 1L);
    }

    private static string ConvertKBToUnitDisplay(int kb, ThresholdUnitOption unit)
    {
        if (unit is null || unit.FactorKB <= 0) return "0";
        long value = kb / unit.FactorKB;
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private void InitializeEncryptionExtensions()
    {
        string[] defaults = [".txt", ".docx", ".pdf", ".xlsx", ".pptx", ".zip", ".json", ".xml"];
        HashSet<string> selected = _settings.EncryptedExtensions
            .Select(NormalizeExtension)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        EncryptionExtensionOptions.Clear();
        foreach (string extension in defaults.Concat(selected).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
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
        if (string.IsNullOrWhiteSpace(normalized)) return;

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
        if (e.PropertyName != nameof(ExtensionOptionViewModel.IsSelected)) return;
        SaveEncryptionExtensionsFromOptions();
        OnPropertyChanged(nameof(SelectedEncryptionExtensionsDisplay));
    }

    private void SaveEncryptionExtensionsFromOptions()
    {
        _settings.EncryptedExtensions = EncryptionExtensionOptions
            .Where(x => x.IsSelected)
            .Select(x => NormalizeExtension(x.Extension))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _settings.BusinessSoftwareNames = BusinessSoftwareNames.ToList();
        _generalSettingsService.Save(_settings);
    }

    private void InitializePriorityExtensions()
    {
        string[] defaults = [".docx", ".pdf", ".xlsx", ".sql", ".json", ".xml", ".mdb"];
        HashSet<string> selected = _settings.PriorityExtensions
            .Select(NormalizeExtension)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        PriorityExtensionOptions.Clear();
        foreach (string extension in defaults.Concat(selected).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var option = new ExtensionOptionViewModel(extension, selected.Contains(extension));
            option.PropertyChanged += OnPriorityOptionPropertyChanged;
            PriorityExtensionOptions.Add(option);
        }

        SavePriorityExtensionsFromOptions();
    }

    private void AddCustomPriorityExtension()
    {
        string normalized = NormalizeExtension(CustomPriorityExtensionInput);
        if (string.IsNullOrWhiteSpace(normalized)) return;

        ExtensionOptionViewModel? existing = PriorityExtensionOptions
            .FirstOrDefault(x => string.Equals(x.Extension, normalized, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            existing = new ExtensionOptionViewModel(normalized, true);
            existing.PropertyChanged += OnPriorityOptionPropertyChanged;
            PriorityExtensionOptions.Add(existing);
        }
        else
        {
            existing.IsSelected = true;
        }

        CustomPriorityExtensionInput = "";
        SavePriorityExtensionsFromOptions();
        OnPropertyChanged(nameof(SelectedPriorityExtensionsDisplay));
    }

    private void OnPriorityOptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ExtensionOptionViewModel.IsSelected)) return;
        SavePriorityExtensionsFromOptions();
        OnPropertyChanged(nameof(SelectedPriorityExtensionsDisplay));
    }

    private void SavePriorityExtensionsFromOptions()
    {
        _settings.PriorityExtensions = PriorityExtensionOptions
            .Where(x => x.IsSelected)
            .Select(x => NormalizeExtension(x.Extension))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _settings.BusinessSoftwareNames = BusinessSoftwareNames.ToList();
        _generalSettingsService.Save(_settings);
    }

    private static string NormalizeExtension(string value)
    {
        string v = value.Trim();
        if (string.IsNullOrWhiteSpace(v)) return string.Empty;
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
        if (string.Equals(_settings.LogDestination, "Central", StringComparison.OrdinalIgnoreCase))
        {
            string centralDir = DisplayCentralLogFolder;
            string ndjson = Path.Combine(centralDir, DateTime.UtcNow.ToString("yyyy-MM-dd") + ".ndjson");
            _displayLogFilePath = File.Exists(ndjson) ? ndjson : centralDir;
            OnPropertyChanged(nameof(DisplayLogFilePath));

            if (File.Exists(ndjson))
            {
                try
                {
                    string[] lines = File.ReadAllLines(ndjson);
                    int take = Math.Min(25, lines.Length);
                    LogPreview = string.Join(Environment.NewLine, lines.Skip(Math.Max(0, lines.Length - take)));
                }
                catch
                {
                    LogPreview = IsFrench
                        ? $"Fichier central : {ndjson}"
                        : $"Central file: {ndjson}";
                }
            }
            else
            {
                LogPreview = IsFrench
                    ? $"Mode Central — pas encore de fichier ici :{Environment.NewLine}{ndjson}{Environment.NewLine}{Environment.NewLine}1) docker compose up -d{Environment.NewLine}2) URL : http://localhost:5088{Environment.NewLine}3) Lancez une sauvegarde puis Rafraîchir."
                    : $"Central mode — no file yet at:{Environment.NewLine}{ndjson}{Environment.NewLine}{Environment.NewLine}1) docker compose up -d{Environment.NewLine}2) URL: http://localhost:5088{Environment.NewLine}3) Run a backup, then Refresh.";
            }

            OnPropertyChanged(nameof(HasLogPreview));
            return;
        }

        string extension = IsLogFormatJson ? ".json" : ".xml";
        _displayLogFilePath = "";
        OnPropertyChanged(nameof(DisplayLogFilePath));

        if (!Directory.Exists(DisplayEasyLogFolder))
        {
            LogPreview = IsFrench ? "Le dossier de logs n'existe pas encore." : "The log folder does not exist yet.";
            OnPropertyChanged(nameof(HasLogPreview));
            return;
        }

        FileInfo? latest = new DirectoryInfo(DisplayEasyLogFolder).GetFiles("*" + extension).OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
        if (latest is null)
        {
            LogPreview = IsFrench ? $"Aucun log {extension} disponible pour le moment." : $"No {extension} log available yet.";
            OnPropertyChanged(nameof(HasLogPreview));
            return;
        }

        _displayLogFilePath = latest.FullName;
        OnPropertyChanged(nameof(DisplayLogFilePath));

        try
        {
            List<LogEntry> entries = IsLogFormatJson ? ReadJsonLogEntries(latest.FullName) : ReadXmlLogEntries(latest.FullName);
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
        return (_logDocumentSerializer.Deserialize(stream) as LogDocument)?.Entries ?? [];
    }

    private string BuildLogPreview(List<LogEntry> entries)
    {
        if (entries.Count == 0) return IsFrench ? "Le fichier de log est vide." : "The log file is empty.";

        StringBuilder sb = new();
        foreach (LogEntry entry in entries.TakeLast(25))
        {
            string status = entry.Success ? "OK" : (IsFrench ? "ERREUR" : "ERROR");
            sb.Append($"{entry.Timestamp} | {entry.WorkName} | {status} | {Path.GetFileName(entry.SourceFile)} -> {Path.GetFileName(entry.DestinationFile)} | {entry.TransferTimeMs}ms");
            if (!entry.Success && !string.IsNullOrWhiteSpace(entry.ErrorMessage))
                sb.Append(" | " + entry.ErrorMessage);
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private void OpenLogsFolder()
    {
        try
        {
            Directory.CreateDirectory(DisplayEasyLogFolder);
            Process.Start(new ProcessStartInfo { FileName = DisplayEasyLogFolder, UseShellExecute = true });
        }
        catch
        {
            ShowBanner(IsFrench ? "Impossible d'ouvrir le dossier des logs." : "Unable to open log folder.", "warning");
        }
    }
}
