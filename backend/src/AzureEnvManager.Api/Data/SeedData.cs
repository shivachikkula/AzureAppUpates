using AzureEnvManager.Api.Models.Domain;

namespace AzureEnvManager.Api.Data;

/// <summary>Local-dev convenience seeding only. In real environments, applications and assignments are managed through the database directly (or a future admin UI), not baked into code.</summary>
public static class SeedData
{
    public static void EnsureSeeded(AppDbContext db)
    {
        if (db.Applications.Any())
        {
            return;
        }

        var webApp = new AzureApplication
        {
            Id = Guid.NewGuid(),
            Name = "contoso-orders-api",
            ResourceGroup = "rg-contoso-dev",
            SubscriptionId = "00000000-0000-0000-0000-000000000000",
            Environment = AppEnvironment.Development,
            DefaultHostName = "contoso-orders-api.azurewebsites.net",
        };

        var stagingApp = new AzureApplication
        {
            Id = Guid.NewGuid(),
            Name = "contoso-orders-api-staging",
            ResourceGroup = "rg-contoso-staging",
            SubscriptionId = "00000000-0000-0000-0000-000000000000",
            Environment = AppEnvironment.Staging,
            DefaultHostName = "contoso-orders-api-staging.azurewebsites.net",
        };

        var prodApp = new AzureApplication
        {
            Id = Guid.NewGuid(),
            Name = "contoso-orders-api-prod",
            ResourceGroup = "rg-contoso-prod",
            SubscriptionId = "00000000-0000-0000-0000-000000000000",
            Environment = AppEnvironment.Production,
            DefaultHostName = "contoso-orders-api-prod.azurewebsites.net",
        };

        db.Applications.AddRange(webApp, stagingApp, prodApp);
        db.SaveChanges();
    }
}
