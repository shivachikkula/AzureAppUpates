using AzureEnvManager.Api.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace AzureEnvManager.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<AzureApplication> Applications => Set<AzureApplication>();
    public DbSet<AppAssignment> Assignments => Set<AppAssignment>();
    public DbSet<ManagerTeamAssignment> ManagerAssignments => Set<ManagerTeamAssignment>();
    public DbSet<ChangeRequest> ChangeRequests => Set<ChangeRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasIndex(t => t.Name).IsUnique();
        });

        modelBuilder.Entity<AzureApplication>(entity =>
        {
            entity.HasIndex(a => new { a.SubscriptionId, a.ResourceGroup, a.Name }).IsUnique();
            entity.Property(a => a.Environment).HasConversion<string>();
            entity.HasOne(a => a.Team)
                .WithMany(t => t.Applications)
                .HasForeignKey(a => a.TeamId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppAssignment>(entity =>
        {
            entity.HasIndex(a => new { a.ApplicationId, a.UserObjectId }).IsUnique();
            entity.HasOne(a => a.Application)
                .WithMany(app => app.Assignments)
                .HasForeignKey(a => a.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ManagerTeamAssignment>(entity =>
        {
            entity.HasIndex(a => new { a.TeamId, a.UserObjectId }).IsUnique();
            entity.HasOne(a => a.Team)
                .WithMany(t => t.Managers)
                .HasForeignKey(a => a.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChangeRequest>(entity =>
        {
            entity.Property(c => c.Type).HasConversion<string>();
            entity.Property(c => c.Status).HasConversion<string>();
            entity.HasOne(c => c.Application)
                .WithMany()
                .HasForeignKey(c => c.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
