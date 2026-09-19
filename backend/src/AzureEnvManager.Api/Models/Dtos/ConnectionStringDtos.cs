using System.ComponentModel.DataAnnotations;
using AzureEnvManager.Api.Models.Domain;

namespace AzureEnvManager.Api.Models.Dtos;

public class ConnectionStringUpdateRequestDto
{
    [Required]
    public Guid ApplicationId { get; set; }

    [Required]
    [RegularExpression(@"^[A-Za-z0-9_.:-]+$")]
    [StringLength(200)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Value { get; set; } = string.Empty;

    public ConnectionStringKind Type { get; set; } = ConnectionStringKind.Custom;

    [StringLength(1000)]
    public string? Reason { get; set; }
}

public record SaveConnectionStringResultDto(bool Applied, bool RequiresApproval, Guid? ChangeRequestId, string Message);
