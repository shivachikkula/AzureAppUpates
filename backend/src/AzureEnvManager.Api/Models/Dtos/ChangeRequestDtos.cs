using AzureEnvManager.Api.Models.Domain;

namespace AzureEnvManager.Api.Models.Dtos;

public record ChangeRequestDto(
    Guid Id,
    Guid ApplicationId,
    string ApplicationName,
    AppEnvironment Environment,
    string RequestedByName,
    string RequestedByEmail,
    string Key,
    string Value,
    string Type,
    string? Reason,
    ChangeRequestStatus Status,
    DateTimeOffset CreatedUtc,
    string? DecidedByName,
    DateTimeOffset? DecidedUtc,
    string? DecisionNote)
{
    public static ChangeRequestDto FromDomain(ChangeRequest cr) => new(
        cr.Id,
        cr.ApplicationId,
        cr.Application?.Name ?? string.Empty,
        cr.Application?.Environment ?? AppEnvironment.Production,
        cr.RequestedByName,
        cr.RequestedByEmail,
        cr.Key,
        cr.Value,
        cr.Type.ToString(),
        cr.Reason,
        cr.Status,
        cr.CreatedUtc,
        cr.DecidedByName,
        cr.DecidedUtc,
        cr.DecisionNote);
}

public class DecisionDto
{
    public string? Note { get; set; }
}
