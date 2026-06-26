namespace ExamShield.Domain.ValueObjects;

public sealed record DeviceId : GuidId
{
    public DeviceId(Guid value) : base(value) { }
    public static DeviceId New() => new(Guid.NewGuid());
}
