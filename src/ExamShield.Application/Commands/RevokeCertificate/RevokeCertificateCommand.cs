using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Commands.RevokeCertificate;

public sealed record RevokeCertificateCommand(Guid CertificateId, string Reason) : IRequest;

public sealed class RevokeCertificateCommandHandler(IDeviceCertificateRepository certificates)
    : IRequestHandler<RevokeCertificateCommand>
{
    public async Task Handle(RevokeCertificateCommand cmd, CancellationToken ct)
    {
        var cert = await certificates.GetByIdAsync(cmd.CertificateId, ct)
            ?? throw new KeyNotFoundException($"Certificate {cmd.CertificateId} not found.");

        cert.Revoke(cmd.Reason);
        await certificates.UpdateAsync(cert, ct);
    }
}
