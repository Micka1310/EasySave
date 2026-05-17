using System.Diagnostics;
using System.Windows.Input;
using Application = System.Windows.Application;

namespace EasySave.WPF.ViewModels;

public partial class MainViewModel
{
    private string _customBusinessSoftwareInput = "";
    public string CustomBusinessSoftwareInput
    {
        get => _customBusinessSoftwareInput;
        set => SetField(ref _customBusinessSoftwareInput, value);
    }

    public string MonitoredBusinessSoftwareDisplay => string.Join("; ", BusinessSoftwareNames);
    public bool HasBusinessSoftwareConfigured => BusinessSoftwareNames.Count > 0;

    private async Task MonitorBusinessSoftwareAsync(
        WorkItemViewModel vm,
        CancellationToken token)
    {
        bool loggedCurrentDetection = false;
        while (!token.IsCancellationRequested)
        {
            if (TryGetRunningBusinessSoftware(out string processName))
            {
                if (!vm.PausedByBusinessSoftware)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        vm.PausedByBusinessSoftware = true;
                        vm.PauseRequested = true;
                        if (vm.StatusKey != "Paused")
                            vm.StatusKey = "Paused";
                        ShowBanner($"{_lang.GetString("wpf_business_software_pause")} ({processName})", "warning");
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
                if (vm.PausedByBusinessSoftware)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        vm.PausedByBusinessSoftware = false;
                        if (!vm.PausedByUser)
                        {
                            vm.PauseRequested = false;
                            vm.StatusKey = "Active";
                            ShowBanner(_lang.GetString("wpf_business_software_resume"), "info");
                        }
                    });
                }

                loggedCurrentDetection = false;
            }

            try { await Task.Delay(400, token); } catch (TaskCanceledException) { break; }
        }
    }

    private void InitializeBusinessSoftware()
    {
        List<string> configured = _settings.BusinessSoftwareNames
            .Select(NormalizeProcessName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (configured.Count == 0) configured = ["notepad", "calc"];

        BusinessSoftwareNames.Clear();
        foreach (string name in configured.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            BusinessSoftwareNames.Add(name);

        _settings.BusinessSoftwareNames = BusinessSoftwareNames.ToList();
        _generalSettingsService.Save(_settings);
        OnPropertyChanged(nameof(MonitoredBusinessSoftwareDisplay));
        OnPropertyChanged(nameof(HasBusinessSoftwareConfigured));
        CommandManager.InvalidateRequerySuggested();
    }

    private void AddBusinessSoftware()
    {
        string normalized = NormalizeProcessName(CustomBusinessSoftwareInput);
        if (string.IsNullOrWhiteSpace(normalized)) return;

        if (!BusinessSoftwareNames.Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase)))
            BusinessSoftwareNames.Add(normalized);

        List<string> ordered = BusinessSoftwareNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        BusinessSoftwareNames.Clear();
        foreach (string name in ordered)
            BusinessSoftwareNames.Add(name);

        CustomBusinessSoftwareInput = "";
        _settings.BusinessSoftwareNames = BusinessSoftwareNames.ToList();
        _generalSettingsService.Save(_settings);
        OnPropertyChanged(nameof(MonitoredBusinessSoftwareDisplay));
        OnPropertyChanged(nameof(HasBusinessSoftwareConfigured));
        CommandManager.InvalidateRequerySuggested();
    }

    private void RemoveBusinessSoftware(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return;

        string normalized = NormalizeProcessName(processName);
        string? existing = BusinessSoftwareNames.FirstOrDefault(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
        if (existing is null) return;

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

    private bool TryGetRunningBusinessSoftware(out string processName)
    {
        processName = "";
        if (BusinessSoftwareNames.Count == 0) return false;

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
            catch { }
            finally { process.Dispose(); }
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
        _logger.WriteLogs("BusinessSoftwareGuard", processName, "", 0, 0, 0, success: false, errorMessage: message);
    }

    private static string NormalizeProcessName(string value)
    {
        string v = value.Trim();
        if (string.IsNullOrWhiteSpace(v)) return string.Empty;
        if (v.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) v = v[..^4];
        return v.ToLowerInvariant();
    }
}
