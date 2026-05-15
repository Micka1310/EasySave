using System.Text.Json;

namespace EasyLog;

public readonly record struct LogRoutingConfig(bool WriteLocal, bool WriteCentral, string CentralBaseUrl);

public static class CentralLogRouting
{
    public static LogRoutingConfig Load()
    {
        string dest = "Local";
        string url = "";

        string? envDest = Environment.GetEnvironmentVariable("EASYSAVE_LOG_DESTINATION");
        string? envUrl = Environment.GetEnvironmentVariable("EASYSAVE_CENTRAL_LOG_URL");

        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EasySave",
            "general-settings.json");

        if (File.Exists(path))
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
                JsonElement root = doc.RootElement;
                if (TryGetStringInsensitive(root, "logDestination", out string? d))
                    dest = d ?? "Local";
                if (TryGetStringInsensitive(root, "centralLogBaseUrl", out string? u))
                    url = u ?? "";
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(envDest))
            dest = envDest.Trim();
        if (!string.IsNullOrWhiteSpace(envUrl))
            url = envUrl.Trim();

        dest = dest.Trim();
        if (!dest.Equals("Local", StringComparison.OrdinalIgnoreCase)
            && !dest.Equals("Central", StringComparison.OrdinalIgnoreCase)
            && !dest.Equals("Both", StringComparison.OrdinalIgnoreCase))
        {
            dest = "Local";
        }

        bool writeCentral = dest.Equals("Central", StringComparison.OrdinalIgnoreCase)
            || dest.Equals("Both", StringComparison.OrdinalIgnoreCase);
        bool writeLocal = dest.Equals("Local", StringComparison.OrdinalIgnoreCase)
            || dest.Equals("Both", StringComparison.OrdinalIgnoreCase);

        url = url.Trim().TrimEnd('/');
        if (writeCentral && string.IsNullOrWhiteSpace(url))
            url = "http://localhost:5088";

        return new LogRoutingConfig(writeLocal, writeCentral, url);
    }

    private static bool TryGetStringInsensitive(JsonElement root, string name, out string? value)
    {
        value = null;
        foreach (JsonProperty p in root.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = p.Value.GetString();
                return true;
            }
        }

        return false;
    }
}
