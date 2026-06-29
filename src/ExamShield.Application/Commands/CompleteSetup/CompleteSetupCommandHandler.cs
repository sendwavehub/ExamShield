using ExamShield.Application.Interfaces;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ExamShield.Application.Commands.CompleteSetup;

public sealed class CompleteSetupCommandHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IAuditLogRepository auditLog,
    IServiceScopeFactory scopeFactory)
    : IRequestHandler<CompleteSetupCommand, CompleteSetupResult>
{
    public async Task<CompleteSetupResult> Handle(CompleteSetupCommand command, CancellationToken ct)
    {
        var all = await users.ListAllAsync(ct);
        if (all.Any(u => u.Role == UserRole.SuperAdministrator))
            throw new InvalidOperationException("Setup has already been completed.");

        var email = new Email(command.AdminEmail);
        if (await users.FindByEmailAsync(email, ct) is not null)
            throw new UserAlreadyExistsException(command.AdminEmail);

        var hash = hasher.Hash(new Password(command.AdminPassword).Value);
        var admin = User.Create(email, hash, UserRole.SuperAdministrator);
        admin.UpdateProfile(command.AdminDisplayName);

        await users.AddAsync(admin, ct);
        await auditLog.AppendAsync(
            AuditLog.Record(AuditAction.UserCreated,
                reason: "First-run setup: Super Administrator created"),
            ct);

        if (command.SeedDemoData)
        {
            using var scope = scopeFactory.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<IDemoDataSeeder>();
            await seeder.SeedAsync(ct);
        }

        return new CompleteSetupResult(admin.Id.Value);
    }
}
