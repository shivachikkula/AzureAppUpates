using AzureEnvManager.Api.Models.Domain;

namespace AzureEnvManager.Api.Models.Dtos;

public record AzureApplicationDto(
    Guid Id,
    string Name,
    string ResourceGroup,
    string SubscriptionId,
    AppEnvironment Environment,
    string DefaultHostName)
{
    public static AzureApplicationDto FromDomain(AzureApplication app) => new(
        app.Id,
        app.Name,
        app.ResourceGroup,
        app.SubscriptionId,
        app.Environment,
        app.DefaultHostName);
}
