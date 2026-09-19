using AzureEnvManager.Api.Authorization;
using AzureEnvManager.Api.Data;
using AzureEnvManager.Api.Extensions;
using AzureEnvManager.Api.Models.Dtos;
using AzureEnvManager.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AzureEnvManager.Api.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationsController(AppDbContext db, IAppAccessService accessService) : ControllerBase
{
    /// <summary>Applications assigned to the signed-in developer.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<AzureApplicationDto>>> GetMine(CancellationToken ct)
    {
        var apps = await accessService.GetAssignedApplicationsAsync(User.GetObjectId(), ct);
        return Ok(apps.Select(AzureApplicationDto.FromDomain));
    }

    /// <summary>All tracked applications; used by managers reviewing approvals.</summary>
    [HttpGet]
    [Authorize(Policy = AppRoles.ManagerPolicy)]
    public async Task<ActionResult<IEnumerable<AzureApplicationDto>>> GetAll(CancellationToken ct)
    {
        var apps = await db.Applications.OrderBy(a => a.Name).ToListAsync(ct);
        return Ok(apps.Select(AzureApplicationDto.FromDomain));
    }
}
