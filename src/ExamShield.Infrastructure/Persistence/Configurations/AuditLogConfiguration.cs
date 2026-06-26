using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamShield.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, v => new AuditLogId(v));

        builder.Property(a => a.Action)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.CaptureId)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                v => v != null ? new CaptureId(v.Value) : null);

        builder.Property(a => a.UserId).HasMaxLength(256);
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.Reason).HasMaxLength(500);

        // Store as UTC ticks (long) so SQLite can sort on it without a DateTimeOffset cast.
        builder.Property(a => a.OccurredAt)
            .HasConversion(
                dt => dt.UtcTicks,
                v => new DateTimeOffset(v, TimeSpan.Zero));

        builder.HasIndex(a => a.CaptureId);
        builder.HasIndex(a => a.OccurredAt);
    }
}
