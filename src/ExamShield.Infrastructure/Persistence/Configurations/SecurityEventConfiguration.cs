using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamShield.Infrastructure.Persistence.Configurations;

public sealed class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.ToTable("SecurityEvents");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.Severity)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.Message)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(e => e.UserId).HasMaxLength(256);
        builder.Property(e => e.IpAddress).HasMaxLength(64);

        builder.HasIndex(e => e.OccurredAt);
        builder.HasIndex(e => e.Severity);
    }
}
