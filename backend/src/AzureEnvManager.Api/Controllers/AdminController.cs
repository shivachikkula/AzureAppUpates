using AzureEnvManager.Api.Authorization;
using AzureEnvManager.Api.Models.Dtos;
using AzureEnvManager.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureEnvManager.Api.Controllers;

/// <summary>Manages teams, tracked applications, and who has developer/manager access to what. Admin-only.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = AppRoles.AdminPolicy)]
public class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpGet("teams")]
    public async Task<ActionResult<IEnumerable<TeamDto>>> GetTeams(CancellationToken ct) =>
        Ok(await adminService.GetTeamsAsync(ct));

    [HttpPost("teams")]
    public async Task<ActionResult<TeamDto>> CreateTeam([FromBody] CreateTeamDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await adminService.CreateTeamAsync(dto, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("applications")]
    public async Task<ActionResult<IEnumerable<ApplicationAdminDto>>> GetApplications(CancellationToken ct) =>
        Ok(await adminService.GetApplicationsAsync(ct));

    [HttpPost("applications")]
    public async Task<ActionResult<ApplicationAdminDto>> CreateApplication([FromBody] ApplicationUpsertDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await adminService.CreateApplicationAsync(dto, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("applications/{id:guid}")]
    public async Task<ActionResult<ApplicationAdminDto>> UpdateApplication(Guid id, [FromBody] ApplicationUpsertDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await adminService.UpdateApplicationAsync(id, dto, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("applications/{id:guid}")]
    public async Task<IActionResult> DeleteApplication(Guid id, CancellationToken ct)
    {
        try
        {
            await adminService.DeleteApplicationAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("assignments")]
    public async Task<ActionResult<IEnumerable<AppAssignmentDto>>> GetAppAssignments([FromQuery] Guid? applicationId, CancellationToken ct) =>
        Ok(await adminService.GetAppAssignmentsAsync(applicationId, ct));

    [HttpPost("assignments")]
    public async Task<ActionResult<AppAssignmentDto>> CreateAppAssignment([FromBody] CreateAppAssignmentDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await adminService.CreateAppAssignmentAsync(dto, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("assignments/{id:guid}")]
    public async Task<IActionResult> DeleteAppAssignment(Guid id, CancellationToken ct)
    {
        try
        {
            await adminService.DeleteAppAssignmentAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("manager-assignments")]
    public async Task<ActionResult<IEnumerable<ManagerAssignmentDto>>> GetManagerAssignments([FromQuery] Guid? teamId, CancellationToken ct) =>
        Ok(await adminService.GetManagerAssignmentsAsync(teamId, ct));

    [HttpPost("manager-assignments")]
    public async Task<ActionResult<ManagerAssignmentDto>> CreateManagerAssignment([FromBody] CreateManagerAssignmentDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await adminService.CreateManagerAssignmentAsync(dto, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("manager-assignments/{id:guid}")]
    public async Task<IActionResult> DeleteManagerAssignment(Guid id, CancellationToken ct)
    {
        try
        {
            await adminService.DeleteManagerAssignmentAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
