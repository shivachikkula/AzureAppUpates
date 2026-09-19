namespace AzureEnvManager.Api.Authorization;

public static class AppRoles
{
    public const string Developer = "Developer";
    public const string Manager = "Manager";
    public const string Admin = "Admin";

    public const string ManagerPolicy = "RequireManager";
    public const string AdminPolicy = "RequireAdmin";
}
