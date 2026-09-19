using AzureEnvManager.Api.Models.Domain;

namespace AzureEnvManager.Api.Services;

public interface IAppAccessService
{
    Task<List<AzureApplication>> GetAssignedApplicationsAsync(string userObjectId, CancellationToken ct = default);

    Task<bool> HasAccessAsync(string userObjectId, Guid applicationId, CancellationToken ct = default);
}
