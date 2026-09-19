using System.Security.Claims;

namespace AzureEnvManager.Api.Tests.TestHelpers;

public static class TestPrincipal
{
    public static ClaimsPrincipal Create(string objectId, string email, string displayName, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new("oid", objectId),
            new("preferred_username", email),
            new("name", displayName),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }
}
