using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LibrarySystem.Contracts.Protos;
using System.Text;
using System.Text.Json;

namespace LibrarySystem.Grpc.Services;

public class WafLogService : Security.SecurityBase
{
    private readonly ILogger<WafLogService> _logger;
    private const string LogPath = "/var/log/waf/audit.json";

    public WafLogService(ILogger<WafLogService> logger)
    {
        _logger = logger;
    }

    public override async Task WatchWafLogs(Empty request, IServerStreamWriter<WafLogEntry> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("Client connected to WAF Log Stream.");

        if (!File.Exists(LogPath))
        {
            await responseStream.WriteAsync(new WafLogEntry
            {
                Timestamp = DateTime.UtcNow.ToString("O"),
                Details = $"Log file not found at {LogPath}. Is the WAF container running?"
            });
            return;
        }

        using var fileStream = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fileStream, Encoding.UTF8);

        fileStream.Seek(0, SeekOrigin.End);

        while (!context.CancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(context.CancellationToken);

            if (line != null)
            {
                var entry = ParseLogLine(line);
                if (entry != null)
                {
                    await responseStream.WriteAsync(entry);
                }
            }
            else
            {
                try { await Task.Delay(500, context.CancellationToken); }
                catch (OperationCanceledException) { break; }
            }
        }
        
        _logger.LogInformation("Client disconnected from WAF Log Stream.");
    }

    private WafLogEntry? ParseLogLine(string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            var transaction = root.TryGetProperty("transaction", out var tx) ? tx : root;
            
            var timestamp = transaction.TryGetProperty("timestamp", out var ts) ? ts.GetString() : DateTime.UtcNow.ToString("HH:mm:ss");
            var clientIp = transaction.TryGetProperty("client_ip", out var ip) ? ip.GetString() : "Unknown";
            var request = transaction.TryGetProperty("request", out var req) ? req : default;
            var uri = request.ValueKind != JsonValueKind.Undefined && request.TryGetProperty("uri", out var u) ? u.GetString() : "/";
            
            var isInterrupted = transaction.TryGetProperty("is_interrupted", out var interrupted)
                && interrupted.ValueKind == JsonValueKind.True;
            var action = isInterrupted ? "Blocked" : "Allowed";

            var ruleId = "N/A";
            if (transaction.TryGetProperty("producer", out var producer)
                && producer.TryGetProperty("rulesets", out var rulesets)
                && rulesets.ValueKind == JsonValueKind.Array
                && rulesets.GetArrayLength() > 0)
            {
                ruleId = rulesets[0].GetString() ?? "N/A";
            }

            var details = "";
            if (request.ValueKind != JsonValueKind.Undefined
                && request.TryGetProperty("body", out var body))
            {
                // Strip protobuf binary framing bytes, keep printable ASCII payload
                var raw = body.GetString() ?? "";
                details = System.Text.RegularExpressions.Regex.Replace(raw, @"[^\x20-\x7E]", " ")
                               .Trim()
                               .Replace("  ", " ");
            }
            if (string.IsNullOrWhiteSpace(details))
                details = uri ?? "";

            return new WafLogEntry
            {
                Timestamp  = timestamp ?? "",
                ClientIp   = clientIp ?? "",
                RequestUri = uri ?? "",
                Action     = action,
                RuleId     = ruleId,
                Details    = details.Length > 200 ? details[..200] + "…" : details
            };
        }
        catch (Exception)
        {
            return new WafLogEntry { Details = "Raw: " + (line.Length > 100 ? line.Substring(0, 100) : line) };
        }
    }
}