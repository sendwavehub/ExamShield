using ExamShield.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamShield.Infrastructure.Persistence.Configurations;

public sealed class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    public void Configure(EntityTypeBuilder<SystemSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.NotificationSeverity).HasMaxLength(20).IsRequired();
        builder.Property(s => s.UpdatedAt)
            .HasConversion(dt => dt.UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
        builder.HasData(SystemSettings.CreateDefault());
    }
}
