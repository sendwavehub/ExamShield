namespace ExamShield.Domain.Exceptions;

public sealed class ImageNotFoundException(string storageKey)
    : Exception($"Image not found at storage key: {storageKey}");
