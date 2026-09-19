namespace AzureEnvManager.Api.Models.Domain;

/// <summary>A connection string change awaiting (or having gone through) manager approval, used for production applications.</summary>
public class ChangeRequest
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public AzureApplication? Application { get; set; }

    public required string Key { get; set; }
    public required string Value { get; set; }
    public ConnectionStringKind Type { get; set; }
    public string? Reason { get; set; }

    public required string RequestedByObjectId { get; set; }
    public required string RequestedByEmail { get; set; }
    public required string RequestedByName { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }

    public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.PendingApproval;
    public string? DecidedByObjectId { get; set; }
    public string? DecidedByName { get; set; }
    public DateTimeOffset? DecidedUtc { get; set; }
    public string? DecisionNote { get; set; }
    public string? FailureDetail { get; set; }
}
