using System.Text.Json;
using System.IO;

namespace EasySave.WPF.Services;

public sealed class GeneralSettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave");
    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "general-settings.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public GeneralSettings Load()
    {
        Directory.CreateDirectory(SettingsDirectory);

        if (!File.Exists(SettingsPath))
        {
            return new GeneralSettings();
        }

        try
        {
            string json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<GeneralSettings>(json) ?? new GeneralSettings();
        }
        catch
        {
            return new GeneralSettings();
        }
    }

    public void Save(GeneralSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        string json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }
}

public sealed class GeneralSettings
{
    public List<string> EncryptedExtensions { get; set; } = [];
    public string LogFormat { get; set; } = "json";
}
