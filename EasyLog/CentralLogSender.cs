using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace EasyLog;

internal static class CentralLogSender
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static void TrySendInBackground(LogEntry entry, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return;

        CentralPayload payload = CentralPayload.From(entry);
        _ = Task.Run(() => TrySendOnceAsync(payload, baseUrl));
    }

    private static async Task TrySendOnceAsync(CentralPayload payload, string baseUrl)
    {
        try
        {
            string url = baseUrl.Trim().TrimEnd('/') + "/api/logs";
            string json = JsonSerializer.Serialize(payload, JsonOpts);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await Http.PostAsync(url, content).ConfigureAwait(false);
            _ = response.IsSuccessStatusCode;
        }
        catch { }
    }

    private sealed class CentralPayload
    {
        public string Timestamp { get; set; } = "";
        public string WorkName { get; set; } = "";
        public string SourceFile { get; set; } = "";
        public string DestinationFile { get; set; } = "";
        public long FileSize { get; set; }
        public long TransferTimeMs { get; set; }
        public long EncryptionTimeMs { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string ClientUser { get; set; } = "";
        public string ClientMachine { get; set; } = "";
        public string ClientId { get; set; } = "";

        public static CentralPayload From(LogEntry e) => new()
        {
            Timestamp = e.Timestamp,
            WorkName = e.WorkName,
            SourceFile = e.SourceFile,
            DestinationFile = e.DestinationFile,
            FileSize = e.FileSize,
            TransferTimeMs = e.TransferTimeMs,
            EncryptionTimeMs = e.EncryptionTimeMs,
            Success = e.Success,
            ErrorMessage = e.ErrorMessage,
            ClientUser = Environment.UserName,
            ClientMachine = Environment.MachineName,
            ClientId = ClientInstanceId.Get()
        };
    }
}
