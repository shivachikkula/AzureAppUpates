using AzureEnvManager.Api.Data;
using AzureEnvManager.Api.Models.Domain;
using AzureEnvManager.Api.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace AzureEnvManager.Api.Services;

public class AdminService(AppDbContext db) : IAdminService
{
    public async Task<List<TeamDto>> GetTeamsAsync(CancellationToken ct = default)
    {
        var teams = await db.Teams
            .Include(t => t.Applications)
            .Include(t => t.Managers)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        return teams
            .Select(t => new TeamDto(t.Id, t.Name, t.Applications.Count, t.Managers.Count))
            .ToList();
    }

    public async Task<TeamDto> CreateTeamAsync(CreateTeamDto dto, CancellationToken ct = default)
    {
        if (await db.Teams.AnyAsync(t => t.Name == dto.Name, ct))
        {
            throw new InvalidOperationException($"A team named '{dto.Name}' already exists.");
        }

        var team = new Team { Id = Guid.NewGuid(), Name = dto.Name };
        db.Teams.Add(team);
        await db.SaveChangesAsync(ct);

        return new TeamDto(team.Id, team.Name, 0, 0);
    }

    public async Task<List<ApplicationAdminDto>> GetApplicationsAsync(CancellationToken ct = default)
    {
        var apps = await db.Applications
            .Include(a => a.Team)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

        return apps.Select(ApplicationAdminDto.FromDomain).ToList();
    }

    public async Task<ApplicationAdminDto> CreateApplicationAsync(ApplicationUpsertDto dto, CancellationToken ct = default)
    {
        await EnsureTeamExistsAsync(dto.TeamId, ct);

        if (await db.Applications.AnyAsync(
                a => a.SubscriptionId == dto.SubscriptionId && a.ResourceGroup == dto.ResourceGroup && a.Name == dto.Name,
                ct))
        {
            throw new InvalidOperationException($"'{dto.Name}' is already tracked in that resource group.");
        }

        var app = new AzureApplication
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            ResourceGroup = dto.ResourceGroup,
            SubscriptionId = dto.SubscriptionId,
            Environment = dto.Environment,
            DefaultHostName = dto.DefaultHostName,
            TeamId = dto.TeamId,
        };

        db.Applications.Add(app);
        await db.SaveChangesAsync(ct);

        return ApplicationAdminDto.FromDomain(await LoadApplicationAsync(app.Id, ct));
    }

    public async Task<ApplicationAdminDto> UpdateApplicationAsync(Guid applicationId, ApplicationUpsertDto dto, CancellationToken ct = default)
    {
        await EnsureTeamExistsAsync(dto.TeamId, ct);

        var app = await db.Applications.FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new KeyNotFoundException($"Application {applicationId} was not found.");

        app.Name = dto.Name;
        app.ResourceGroup = dto.ResourceGroup;
        app.SubscriptionId = dto.SubscriptionId;
        app.Environment = dto.Environment;
        app.DefaultHostName = dto.DefaultHostName;
        app.TeamId = dto.TeamId;

        await db.SaveChangesAsync(ct);

        return ApplicationAdminDto.FromDomain(await LoadApplicationAsync(app.Id, ct));
    }

    public async Task DeleteApplicationAsync(Guid applicationId, CancellationToken ct = default)
    {
        var app = await db.Applications.FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new KeyNotFoundException($"Application {applicationId} was not found.");

        db.Applications.Remove(app);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<AppAssignmentDto>> GetAppAssignmentsAsync(Guid? applicationId, CancellationToken ct = default)
    {
        var query = db.Assignments.Include(a => a.Application).AsQueryable();
        if (applicationId is not null)
        {
            query = query.Where(a => a.ApplicationId == applicationId);
        }

        var assignments = await query.OrderBy(a => a.UserDisplayName).ToListAsync(ct);
        return assignments.Select(AppAssignmentDto.FromDomain).ToList();
    }

    public async Task<AppAssignmentDto> CreateAppAssignmentAsync(CreateAppAssignmentDto dto, CancellationToken ct = default)
    {
        var app = await db.Applications.FirstOrDefaultAsync(a => a.Id == dto.ApplicationId, ct)
            ?? throw new KeyNotFoundException($"Application {dto.ApplicationId} was not found.");

        if (await db.Assignments.AnyAsync(a => a.ApplicationId == dto.ApplicationId && a.UserObjectId == dto.UserObjectId, ct))
        {
            throw new InvalidOperationException($"{dto.UserDisplayName} already has access to {app.Name}.");
        }

        var assignment = new AppAssignment
        {
            Id = Guid.NewGuid(),
            ApplicationId = dto.ApplicationId,
            UserObjectId = dto.UserObjectId,
            UserEmail = dto.UserEmail,
            UserDisplayName = dto.UserDisplayName,
        };

        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(ct);

        return new AppAssignmentDto(assignment.Id, app.Id, app.Name, dto.UserObjectId, dto.UserEmail, dto.UserDisplayName);
    }

    public async Task DeleteAppAssignmentAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var assignment = await db.Assignments.FirstOrDefaultAsync(a => a.Id == assignmentId, ct)
            ?? throw new KeyNotFoundException($"Assignment {assignmentId} was not found.");

        db.Assignments.Remove(assignment);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<ManagerAssignmentDto>> GetManagerAssignmentsAsync(Guid? teamId, CancellationToken ct = default)
    {
        var query = db.ManagerAssignments.Include(a => a.Team).AsQueryable();
        if (teamId is not null)
        {
            query = query.Where(a => a.TeamId == teamId);
        }

        var assignments = await query.OrderBy(a => a.UserDisplayName).ToListAsync(ct);
        return assignments.Select(ManagerAssignmentDto.FromDomain).ToList();
    }

    public async Task<ManagerAssignmentDto> CreateManagerAssignmentAsync(CreateManagerAssignmentDto dto, CancellationToken ct = default)
    {
        var team = await db.Teams.FirstOrDefaultAsync(t => t.Id == dto.TeamId, ct)
            ?? throw new KeyNotFoundException($"Team {dto.TeamId} was not found.");

        if (await db.ManagerAssignments.AnyAsync(a => a.TeamId == dto.TeamId && a.UserObjectId == dto.UserObjectId, ct))
        {
            throw new InvalidOperationException($"{dto.UserDisplayName} already manages {team.Name}.");
        }

        var assignment = new ManagerTeamAssignment
        {
            Id = Guid.NewGuid(),
            TeamId = dto.TeamId,
            UserObjectId = dto.UserObjectId,
            UserEmail = dto.UserEmail,
            UserDisplayName = dto.UserDisplayName,
        };

        db.ManagerAssignments.Add(assignment);
        await db.SaveChangesAsync(ct);

        return new ManagerAssignmentDto(assignment.Id, team.Id, team.Name, dto.UserObjectId, dto.UserEmail, dto.UserDisplayName);
    }

    public async Task DeleteManagerAssignmentAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var assignment = await db.ManagerAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId, ct)
            ?? throw new KeyNotFoundException($"Manager assignment {assignmentId} was not found.");

        db.ManagerAssignments.Remove(assignment);
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureTeamExistsAsync(Guid teamId, CancellationToken ct)
    {
        if (!await db.Teams.AnyAsync(t => t.Id == teamId, ct))
        {
            throw new KeyNotFoundException($"Team {teamId} was not found.");
        }
    }

    private Task<AzureApplication> LoadApplicationAsync(Guid applicationId, CancellationToken ct) =>
        db.Applications.Include(a => a.Team).FirstAsync(a => a.Id == applicationId, ct);
}
