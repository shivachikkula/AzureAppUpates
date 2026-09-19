namespace AzureEnvManager.Api.Models.Domain;

/// <summary>Grants a developer, identified by their Entra ID object id, access to manage one application.</summary>
public class AppAssignment
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public AzureApplication? Application { get; set; }

    public required string UserObjectId { get; set; }
    public required string UserEmail { get; set; }
    public required string UserDisplayName { get; set; }
}
