using AzureEnvManager.Api.Models.Domain;
using AzureEnvManager.Api.Services;
using AzureEnvManager.Api.Tests.TestHelpers;
using Xunit;

namespace AzureEnvManager.Api.Tests.Services;

public class AppAccessServiceTests
{
    [Fact]
    public async Task GetAssignedApplicationsAsync_OnlyReturnsAppsAssignedToThatUser()
    {
        using var db = TestDbContextFactory.Create();
        var team = new Team { Id = Guid.NewGuid(), Name = "Orders Platform" };
        var assignedApp = new AzureApplication
        {
            Id = Guid.NewGuid(),
            Name = "assigned-app",
            ResourceGroup = "rg",
            SubscriptionId = "sub",
            Environment = AppEnvironment.Development,
            TeamId = team.Id,
        };
        var unassignedApp = new AzureApplication
        {
            Id = Guid.NewGuid(),
            Name = "unassigned-app",
            ResourceGroup = "rg",
            SubscriptionId = "sub",
            Environment = AppEnvironment.Development,
            TeamId = team.Id,
        };

        db.Teams.Add(team);
        db.Applications.AddRange(assignedApp, unassignedApp);
        db.Assignments.Add(new AppAssignment
        {
            Id = Guid.NewGuid(),
            ApplicationId = assignedApp.Id,
            UserObjectId = "dev-1",
            UserEmail = "dev@contoso.com",
            UserDisplayName = "Dev One",
        });
        await db.SaveChangesAsync();

        var sut = new AppAccessService(db);
        var result = await sut.GetAssignedApplicationsAsync("dev-1");

        Assert.Single(result);
        Assert.Equal(assignedApp.Id, result[0].Id);
    }

    [Fact]
    public async Task HasAccessAsync_ReturnsFalseForUnassignedUser()
    {
        using var db = TestDbContextFactory.Create();
        var team = new Team { Id = Guid.NewGuid(), Name = "Orders Platform" };
        var app = new AzureApplication
        {
            Id = Guid.NewGuid(),
            Name = "app",
            ResourceGroup = "rg",
            SubscriptionId = "sub",
            Environment = AppEnvironment.Development,
            TeamId = team.Id,
        };
        db.Teams.Add(team);
        db.Applications.Add(app);
        db.Assignments.Add(new AppAssignment
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            UserObjectId = "dev-1",
            UserEmail = "dev@contoso.com",
            UserDisplayName = "Dev One",
        });
        await db.SaveChangesAsync();

        var sut = new AppAccessService(db);

        Assert.True(await sut.HasAccessAsync("dev-1", app.Id));
        Assert.False(await sut.HasAccessAsync("dev-2", app.Id));
    }
}
