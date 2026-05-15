using System.Text.Json;

namespace EasySave.LogCentral;

public sealed class CentralLogEntryDto
{
    public string Timestamp { get; set; } = "";
    public string WorkName { get; set; } = "";
    public string SourceFile { get; set; } = "";
    public string DestinationFile { get; set; } = "";
    public long FileSize { get; set; }
    public long TransferTimeMs { get; set; }
    public long EncryptionTimeMs { get; set; }
    public bool Success { get; set; } = true;
    public string ErrorMessage { get; set; } = "";
    public string ClientUser { get; set; } = "";
    public string ClientMachine { get; set; } = "";
    public string ClientId { get; set; } = "";
}

public sealed class DailyLogStorage(string directory)
{
    private static readonly JsonSerializerOptions JsonLine = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly object _lock = new();
    private readonly string _dir = directory;

    public Task AppendAsync(CentralLogEntryDto dto, CancellationToken ct = default)
    {
        string line = JsonSerializer.Serialize(dto, JsonLine) + Environment.NewLine;
        string path = Path.Combine(_dir, $"{DateTime.UtcNow:yyyy-MM-dd}.ndjson");

        lock (_lock)
        {
            Directory.CreateDirectory(_dir);
            File.AppendAllText(path, line);
        }

        return Task.CompletedTask;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        string logDir = builder.Configuration["LOG_DATA_DIR"]
            ?? Environment.GetEnvironmentVariable("LOG_DATA_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, "logs");

        Console.WriteLine($"LogCentral — journaux : {Path.GetFullPath(logDir)}");
        builder.Services.AddSingleton(new DailyLogStorage(logDir));
        builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
            p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

        WebApplication app = builder.Build();
        app.UseCors();

        app.MapGet("/", () => Results.Content(
            """
            <!DOCTYPE html><html lang="fr"><head><meta charset="utf-8"><title>EasySave LogCentral</title>
            <style>body{font-family:Segoe UI,sans-serif;max-width:640px;margin:2rem auto;padding:0 1rem}
            code{background:#eee;padding:2px 6px;border-radius:4px}</style></head><body>
            <h1>EasySave — LogCentral</h1>
            <p>Service de centralisation des journaux (Docker). <strong>Ce n’est pas une page à utiliser à la main</strong> : EasySave envoie les logs en POST.</p>
            <ul>
            <li><a href="/health">/health</a> — test (doit afficher OK)</li>
            <li><code>POST /api/logs</code> — réception des entrées (JSON)</li>
            </ul>
            <p>Fichiers sur le serveur : un <code>.ndjson</code> par jour dans le dossier monté <code>central-logs</code> à la racine du projet.</p>
            <p>URL à mettre dans EasySave : <code>http://localhost:5088</code></p>
            </body></html>
            """,
            "text/html; charset=utf-8"));

        app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

        app.MapPost("/api/logs", async (CentralLogEntryDto? dto, DailyLogStorage storage, CancellationToken ct) =>
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.Timestamp))
                return Results.BadRequest();
            await storage.AppendAsync(dto, ct);
            return Results.Accepted();
        });

        app.Run();
    }
}
