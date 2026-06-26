namespace ExamShield.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string Section = "Storage";
    public string Type { get; init; } = "InMemory";
    public string Endpoint { get; init; } = "localhost:9000";
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string BucketName { get; init; } = "examshield";
    public bool UseSSL { get; init; } = false;
}
