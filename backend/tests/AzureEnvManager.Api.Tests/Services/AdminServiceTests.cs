using AzureEnvManager.Api.Models.Domain;
using AzureEnvManager.Api.Models.Dtos;
using AzureEnvManager.Api.Services;
using AzureEnvManager.Api.Tests.TestHelpers;
using Xunit;

namespace AzureEnvManager.Api.Tests.Services;

public class AdminServiceTests
{
    [Fact]
    public async Task CreateTeamAsync_DuplicateName_Throws()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new AdminService(db);
        await sut.CreateTeamAsync(new CreateTeamDto { Name = "Orders Platform" });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateTeamAsync(new CreateTeamDto { Name = "Orders Platform" }));
    }

    [Fact]
    public async Task CreateApplicationAsync_UnknownTeam_ThrowsKeyNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new AdminService(db);

        var dto = new ApplicationUpsertDto
        {
            Name = "orders-api",
            ResourceGroup = "rg",
            SubscriptionId = "sub",
            Environment = AppEnvironment.Development,
            TeamId = Guid.NewGuid(),
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.CreateApplicationAsync(dto));
    }

    [Fact]
    public async Task CreateApplicationAsync_ThenAssignDeveloper_RoundTrips()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new AdminService(db);
        var team = await sut.CreateTeamAsync(new CreateTeamDto { Name = "Orders Platform" });

        var app = await sut.CreateApplicationAsync(new ApplicationUpsertDto
        {
            Name = "orders-api",
            ResourceGroup = "rg-dev",
            SubscriptionId = "sub-1",
            Environment = AppEnvironment.Development,
            DefaultHostName = "orders-api.azurewebsites.net",
            TeamId = team.Id,
        });

        Assert.Equal(team.Id, app.TeamId);
        Assert.Equal("Orders Platform", app.TeamName);

        var assignment = await sut.CreateAppAssignmentAsync(new CreateAppAssignmentDto
        {
            ApplicationId = app.Id,
            UserObjectId = "dev-1",
            UserEmail = "dev@contoso.com",
            UserDisplayName = "Dev One",
        });

        Assert.Equal(app.Id, assignment.ApplicationId);

        var assignments = await sut.GetAppAssignmentsAsync(app.Id);
        Assert.Single(assignments);
    }

    [Fact]
    public async Task CreateAppAssignmentAsync_Duplicate_Throws()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new AdminService(db);
        var team = await sut.CreateTeamAsync(new CreateTeamDto { Name = "Orders Platform" });
        var app = await sut.CreateApplicationAsync(new ApplicationUpsertDto
        {
            Name = "orders-api",
            ResourceGroup = "rg-dev",
            SubscriptionId = "sub-1",
            Environment = AppEnvironment.Development,
            TeamId = team.Id,
        });

        var dto = new CreateAppAssignmentDto
        {
            ApplicationId = app.Id,
            UserObjectId = "dev-1",
            UserEmail = "dev@contoso.com",
            UserDisplayName = "Dev One",
        };
        await sut.CreateAppAssignmentAsync(dto);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAppAssignmentAsync(dto));
    }

    [Fact]
    public async Task CreateManagerAssignmentAsync_ThenDelete_RemovesAccess()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new AdminService(db);
        var team = await sut.CreateTeamAsync(new CreateTeamDto { Name = "Orders Platform" });

        var assignment = await sut.CreateManagerAssignmentAsync(new CreateManagerAssignmentDto
        {
            TeamId = team.Id,
            UserObjectId = "mgr-1",
            UserEmail = "mgr@contoso.com",
            UserDisplayName = "Mgr One",
        });

        Assert.Single(await sut.GetManagerAssignmentsAsync(team.Id));

        await sut.DeleteManagerAssignmentAsync(assignment.Id);

        Assert.Empty(await sut.GetManagerAssignmentsAsync(team.Id));
    }

    [Fact]
    public async Task DeleteApplicationAsync_UnknownId_ThrowsKeyNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new AdminService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.DeleteApplicationAsync(Guid.NewGuid()));
    }
}
