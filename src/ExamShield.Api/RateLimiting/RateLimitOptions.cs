namespace ExamShield.Api.RateLimiting;

public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
}

public sealed class RateLimitOptions
{
    public RateLimitPolicyOptions Auth   { get; set; } = new();
    public RateLimitPolicyOptions Upload { get; set; } = new();
    public RateLimitPolicyOptions Global { get; set; } = new();
}
