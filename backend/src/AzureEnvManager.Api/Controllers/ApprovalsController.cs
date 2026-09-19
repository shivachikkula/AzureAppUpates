using AzureEnvManager.Api.Authorization;
using AzureEnvManager.Api.Extensions;
using AzureEnvManager.Api.Models.Dtos;
using AzureEnvManager.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureEnvManager.Api.Controllers;

[ApiController]
[Route("api/approvals")]
[Authorize]
public class ApprovalsController(IChangeRequestService changeRequestService) : ControllerBase
{
    /// <summary>Requests awaiting a decision, scoped to the teams the caller manages (Admins see every team).</summary>
    [HttpGet("pending")]
    [Authorize(Policy = AppRoles.ManagerPolicy)]
    public async Task<ActionResult<IEnumerable<ChangeRequestDto>>> GetPending(CancellationToken ct)
    {
        return Ok(await changeRequestService.GetPendingAsync(User, ct));
    }

    /// <summary>The signed-in developer's own submitted requests and their outcomes.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<ChangeRequestDto>>> GetMine(CancellationToken ct)
    {
        return Ok(await changeRequestService.GetMineAsync(User.GetObjectId(), ct));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = AppRoles.ManagerPolicy)]
    public async Task<ActionResult<ChangeRequestDto>> Approve(Guid id, [FromBody] DecisionDto decision, CancellationToken ct)
    {
        try
        {
            return Ok(await changeRequestService.ApproveAsync(id, User, decision.Note, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = AppRoles.ManagerPolicy)]
    public async Task<ActionResult<ChangeRequestDto>> Reject(Guid id, [FromBody] DecisionDto decision, CancellationToken ct)
    {
        try
        {
            return Ok(await changeRequestService.RejectAsync(id, User, decision.Note, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}
