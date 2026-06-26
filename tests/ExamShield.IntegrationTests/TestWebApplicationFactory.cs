using System.Net.Http.Headers;
using System.Net.Http.Json;
using ExamShield.Api.Contracts;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using ExamShield.IntegrationTests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExamShield.IntegrationTests;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.examshield";
    public const string AdminPassword = "Test@1234!Admin";

    // Pre-seed with a test admin whose password is BCrypt-hashed at low work factor for speed.
    private static readonly User _adminUser = User.Create(
        new Email(AdminEmail),
        BCrypt.Net.BCrypt.HashPassword(AdminPassword, workFactor: 4),
        UserRole.Administrator);

    private string? _cachedToken;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // Swap real repositories for fast in-memory fakes.
            services.RemoveAll<ICaptureRepository>();
            services.AddSingleton<ICaptureRepository, InMemoryCaptureRepository>();

            services.RemoveAll<IAuditLogRepository>();
            services.AddSingleton<IAuditLogRepository, InMemoryAuditLogRepository>();

            services.RemoveAll<IDeviceRepository>();
            services.AddSingleton<IDeviceRepository, InMemoryDeviceRepository>();

            services.RemoveAll<IUserRepository>();
            services.AddSingleton<IUserRepository>(
                new InMemoryUserRepository(seed: [_adminUser]));

            // Replace real AlertService (makes HTTP calls) with a no-op fake for tests.
            services.RemoveAll<IAlertService>();
            services.AddSingleton<IAlertService, NullAlertService>();

            services.RemoveAll<IOcrResultRepository>();
            services.AddSingleton<IOcrResultRepository, InMemoryOcrResultRepository>();

            services.RemoveAll<IManualReviewRepository>();
            services.AddSingleton<IManualReviewRepository, InMemoryManualReviewRepository>();

            services.RemoveAll<IScoreRepository>();
            services.AddSingleton<IScoreRepository, InMemoryScoreRepository>();

            // IPasswordHasher and IJwtTokenService stay — real BCrypt + real JWT for auth tests.
        });
    }

    public async Task<string> GetAuthTokenAsync()
    {
        if (_cachedToken is not null)
            return _cachedToken;

        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login",
            new LoginRequest(AdminEmail, AdminPassword));

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        _cachedToken = body!.Token;
        return _cachedToken;
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var token = await GetAuthTokenAsync();
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
