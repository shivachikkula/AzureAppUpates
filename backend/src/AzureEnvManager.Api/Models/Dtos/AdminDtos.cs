using System.ComponentModel.DataAnnotations;
using AzureEnvManager.Api.Models.Domain;

namespace AzureEnvManager.Api.Models.Dtos;

public record TeamDto(Guid Id, string Name, int ApplicationCount, int ManagerCount);

public class CreateTeamDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}

public record ApplicationAdminDto(
    Guid Id,
    string Name,
    string ResourceGroup,
    string SubscriptionId,
    AppEnvironment Environment,
    string DefaultHostName,
    Guid TeamId,
    string TeamName)
{
    public static ApplicationAdminDto FromDomain(AzureApplication app) => new(
        app.Id,
        app.Name,
        app.ResourceGroup,
        app.SubscriptionId,
        app.Environment,
        app.DefaultHostName,
        app.TeamId,
        app.Team?.Name ?? string.Empty);
}

public class ApplicationUpsertDto
{
    [Required]
    [StringLength(60)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(90)]
    public string ResourceGroup { get; set; } = string.Empty;

    [Required]
    public string SubscriptionId { get; set; } = string.Empty;

    [Required]
    public AppEnvironment Environment { get; set; }

    [StringLength(260)]
    public string DefaultHostName { get; set; } = string.Empty;

    [Required]
    public Guid TeamId { get; set; }
}

public record AppAssignmentDto(
    Guid Id,
    Guid ApplicationId,
    string ApplicationName,
    string UserObjectId,
    string UserEmail,
    string UserDisplayName)
{
    public static AppAssignmentDto FromDomain(AppAssignment a) => new(
        a.Id,
        a.ApplicationId,
        a.Application?.Name ?? string.Empty,
        a.UserObjectId,
        a.UserEmail,
        a.UserDisplayName);
}

public class CreateAppAssignmentDto
{
    [Required]
    public Guid ApplicationId { get; set; }

    [Required]
    public string UserObjectId { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string UserEmail { get; set; } = string.Empty;

    [Required]
    public string UserDisplayName { get; set; } = string.Empty;
}

public record ManagerAssignmentDto(
    Guid Id,
    Guid TeamId,
    string TeamName,
    string UserObjectId,
    string UserEmail,
    string UserDisplayName)
{
    public static ManagerAssignmentDto FromDomain(ManagerTeamAssignment a) => new(
        a.Id,
        a.TeamId,
        a.Team?.Name ?? string.Empty,
        a.UserObjectId,
        a.UserEmail,
        a.UserDisplayName);
}

public class CreateManagerAssignmentDto
{
    [Required]
    public Guid TeamId { get; set; }

    [Required]
    public string UserObjectId { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string UserEmail { get; set; } = string.Empty;

    [Required]
    public string UserDisplayName { get; set; } = string.Empty;
}
