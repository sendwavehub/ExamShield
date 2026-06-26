namespace ExamShield.Domain.ValueObjects;

public sealed class ReviewedAnswer
{
    public int QuestionNumber { get; }
    public string Text { get; }

    public ReviewedAnswer(int questionNumber, string text)
    {
        if (questionNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(questionNumber), "Question number must be positive.");
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        QuestionNumber = questionNumber;
        Text = text;
    }
}
