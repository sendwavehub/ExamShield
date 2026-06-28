using ExamShield.Domain.Entities;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.IssueCertificate;

public sealed record IssueCertificateCommand(Guid DeviceId, string PublicKeyPem, int ValidDays = 365)
    : IRequest<IssueCertificateResult>;

public sealed record IssueCertificateResult(
    Guid CertificateId,
    Guid DeviceId,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);

public sealed class IssueCertificateCommandHandler(
    IDeviceRepository devices,
    IDeviceCertificateRepository certificates)
    : IRequestHandler<IssueCertificateCommand, IssueCertificateResult>
{
    public async Task<IssueCertificateResult> Handle(IssueCertificateCommand cmd, CancellationToken ct)
    {
        var deviceId = new DeviceId(cmd.DeviceId);
        _ = await devices.GetByIdAsync(deviceId, ct)
            ?? throw new DeviceNotFoundException(cmd.DeviceId);

        var cert = DeviceCertificate.Issue(deviceId, cmd.PublicKeyPem, cmd.ValidDays);
        await certificates.AddAsync(cert, ct);

        return new IssueCertificateResult(cert.Id, cert.DeviceId.Value, cert.IssuedAt, cert.ExpiresAt);
    }
}
