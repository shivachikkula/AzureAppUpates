namespace AzureEnvManager.Api.Models.Domain;

/// <summary>
/// Owns a set of applications. Production change requests for an app are only visible to, and decidable
/// by, managers assigned to that app's team (or an Admin, who can see/decide everything).
/// </summary>
public class Team
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    public ICollection<AzureApplication> Applications { get; set; } = new List<AzureApplication>();
    public ICollection<ManagerTeamAssignment> Managers { get; set; } = new List<ManagerTeamAssignment>();
}
