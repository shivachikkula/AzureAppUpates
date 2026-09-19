using System.Security.Claims;
using AzureEnvManager.Api.Data;
using AzureEnvManager.Api.Extensions;
using AzureEnvManager.Api.Models.Domain;
using AzureEnvManager.Api.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace AzureEnvManager.Api.Services;

public class ChangeRequestService(AppDbContext db, IAzureAppServiceClient azureClient, ILogger<ChangeRequestService> logger)
    : IChangeRequestService
{
    public async Task<SaveConnectionStringResultDto> SubmitAsync(
        AzureApplication app,
        ConnectionStringUpdateRequestDto request,
        ClaimsPrincipal requester,
        CancellationToken ct = default)
    {
        if (app.Environment != AppEnvironment.Production)
        {
            await azureClient.SetConnectionStringAsync(app, request.Key, request.Value, request.Type, ct);
            return new SaveConnectionStringResultDto(
                Applied: true,
                RequiresApproval: false,
                ChangeRequestId: null,
                Message: $"'{request.Key}' was updated on {app.Name} ({app.Environment}).");
        }

        var changeRequest = new ChangeRequest
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            Key = request.Key,
            Value = request.Value,
            Type = request.Type,
            Reason = request.Reason,
            RequestedByObjectId = requester.GetObjectId(),
            RequestedByEmail = requester.GetEmail(),
            RequestedByName = requester.GetDisplayName(),
            CreatedUtc = DateTimeOffset.UtcNow,
            Status = ChangeRequestStatus.PendingApproval,
        };

        db.ChangeRequests.Add(changeRequest);
        await db.SaveChangesAsync(ct);

        return new SaveConnectionStringResultDto(
            Applied: false,
            RequiresApproval: true,
            ChangeRequestId: changeRequest.Id,
            Message: $"'{request.Key}' change for {app.Name} was submitted for manager approval.");
    }

    public async Task<List<ChangeRequestDto>> GetPendingAsync(CancellationToken ct = default)
    {
        var pending = await db.ChangeRequests
            .Include(c => c.Application)
            .Where(c => c.Status == ChangeRequestStatus.PendingApproval)
            .OrderBy(c => c.CreatedUtc)
            .ToListAsync(ct);

        return pending.Select(ChangeRequestDto.FromDomain).ToList();
    }

    public async Task<List<ChangeRequestDto>> GetMineAsync(string userObjectId, CancellationToken ct = default)
    {
        var mine = await db.ChangeRequests
            .Include(c => c.Application)
            .Where(c => c.RequestedByObjectId == userObjectId)
            .OrderByDescending(c => c.CreatedUtc)
            .ToListAsync(ct);

        return mine.Select(ChangeRequestDto.FromDomain).ToList();
    }

    public async Task<ChangeRequestDto> ApproveAsync(Guid changeRequestId, ClaimsPrincipal approver, string? note, CancellationToken ct = default)
    {
        var changeRequest = await LoadPendingAsync(changeRequestId, ct);

        changeRequest.Status = ChangeRequestStatus.Approved;
        changeRequest.DecidedByObjectId = approver.GetObjectId();
        changeRequest.DecidedByName = approver.GetDisplayName();
        changeRequest.DecidedUtc = DateTimeOffset.UtcNow;
        changeRequest.DecisionNote = note;

        try
        {
            await azureClient.SetConnectionStringAsync(
                changeRequest.Application!,
                changeRequest.Key,
                changeRequest.Value,
                changeRequest.Type,
                ct);

            changeRequest.Status = ChangeRequestStatus.Applied;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply approved change request {ChangeRequestId}", changeRequestId);
            changeRequest.Status = ChangeRequestStatus.Failed;
            changeRequest.FailureDetail = ex.Message;
        }

        await db.SaveChangesAsync(ct);
        return ChangeRequestDto.FromDomain(changeRequest);
    }

    public async Task<ChangeRequestDto> RejectAsync(Guid changeRequestId, ClaimsPrincipal approver, string? note, CancellationToken ct = default)
    {
        var changeRequest = await LoadPendingAsync(changeRequestId, ct);

        changeRequest.Status = ChangeRequestStatus.Rejected;
        changeRequest.DecidedByObjectId = approver.GetObjectId();
        changeRequest.DecidedByName = approver.GetDisplayName();
        changeRequest.DecidedUtc = DateTimeOffset.UtcNow;
        changeRequest.DecisionNote = note;

        await db.SaveChangesAsync(ct);
        return ChangeRequestDto.FromDomain(changeRequest);
    }

    private async Task<ChangeRequest> LoadPendingAsync(Guid changeRequestId, CancellationToken ct)
    {
        var changeRequest = await db.ChangeRequests
            .Include(c => c.Application)
            .FirstOrDefaultAsync(c => c.Id == changeRequestId, ct);

        if (changeRequest is null)
        {
            throw new KeyNotFoundException($"Change request {changeRequestId} was not found.");
        }

        if (changeRequest.Status != ChangeRequestStatus.PendingApproval)
        {
            throw new InvalidOperationException($"Change request {changeRequestId} has already been decided.");
        }

        return changeRequest;
    }
}
