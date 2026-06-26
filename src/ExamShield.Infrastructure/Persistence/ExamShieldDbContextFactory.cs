using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExamShield.Infrastructure.Persistence;

// Used exclusively by the `dotnet ef` CLI at design time (migrations add/remove/script).
// Never instantiated at runtime — the real DbContext comes from DI.
public sealed class ExamShieldDbContextFactory : IDesignTimeDbContextFactory<ExamShieldDbContext>
{
    public ExamShieldDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ExamShieldDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=examshield;Username=examshield;Password=examshield")
            .Options;

        return new ExamShieldDbContext(options);
    }
}
