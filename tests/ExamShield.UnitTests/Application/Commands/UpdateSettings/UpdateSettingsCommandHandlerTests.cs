using ExamShield.Application.Commands.UpdateSettings;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace ExamShield.UnitTests.Application.Commands.UpdateSettings;

public sealed class UpdateSettingsCommandHandlerTests
{
    private readonly ISystemSettingsRepository _repo = Substitute.For<ISystemSettingsRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();

    private UpdateSettingsCommandHandler CreateHandler() => new(_repo, _audit);

    private static UpdateSettingsCommand DefaultCommand(
        double threshold = 0.85,
        bool notifications = true,
        string severity = "Warning",
        int accessMins = 60,
        int refreshDays = 7)
        => new(threshold, notifications, severity, accessMins, refreshDays);

    [Fact]
    public async Task Handle_UpdatesSettingsAndReturnsDto()
    {
        var settings = SystemSettings.CreateDefault();
        _repo.GetAsync(default).ReturnsForAnyArgs(settings);

        var result = await CreateHandler().Handle(DefaultCommand(), default);

        result.OcrConfidenceThreshold.Should().Be(0.85);
        result.NotificationsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PersistsSettings()
    {
        _repo.GetAsync(default).ReturnsForAnyArgs(SystemSettings.CreateDefault());

        await CreateHandler().Handle(DefaultCommand(), default);

        await _repo.Received(1).SaveAsync(Arg.Any<SystemSettings>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AppendsSettingsUpdatedAuditLog()
    {
        _repo.GetAsync(default).ReturnsForAnyArgs(SystemSettings.CreateDefault());

        await CreateHandler().Handle(DefaultCommand(), default);

        await _audit.Received(1).AppendAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.SettingsUpdated),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsDtoWithAllFieldsMapped()
    {
        _repo.GetAsync(default).ReturnsForAnyArgs(SystemSettings.CreateDefault());

        var cmd = DefaultCommand(0.9, false, "Critical", 30, 14);
        var result = await CreateHandler().Handle(cmd, default);

        result.OcrConfidenceThreshold.Should().Be(0.9);
        result.NotificationsEnabled.Should().BeFalse();
        result.NotificationSeverity.Should().Be("Critical");
        result.AccessTokenExpiryMinutes.Should().Be(30);
        result.RefreshTokenExpiryDays.Should().Be(14);
    }
}
