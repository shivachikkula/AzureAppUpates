using System.Security.Claims;

namespace AzureEnvManager.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Entra ID object id (oid claim), stable per user per tenant.</summary>
    public static string GetObjectId(this ClaimsPrincipal user) =>
        user.FindFirstValue("oid")
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Token is missing an object id claim.");

    public static string GetEmail(this ClaimsPrincipal user) =>
        user.FindFirstValue("preferred_username")
        ?? user.FindFirstValue(ClaimTypes.Email)
        ?? user.FindFirstValue(ClaimTypes.Upn)
        ?? string.Empty;

    public static string GetDisplayName(this ClaimsPrincipal user) =>
        user.FindFirstValue("name")
        ?? user.Identity?.Name
        ?? user.GetEmail();
}
