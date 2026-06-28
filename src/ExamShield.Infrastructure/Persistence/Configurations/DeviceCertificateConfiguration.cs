using ExamShield.Domain.Entities;
using ExamShield.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamShield.Infrastructure.Persistence.Configurations;

public sealed class DeviceCertificateConfiguration : IEntityTypeConfiguration<DeviceCertificate>
{
    public void Configure(EntityTypeBuilder<DeviceCertificate> builder)
    {
        builder.ToTable("DeviceCertificates");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.DeviceId)
            .HasConversion(id => id.Value, v => new DeviceId(v));

        builder.Property(c => c.PublicKeyPem).IsRequired();

        builder.Property(c => c.IssuedAt)
            .HasConversion(dt => dt.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));

        builder.Property(c => c.ExpiresAt)
            .HasConversion(dt => dt.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));

        builder.Property(c => c.RevokedAt)
            .HasConversion(
                dt => dt.HasValue ? (long?)dt.Value.UtcTicks : null,
                v  => v.HasValue  ? (DateTimeOffset?)new DateTimeOffset(v.Value, TimeSpan.Zero) : null);

        builder.HasIndex(c => c.DeviceId);
    }
}
