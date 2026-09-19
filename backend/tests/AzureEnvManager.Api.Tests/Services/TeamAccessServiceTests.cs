using AzureEnvManager.Api.Models.Domain;
using AzureEnvManager.Api.Services;
using AzureEnvManager.Api.Tests.TestHelpers;
using Xunit;

namespace AzureEnvManager.Api.Tests.Services;

public class TeamAccessServiceTests
{
    [Fact]
    public async Task GetManagedTeamIdsAsync_ReturnsOnlyTeamsTheManagerIsAssignedTo()
    {
        using var db = TestDbContextFactory.Create();
        var teamA = new Team { Id = Guid.NewGuid(), Name = "Team A" };
        var teamB = new Team { Id = Guid.NewGuid(), Name = "Team B" };
        db.Teams.AddRange(teamA, teamB);
        db.ManagerAssignments.Add(new ManagerTeamAssignment
        {
            Id = Guid.NewGuid(),
            TeamId = teamA.Id,
            UserObjectId = "mgr-1",
            UserEmail = "mgr@contoso.com",
            UserDisplayName = "Mgr One",
        });
        await db.SaveChangesAsync();

        var sut = new TeamAccessService(db);
        var managedTeamIds = await sut.GetManagedTeamIdsAsync("mgr-1");

        Assert.Single(managedTeamIds);
        Assert.Equal(teamA.Id, managedTeamIds[0]);
    }

    [Fact]
    public async Task IsManagerOfTeamAsync_FalseWhenNotAssigned()
    {
        using var db = TestDbContextFactory.Create();
        var team = new Team { Id = Guid.NewGuid(), Name = "Team A" };
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        var sut = new TeamAccessService(db);

        Assert.False(await sut.IsManagerOfTeamAsync("mgr-1", team.Id));
    }
}
