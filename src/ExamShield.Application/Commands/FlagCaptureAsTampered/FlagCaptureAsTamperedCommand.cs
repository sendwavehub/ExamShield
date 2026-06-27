using ExamShield.Domain.Entities;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using ExamShield.Domain.ValueObjects;
using MediatR;

namespace ExamShield.Application.Commands.FlagCaptureAsTampered;

public sealed record FlagCaptureAsTamperedCommand(Guid CaptureId, string Reason) : IRequest;

public sealed class FlagCaptureAsTamperedCommandHandler(
    ICaptureRepository captures,
    IAuditLogRepository audit,
    IAlertService alerts)
    : IRequestHandler<FlagCaptureAsTamperedCommand>
{
    public async Task Handle(FlagCaptureAsTamperedCommand command, CancellationToken ct)
    {
        var capture = await captures.GetByIdAsync(new CaptureId(command.CaptureId), ct)
            ?? throw new CaptureNotFoundException(command.CaptureId);

        capture.FlagAsTampered(command.Reason);

        await captures.UpdateAsync(capture, ct);
        await audit.AppendAsync(
            AuditLog.Record(
                AuditAction.TamperingDetected,
                captureId: capture.Id,
                reason: command.Reason),
            ct);
        await alerts.SendAsync(
            AlertType.TamperingDetected,
            $"Capture {command.CaptureId} manually flagged as tampered. Reason: {command.Reason}",
            ct);
    }
}
