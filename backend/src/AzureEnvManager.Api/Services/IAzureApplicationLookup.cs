using AzureEnvManager.Api.Models.Domain;

namespace AzureEnvManager.Api.Services;

public interface IAzureApplicationLookup
{
    Task<AzureApplication?> GetAsync(Guid applicationId, CancellationToken ct = default);
}
