using System.Security.Claims;
using AzureEnvManager.Api.Models.Domain;
using AzureEnvManager.Api.Models.Dtos;

namespace AzureEnvManager.Api.Services;

public interface IChangeRequestService
{
    /// <summary>Applies the change immediately for non-production apps, or files a pending approval request for production apps.</summary>
    Task<SaveConnectionStringResultDto> SubmitAsync(AzureApplication app, ConnectionStringUpdateRequestDto request, ClaimsPrincipal requester, CancellationToken ct = default);

    /// <summary>Requests awaiting a decision, scoped to the teams the caller manages (or all, for an Admin).</summary>
    Task<List<ChangeRequestDto>> GetPendingAsync(ClaimsPrincipal manager, CancellationToken ct = default);

    Task<List<ChangeRequestDto>> GetMineAsync(string userObjectId, CancellationToken ct = default);

    Task<ChangeRequestDto> ApproveAsync(Guid changeRequestId, ClaimsPrincipal approver, string? note, CancellationToken ct = default);

    Task<ChangeRequestDto> RejectAsync(Guid changeRequestId, ClaimsPrincipal approver, string? note, CancellationToken ct = default);
}
