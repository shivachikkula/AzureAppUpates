namespace AzureEnvManager.Api.Models.Domain;

/// <summary>Metadata describing an Azure App Service tracked by this tool. The Azure resource itself is the source of truth for configuration; this row only stores enough to address it via ARM.</summary>
public class AzureApplication
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string ResourceGroup { get; set; }
    public required string SubscriptionId { get; set; }
    public AppEnvironment Environment { get; set; }
    public string DefaultHostName { get; set; } = string.Empty;

    public Guid TeamId { get; set; }
    public Team? Team { get; set; }

    public ICollection<AppAssignment> Assignments { get; set; } = new List<AppAssignment>();
}
