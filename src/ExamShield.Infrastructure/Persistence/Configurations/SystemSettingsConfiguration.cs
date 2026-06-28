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
        // Static seed — avoids PendingModelChangesWarning from dynamic DateTimeOffset.UtcNow default
        builder.HasData(new
        {
            Id = 1,
            OcrConfidenceThreshold = 0.85,
            NotificationsEnabled = true,
            NotificationSeverity = "High",
            AccessTokenExpiryMinutes = 60,
            RefreshTokenExpiryDays = 7,
            UpdatedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
        });
    }
}
