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

    public string SelectedEncryptionExtensionsDisplay => string.Join("; ",
        EncryptionExtensionOptions.Where(x => x.IsSelected).Select(x => x.Extension).OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

    public string SelectedPriorityExtensionsDisplay => string.Join("; ",
        PriorityExtensionOptions.Where(x => x.IsSelected).Select(x => x.Extension).OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

    private void InitializeLogFormatFromSettings()
    {
        _isLogFormatJson = !string.Equals(_settings.LogFormat, "xml", StringComparison.OrdinalIgnoreCase);
        LogFormatSettings.Current = _isLogFormatJson ? LogFormat.Json : LogFormat.Xml;
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
