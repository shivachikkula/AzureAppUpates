using AzureEnvManager.Api.Data;
using AzureEnvManager.Api.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace AzureEnvManager.Api.Services;

public class AppAccessService(AppDbContext db) : IAppAccessService
{
    public async Task<List<AzureApplication>> GetAssignedApplicationsAsync(string userObjectId, CancellationToken ct = default)
    {
        return await db.Assignments
            .Where(a => a.UserObjectId == userObjectId)
            .Select(a => a.Application!)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);
    }

    public async Task<bool> HasAccessAsync(string userObjectId, Guid applicationId, CancellationToken ct = default)
    {
        return await db.Assignments.AnyAsync(
            a => a.UserObjectId == userObjectId && a.ApplicationId == applicationId,
            ct);
    }
}
