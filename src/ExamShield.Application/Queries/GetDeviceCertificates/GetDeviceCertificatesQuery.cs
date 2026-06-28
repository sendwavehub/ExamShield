using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Queries.GetDeviceCertificates;

public sealed record GetDeviceCertificatesQuery(Guid DeviceId) : IRequest<IReadOnlyList<DeviceCertificateDto>>;

public sealed record DeviceCertificateDto(
    Guid Id,
    Guid DeviceId,
    string PublicKeyPem,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    bool IsRevoked,
    DateTimeOffset? RevokedAt,
    string? RevocationReason,
    bool IsValid);

public sealed class GetDeviceCertificatesQueryHandler(IDeviceCertificateRepository certificates)
    : IRequestHandler<GetDeviceCertificatesQuery, IReadOnlyList<DeviceCertificateDto>>
{
    public async Task<IReadOnlyList<DeviceCertificateDto>> Handle(
        GetDeviceCertificatesQuery query, CancellationToken ct)
    {
        var all = await certificates.GetAllForDeviceAsync(new DeviceId(query.DeviceId), ct);
        return all.Select(c => new DeviceCertificateDto(
            c.Id, c.DeviceId.Value, c.PublicKeyPem,
            c.IssuedAt, c.ExpiresAt,
            c.IsRevoked, c.RevokedAt, c.RevocationReason,
            c.IsValid)).ToList();
    }
}
