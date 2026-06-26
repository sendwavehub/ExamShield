namespace ExamShield.Domain.ValueObjects;

public sealed record AuditLogId : GuidId
{
    public AuditLogId(Guid value) : base(value) { }
    public static AuditLogId New() => new(Guid.NewGuid());
}
