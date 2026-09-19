namespace AzureEnvManager.Api.Models.Domain;

/// <summary>Grants a manager (Entra ID object id) the ability to see and decide production change requests for one team's applications.</summary>
public class ManagerTeamAssignment
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Team? Team { get; set; }

    public required string UserObjectId { get; set; }
    public required string UserEmail { get; set; }
    public required string UserDisplayName { get; set; }
}
