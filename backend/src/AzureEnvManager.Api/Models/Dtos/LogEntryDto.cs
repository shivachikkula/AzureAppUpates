namespace AzureEnvManager.Api.Models.Dtos;

public record LogEntryDto(string TimestampUtc, string Level, string Message)
{
    public static LogEntryDto Parse(string rawLine)
    {
        var level = "Info";
        var upper = rawLine.ToUpperInvariant();
        if (upper.Contains("ERR")) level = "Error";
        else if (upper.Contains("WARN")) level = "Warning";
        else if (upper.Contains("DEBUG")) level = "Debug";
        else if (upper.Contains("TRACE")) level = "Trace";

        return new LogEntryDto(DateTime.UtcNow.ToString("O"), level, rawLine);
    }
}
