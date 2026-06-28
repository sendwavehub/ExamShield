using ExamShield.Domain.Entities;
using ExamShield.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamShield.Infrastructure.Persistence.Configurations;

internal sealed class ExamCandidateConfiguration : IEntityTypeConfiguration<ExamCandidate>
{
    public void Configure(EntityTypeBuilder<ExamCandidate> builder)
    {
        builder.ToTable("ExamCandidates");

        builder.HasKey(e => new { ExamIdValue = e.ExamId, StudentIdValue = e.StudentId });

        builder.Property(e => e.ExamId)
            .HasConversion(id => id.Value, v => new ExamId(v))
            .HasColumnName("ExamId");

        builder.Property(e => e.StudentId)
            .HasConversion(id => id.Value, v => new StudentId(v))
            .HasColumnName("StudentId");

        builder.Property(e => e.EnrolledAt)
            .HasConversion(dt => dt.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));

        builder.Ignore(e => e.DomainEvents);
    }
}
