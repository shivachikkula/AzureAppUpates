namespace AzureEnvManager.Api.Services;

public interface ITeamAccessService
{
    Task<List<Guid>> GetManagedTeamIdsAsync(string userObjectId, CancellationToken ct = default);

    Task<bool> IsManagerOfTeamAsync(string userObjectId, Guid teamId, CancellationToken ct = default);
}
