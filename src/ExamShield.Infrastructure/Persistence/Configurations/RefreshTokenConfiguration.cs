using ExamShield.Domain.Entities;
using ExamShield.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamShield.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.HasKey(t => t.Id);

        b.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        b.HasIndex(t => t.TokenHash)
            .IsUnique();

        b.Property(t => t.UserId)
            .HasConversion(id => id.Value, v => new UserId(v))
            .IsRequired();

        b.Property(t => t.ExpiresAt).IsRequired();
        b.Property(t => t.RevokedAt);
    }
}
