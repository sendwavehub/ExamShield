namespace ExamShield.Domain.ValueObjects;

public sealed class ExtractedAnswer
{
    public int QuestionNumber { get; }
    public string Text { get; }
    public OcrConfidence Confidence { get; }

    public ExtractedAnswer(int questionNumber, string text, OcrConfidence confidence)
    {
        if (questionNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(questionNumber), "Question number must be positive.");
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(confidence);
        QuestionNumber = questionNumber;
        Text = text;
        Confidence = confidence;
    }
}
