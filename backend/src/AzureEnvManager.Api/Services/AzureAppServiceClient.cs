using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using AzureEnvManager.Api.Models.Domain;

namespace AzureEnvManager.Api.Services;

/// <summary>
/// Talks to Azure Resource Manager to read/write App Service configuration. Authenticates with
/// DefaultAzureCredential, so in Azure this runs as the API's managed identity; locally it falls back to
/// `az login` / Visual Studio / environment credentials. The identity needs "Website Contributor" (or an
/// equivalent custom role covering Microsoft.Web/sites/config/*) on every app it manages.
/// </summary>
public class AzureAppServiceClient : IAzureAppServiceClient
{
    private readonly ArmClient _armClient;
    private readonly ILogger<AzureAppServiceClient> _logger;

    public AzureAppServiceClient(ILogger<AzureAppServiceClient> logger)
    {
        _logger = logger;
        _armClient = new ArmClient(new DefaultAzureCredential());
    }

    public async Task SetConnectionStringAsync(AzureApplication app, string key, string value, ConnectionStringKind kind, CancellationToken ct = default)
    {
        var site = GetSiteResource(app);

        ConnectionStringDictionary existing = await site.GetConnectionStringsAsync(ct);

        existing.Properties[key] = new ConnStringValueTypePair(value, MapConnectionStringType(kind));

        await site.UpdateConnectionStringsAsync(existing, ct);

        _logger.LogInformation(
            "Updated connection string '{Key}' on {App} ({ResourceGroup}/{Subscription})",
            key,
            app.Name,
            app.ResourceGroup,
            app.SubscriptionId);
    }

    public async Task<(string Username, string Password, string ScmHostName)> GetPublishingCredentialsAsync(AzureApplication app, CancellationToken ct = default)
    {
        var site = GetSiteResource(app);

        var credentials = await site.GetPublishingCredentialsAsync(WaitUntil.Completed, ct);
        var data = credentials.Value.Data;

        var scmHostName = !string.IsNullOrEmpty(data.ScmUri)
            ? new Uri(data.ScmUri).Host
            : $"{app.Name}.scm.azurewebsites.net";

        return (data.PublishingUserName, data.PublishingPassword, scmHostName);
    }

    private WebSiteResource GetSiteResource(AzureApplication app)
    {
        var resourceId = WebSiteResource.CreateResourceIdentifier(app.SubscriptionId, app.ResourceGroup, app.Name);
        return _armClient.GetWebSiteResource(resourceId);
    }

    private static ConnectionStringType MapConnectionStringType(ConnectionStringKind kind) => kind switch
    {
        ConnectionStringKind.SQLAzure => ConnectionStringType.SqlAzure,
        ConnectionStringKind.PostgreSQL => ConnectionStringType.PostgreSQL,
        ConnectionStringKind.MySql => ConnectionStringType.MySql,
        _ => ConnectionStringType.Custom,
    };
}
