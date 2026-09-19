using AzureEnvManager.Api.Authorization;
using AzureEnvManager.Api.Data;
using AzureEnvManager.Api.Models.Domain;
using AzureEnvManager.Api.Models.Dtos;
using AzureEnvManager.Api.Services;
using AzureEnvManager.Api.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AzureEnvManager.Api.Tests.Services;

public class ChangeRequestServiceTests
{
    private static (Team team, AzureApplication nonProdApp, AzureApplication prodApp) SeedApps(AppDbContext db)
    {
        var team = new Team { Id = Guid.NewGuid(), Name = "Orders Platform" };
        var nonProdApp = new AzureApplication
        {
            Id = Guid.NewGuid(),
            Name = "orders-api-dev",
            ResourceGroup = "rg-dev",
            SubscriptionId = "sub-1",
            Environment = AppEnvironment.Development,
            TeamId = team.Id,
        };
        var prodApp = new AzureApplication
        {
            Id = Guid.NewGuid(),
            Name = "orders-api-prod",
            ResourceGroup = "rg-prod",
            SubscriptionId = "sub-1",
            Environment = AppEnvironment.Production,
            TeamId = team.Id,
        };

        db.Teams.Add(team);
        db.Applications.AddRange(nonProdApp, prodApp);
        db.SaveChanges();

        return (team, nonProdApp, prodApp);
    }

    private static ChangeRequestService CreateSut(AppDbContext db, Mock<IAzureAppServiceClient> azureClient) =>
        new(db, azureClient.Object, new TeamAccessService(db), NullLogger<ChangeRequestService>.Instance);

    [Fact]
    public async Task SubmitAsync_NonProduction_AppliesImmediately_AndDoesNotCreateChangeRequest()
    {
        using var db = TestDbContextFactory.Create();
        var (_, nonProdApp, _) = SeedApps(db);
        var azureClient = new Mock<IAzureAppServiceClient>();
        var sut = CreateSut(db, azureClient);

        var developer = TestPrincipal.Create("dev-1", "dev@contoso.com", "Dev One", AppRoles.Developer);
        var request = new ConnectionStringUpdateRequestDto
        {
            ApplicationId = nonProdApp.Id,
            Key = "DefaultConnection",
            Value = "Server=...",
            Type = ConnectionStringKind.Custom,
        };

        var result = await sut.SubmitAsync(nonProdApp, request, developer);

        Assert.True(result.Applied);
        Assert.False(result.RequiresApproval);
        Assert.Null(result.ChangeRequestId);
        azureClient.Verify(
            c => c.SetConnectionStringAsync(nonProdApp, "DefaultConnection", "Server=...", ConnectionStringKind.Custom, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Empty(db.ChangeRequests);
    }

    [Fact]
    public async Task SubmitAsync_Production_CreatesPendingRequest_AndDoesNotCallAzure()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, prodApp) = SeedApps(db);
        var azureClient = new Mock<IAzureAppServiceClient>();
        var sut = CreateSut(db, azureClient);

        var developer = TestPrincipal.Create("dev-1", "dev@contoso.com", "Dev One", AppRoles.Developer);
        var request = new ConnectionStringUpdateRequestDto
        {
            ApplicationId = prodApp.Id,
            Key = "DefaultConnection",
            Value = "Server=prod",
            Type = ConnectionStringKind.SQLAzure,
            Reason = "Rotate credentials",
        };

        var result = await sut.SubmitAsync(prodApp, request, developer);

        Assert.False(result.Applied);
        Assert.True(result.RequiresApproval);
        Assert.NotNull(result.ChangeRequestId);
        azureClient.Verify(c => c.SetConnectionStringAsync(
            It.IsAny<AzureApplication>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ConnectionStringKind>(), It.IsAny<CancellationToken>()),
            Times.Never);

        var stored = await db.ChangeRequests.SingleAsync();
        Assert.Equal(ChangeRequestStatus.PendingApproval, stored.Status);
        Assert.Equal("dev-1", stored.RequestedByObjectId);
        Assert.Equal("Rotate credentials", stored.Reason);
    }

