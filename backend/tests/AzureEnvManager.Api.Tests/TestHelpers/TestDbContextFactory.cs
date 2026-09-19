using AzureEnvManager.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AzureEnvManager.Api.Tests.TestHelpers;

public static class TestDbContextFactory
{
    /// <summary>A fresh, isolated in-memory database per call so tests never see each other's data.</summary>
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
