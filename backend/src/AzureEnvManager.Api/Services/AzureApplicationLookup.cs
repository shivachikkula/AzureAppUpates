using AzureEnvManager.Api.Data;
using AzureEnvManager.Api.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace AzureEnvManager.Api.Services;

public class AzureApplicationLookup(AppDbContext db) : IAzureApplicationLookup
{
    public Task<AzureApplication?> GetAsync(Guid applicationId, CancellationToken ct = default) =>
        db.Applications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
}
