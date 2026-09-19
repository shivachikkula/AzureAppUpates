using AzureEnvManager.Api.Models.Dtos;

namespace AzureEnvManager.Api.Services;

/// <summary>Backs the Admin UI: managing teams, the applications each team owns, and who (developers,
/// managers) has access to what. All of it is Admin-only — see <see cref="Authorization.AppRoles.AdminPolicy"/>.</summary>
public interface IAdminService
{
    Task<List<TeamDto>> GetTeamsAsync(CancellationToken ct = default);

    Task<TeamDto> CreateTeamAsync(CreateTeamDto dto, CancellationToken ct = default);

    Task<List<ApplicationAdminDto>> GetApplicationsAsync(CancellationToken ct = default);

    Task<ApplicationAdminDto> CreateApplicationAsync(ApplicationUpsertDto dto, CancellationToken ct = default);

    Task<ApplicationAdminDto> UpdateApplicationAsync(Guid applicationId, ApplicationUpsertDto dto, CancellationToken ct = default);

    Task DeleteApplicationAsync(Guid applicationId, CancellationToken ct = default);

    Task<List<AppAssignmentDto>> GetAppAssignmentsAsync(Guid? applicationId, CancellationToken ct = default);

    Task<AppAssignmentDto> CreateAppAssignmentAsync(CreateAppAssignmentDto dto, CancellationToken ct = default);

    Task DeleteAppAssignmentAsync(Guid assignmentId, CancellationToken ct = default);

    Task<List<ManagerAssignmentDto>> GetManagerAssignmentsAsync(Guid? teamId, CancellationToken ct = default);

    Task<ManagerAssignmentDto> CreateManagerAssignmentAsync(CreateManagerAssignmentDto dto, CancellationToken ct = default);

    Task DeleteManagerAssignmentAsync(Guid assignmentId, CancellationToken ct = default);
}
