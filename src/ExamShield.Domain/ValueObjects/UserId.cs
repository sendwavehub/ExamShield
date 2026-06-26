namespace ExamShield.Domain.ValueObjects;

public sealed record UserId : GuidId
{
    public UserId(Guid value) : base(value) { }
    public static UserId New() => new(Guid.NewGuid());
}
