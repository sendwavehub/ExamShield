using ExamShield.Domain.Interfaces;
using ExamShield.Infrastructure.Alerts;
using ExamShield.Infrastructure.OCR;
using ExamShield.Infrastructure.Persistence;
using ExamShield.Infrastructure.Persistence.Repositories;
using ExamShield.Infrastructure.Security;
using ExamShield.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExamShield.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ExamShieldDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ICaptureRepository, CaptureRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<ISignatureVerificationService, EcdsaSignatureVerificationService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IImageStorage, InMemoryImageStorage>();
        services.AddSingleton<IOcrService, StubOcrService>();
        services.AddSingleton<IOcrResultRepository, InMemoryOcrResultRepository>();
        services.AddSingleton<IManualReviewRepository, InMemoryManualReviewRepository>();
        services.AddHttpClient("Alerts");
        services.AddSingleton<IAlertService, AlertService>();
        return services;
    }
}
