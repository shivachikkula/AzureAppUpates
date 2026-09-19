using System.Security.Claims;
using AzureEnvManager.Api.Models.Domain;
using AzureEnvManager.Api.Models.Dtos;

namespace AzureEnvManager.Api.Services;

public interface IChangeRequestService
{
    /// <summary>Applies the change immediately for non-production apps, or files a pending approval request for production apps.</summary>
    Task<SaveConnectionStringResultDto> SubmitAsync(AzureApplication app, ConnectionStringUpdateRequestDto request, ClaimsPrincipal requester, CancellationToken ct = default);

    Task<List<ChangeRequestDto>> GetPendingAsync(CancellationToken ct = default);

    Task<List<ChangeRequestDto>> GetMineAsync(string userObjectId, CancellationToken ct = default);

    Task<ChangeRequestDto> ApproveAsync(Guid changeRequestId, ClaimsPrincipal approver, string? note, CancellationToken ct = default);

    Task<ChangeRequestDto> RejectAsync(Guid changeRequestId, ClaimsPrincipal approver, string? note, CancellationToken ct = default);
}
