namespace ExamShield.Application.Interfaces;

public interface IDemoDataSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}
