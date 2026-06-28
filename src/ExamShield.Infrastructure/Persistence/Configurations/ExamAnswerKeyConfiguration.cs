using ExamShield.Domain.Entities;
using ExamShield.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamShield.Infrastructure.Persistence.Configurations;

internal sealed class ExamAnswerKeyConfiguration : IEntityTypeConfiguration<ExamAnswerKey>
{
    public void Configure(EntityTypeBuilder<ExamAnswerKey> builder)
    {
        builder.ToTable("ExamAnswerKeys");

        builder.HasKey(e => e.ExamId);
        builder.Property(e => e.ExamId)
            .HasConversion(id => id.Value, v => new ExamId(v));

        builder.Property(e => e.CreatedAt)
            .HasConversion(dt => dt.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));

        builder.Property(e => e.Answers)
            .HasColumnName("Answers")
            .HasColumnType("jsonb")
            .HasConversion(
                d => System.Text.Json.JsonSerializer.Serialize(d, (System.Text.Json.JsonSerializerOptions?)null),
                s => System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, string>>(s,
                    (System.Text.Json.JsonSerializerOptions?)null)
                     ?? new Dictionary<int, string>());

        builder.Ignore(e => e.DomainEvents);
    }
}
