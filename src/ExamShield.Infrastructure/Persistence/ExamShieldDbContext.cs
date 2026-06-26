using ExamShield.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamShield.Infrastructure.Persistence;

public class ExamShieldDbContext : DbContext
{
    public ExamShieldDbContext(DbContextOptions<ExamShieldDbContext> options) : base(options) { }

    public DbSet<Capture> Captures => Set<Capture>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExamShieldDbContext).Assembly);
}
