using ExamShield.Domain.Entities;
using ExamShield.Domain.ValueObjects;

namespace ExamShield.Domain.Interfaces;

public interface IDeviceCertificateRepository
{
    Task AddAsync(DeviceCertificate cert, CancellationToken ct = default);
    Task<DeviceCertificate?> GetActiveAsync(DeviceId deviceId, CancellationToken ct = default);
    Task<IReadOnlyList<DeviceCertificate>> GetAllForDeviceAsync(DeviceId deviceId, CancellationToken ct = default);
    Task UpdateAsync(DeviceCertificate cert, CancellationToken ct = default);
}
