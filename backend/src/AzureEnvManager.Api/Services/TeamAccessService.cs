using AzureEnvManager.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AzureEnvManager.Api.Services;

public class TeamAccessService(AppDbContext db) : ITeamAccessService
{
    public Task<List<Guid>> GetManagedTeamIdsAsync(string userObjectId, CancellationToken ct = default) =>
        db.ManagerAssignments
            .Where(m => m.UserObjectId == userObjectId)
            .Select(m => m.TeamId)
            .ToListAsync(ct);

    public Task<bool> IsManagerOfTeamAsync(string userObjectId, Guid teamId, CancellationToken ct = default) =>
        db.ManagerAssignments.AnyAsync(m => m.UserObjectId == userObjectId && m.TeamId == teamId, ct);
}
