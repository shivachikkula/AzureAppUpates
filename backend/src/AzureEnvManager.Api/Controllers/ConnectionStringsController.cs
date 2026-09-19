using AzureEnvManager.Api.Data;
using AzureEnvManager.Api.Extensions;
using AzureEnvManager.Api.Models.Dtos;
using AzureEnvManager.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AzureEnvManager.Api.Controllers;

[ApiController]
[Route("api/connection-strings")]
[Authorize]
public class ConnectionStringsController(
    AppDbContext db,
    IAppAccessService accessService,
    IChangeRequestService changeRequestService,
    ILogger<ConnectionStringsController> logger) : ControllerBase
{
    /// <summary>
    /// Saves a connection string. Non-production apps are updated in Azure immediately; production apps
    /// instead create a pending <see cref="Models.Domain.ChangeRequest"/> that a manager must approve.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SaveConnectionStringResultDto>> Save(
        [FromBody] ConnectionStringUpdateRequestDto request,
        CancellationToken ct)
    {
        var userObjectId = User.GetObjectId();

        if (!await accessService.HasAccessAsync(userObjectId, request.ApplicationId, ct))
        {
            return Forbid();
        }

        var app = await db.Applications.FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);
        if (app is null)
        {
            return NotFound(new { message = "Application not found." });
        }

        try
        {
            var result = await changeRequestService.SubmitAsync(app, request, User, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save connection string {Key} for {AppId}", request.Key, request.ApplicationId);
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = "Azure rejected the connection string update. Please try again or contact an admin." });
        }
    }
}