    [Fact]
    public async Task ApproveAsync_AsAdmin_AppliesChangeAndMarksApplied()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, prodApp) = SeedApps(db);
        var azureClient = new Mock<IAzureAppServiceClient>();
        var sut = CreateSut(db, azureClient);

        var changeRequest = await CreatePendingChangeRequestAsync(db, prodApp);
        var admin = TestPrincipal.Create("admin-1", "admin@contoso.com", "Admin One", AppRoles.Admin);

        var result = await sut.ApproveAsync(changeRequest.Id, admin, "looks good");

        Assert.Equal(ChangeRequestStatus.Applied, result.Status);
        azureClient.Verify(
            c => c.SetConnectionStringAsync(prodApp, changeRequest.Key, changeRequest.Value, changeRequest.Type, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_AsManagerOfOwningTeam_Allowed()
    {
        using var db = TestDbContextFactory.Create();
        var (team, _, prodApp) = SeedApps(db);
        db.ManagerAssignments.Add(new ManagerTeamAssignment
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            UserObjectId = "mgr-1",
            UserEmail = "mgr@contoso.com",
            UserDisplayName = "Mgr One",
        });
        await db.SaveChangesAsync();

        var azureClient = new Mock<IAzureAppServiceClient>();
        var sut = CreateSut(db, azureClient);
        var changeRequest = await CreatePendingChangeRequestAsync(db, prodApp);
        var manager = TestPrincipal.Create("mgr-1", "mgr@contoso.com", "Mgr One", AppRoles.Manager);

        var result = await sut.ApproveAsync(changeRequest.Id, manager, null);

        Assert.Equal(ChangeRequestStatus.Applied, result.Status);
    }

    [Fact]
    public async Task ApproveAsync_AsManagerOfDifferentTeam_ThrowsUnauthorizedAccessException()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, prodApp) = SeedApps(db);

        var otherTeam = new Team { Id = Guid.NewGuid(), Name = "Payments" };
        db.Teams.Add(otherTeam);
        db.ManagerAssignments.Add(new ManagerTeamAssignment
        {
            Id = Guid.NewGuid(),
            TeamId = otherTeam.Id,
            UserObjectId = "mgr-2",
            UserEmail = "mgr2@contoso.com",
            UserDisplayName = "Mgr Two",
        });
        await db.SaveChangesAsync();

        var azureClient = new Mock<IAzureAppServiceClient>();
        var sut = CreateSut(db, azureClient);
        var changeRequest = await CreatePendingChangeRequestAsync(db, prodApp);
        var manager = TestPrincipal.Create("mgr-2", "mgr2@contoso.com", "Mgr Two", AppRoles.Manager);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.ApproveAsync(changeRequest.Id, manager, null));

        var stillPending = await db.ChangeRequests.SingleAsync(c => c.Id == changeRequest.Id);
        Assert.Equal(ChangeRequestStatus.PendingApproval, stillPending.Status);
        azureClient.Verify(
            c => c.SetConnectionStringAsync(It.IsAny<AzureApplication>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ConnectionStringKind>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApproveAsync_WhenAzureThrows_MarksFailedRatherThanApplied()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, prodApp) = SeedApps(db);
        var azureClient = new Mock<IAzureAppServiceClient>();
        azureClient
            .Setup(c => c.SetConnectionStringAsync(It.IsAny<AzureApplication>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ConnectionStringKind>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Azure said no"));

        var sut = CreateSut(db, azureClient);
        var changeRequest = await CreatePendingChangeRequestAsync(db, prodApp);
        var admin = TestPrincipal.Create("admin-1", "admin@contoso.com", "Admin One", AppRoles.Admin);

        var result = await sut.ApproveAsync(changeRequest.Id, admin, null);

        Assert.Equal(ChangeRequestStatus.Failed, result.Status);
    }

    [Fact]
    public async Task RejectAsync_MarksRejected_AndNeverCallsAzure()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, prodApp) = SeedApps(db);
        var azureClient = new Mock<IAzureAppServiceClient>();
        var sut = CreateSut(db, azureClient);
        var changeRequest = await CreatePendingChangeRequestAsync(db, prodApp);
        var admin = TestPrincipal.Create("admin-1", "admin@contoso.com", "Admin One", AppRoles.Admin);

        var result = await sut.RejectAsync(changeRequest.Id, admin, "not now");

        Assert.Equal(ChangeRequestStatus.Rejected, result.Status);
        azureClient.Verify(
            c => c.SetConnectionStringAsync(It.IsAny<AzureApplication>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ConnectionStringKind>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApproveAsync_AlreadyDecided_ThrowsInvalidOperationException()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, prodApp) = SeedApps(db);
        var azureClient = new Mock<IAzureAppServiceClient>();
        var sut = CreateSut(db, azureClient);
        var changeRequest = await CreatePendingChangeRequestAsync(db, prodApp);
        var admin = TestPrincipal.Create("admin-1", "admin@contoso.com", "Admin One", AppRoles.Admin);

        await sut.RejectAsync(changeRequest.Id, admin, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ApproveAsync(changeRequest.Id, admin, null));
    }

    [Fact]
    public async Task ApproveAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        using var db = TestDbContextFactory.Create();
        SeedApps(db);
        var azureClient = new Mock<IAzureAppServiceClient>();
        var sut = CreateSut(db, azureClient);
        var admin = TestPrincipal.Create("admin-1", "admin@contoso.com", "Admin One", AppRoles.Admin);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.ApproveAsync(Guid.NewGuid(), admin, null));
    }

    [Fact]
    public async Task GetPendingAsync_Admin_ReturnsRequestsAcrossAllTeams()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, prodApp) = SeedApps(db);
        await CreatePendingChangeRequestAsync(db, prodApp);

        var otherTeam = new Team { Id = Guid.NewGuid(), Name = "Payments" };
        var otherProdApp = new AzureApplication
        {
            Id = Guid.NewGuid(),
            Name = "payments-api-prod",
            ResourceGroup = "rg-payments-prod",
            SubscriptionId = "sub-1",
            Environment = AppEnvironment.Production,
            TeamId = otherTeam.Id,
        };
        db.Teams.Add(otherTeam);
        db.Applications.Add(otherProdApp);
        await db.SaveChangesAsync();
        await CreatePendingChangeRequestAsync(db, otherProdApp);

        var azureClient = new Mock<IAzureAppServiceClient>();
        var sut = CreateSut(db, azureClient);
        var admin = TestPrincipal.Create("admin-1", "admin@contoso.com", "Admin One", AppRoles.Admin);

        var pending = await sut.GetPendingAsync(admin);

        Assert.Equal(2, pending.Count);
    }

    [Fact]
    public async Task GetPendingAsync_TeamManager_OnlySeesOwnTeamsRequests()
    {
        using var db = TestDbContextFactory.Create();
        var (team, _, prodApp) = SeedApps(db);
        await CreatePendingChangeRequestAsync(db, prodApp);

        var otherTeam = new Team { Id = Guid.NewGuid(), Name = "Payments" };
        var otherProdApp = new AzureApplication
        {
            Id = Guid.NewGuid(),
            Name = "payments-api-prod",
            ResourceGroup = "rg-payments-prod",
            SubscriptionId = "sub-1",
            Environment = AppEnvironment.Production,
            TeamId = otherTeam.Id,
        };
        db.Teams.Add(otherTeam);
        db.Applications.Add(otherProdApp);
        db.ManagerAssignments.Add(new ManagerTeamAssignment
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            UserObjectId = "mgr-1",
            UserEmail = "mgr@contoso.com",
            UserDisplayName = "Mgr One",
        });
        await db.SaveChangesAsync();
        await CreatePendingChangeRequestAsync(db, otherProdApp);

        var azureClient = new Mock<IAzureAppServiceClient>();
        var sut = CreateSut(db, azureClient);
        var manager = TestPrincipal.Create("mgr-1", "mgr@contoso.com", "Mgr One", AppRoles.Manager);

        var pending = await sut.GetPendingAsync(manager);

        Assert.Single(pending);
        Assert.Equal(prodApp.Id, pending[0].ApplicationId);
    }

    private static async Task<ChangeRequest> CreatePendingChangeRequestAsync(AppDbContext db, AzureApplication app)
    {
        var changeRequest = new ChangeRequest
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            Key = "DefaultConnection",
            Value = "Server=prod",
            Type = ConnectionStringKind.SQLAzure,
            RequestedByObjectId = "dev-1",
            RequestedByEmail = "dev@contoso.com",
            RequestedByName = "Dev One",
            CreatedUtc = DateTimeOffset.UtcNow,
            Status = ChangeRequestStatus.PendingApproval,
        };

        db.ChangeRequests.Add(changeRequest);
        await db.SaveChangesAsync();
        return changeRequest;
    }
}
