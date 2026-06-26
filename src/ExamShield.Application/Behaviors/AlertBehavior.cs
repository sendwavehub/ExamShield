using ExamShield.Domain.Enums;
using ExamShield.Domain.Exceptions;
using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Behaviors;

public sealed class AlertBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAlertService _alertService;

    public AlertBehavior(IAlertService alertService) => _alertService = alertService;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        try
        {
            return await next(ct);
        }
        catch (HashMismatchException ex)
        {
            await _alertService.SendAsync(AlertType.HashMismatch, ex.Message, ct);
            throw;
        }
        catch (InvalidSignatureException ex)
        {
            await _alertService.SendAsync(AlertType.InvalidSignature, ex.Message, ct);
            throw;
        }
        catch (DuplicateUploadException ex)
        {
            await _alertService.SendAsync(AlertType.DuplicateUpload, ex.Message, ct);
            throw;
        }
    }
}
