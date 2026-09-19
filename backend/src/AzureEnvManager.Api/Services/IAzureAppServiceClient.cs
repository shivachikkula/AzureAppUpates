using AzureEnvManager.Api.Models.Domain;

namespace AzureEnvManager.Api.Services;

public interface IAzureAppServiceClient
{
    /// <summary>Creates or updates a single connection string on the target App Service via Azure Resource Manager.</summary>
    Task SetConnectionStringAsync(AzureApplication app, string key, string value, ConnectionStringKind kind, CancellationToken ct = default);

    /// <summary>Retrieves SCM (Kudu) publishing credentials used to open the live log stream for an app.</summary>
    Task<(string Username, string Password, string ScmHostName)> GetPublishingCredentialsAsync(AzureApplication app, CancellationToken ct = default);
}
