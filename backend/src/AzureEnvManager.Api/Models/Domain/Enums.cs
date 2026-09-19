namespace AzureEnvManager.Api.Models.Domain;

public enum AppEnvironment
{
    Development,
    Staging,
    Production,
}

public enum ConnectionStringKind
{
    Custom,
    SQLAzure,
    PostgreSQL,
    MySql,
}

public enum ChangeRequestStatus
{
    PendingApproval,
    Approved,
    Rejected,
    Applied,
    Failed,
}
