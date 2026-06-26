namespace ExamShield.Domain.ValueObjects;

public sealed class AnswerKey
{
    private readonly IReadOnlyDictionary<int, string> _answers;

    public AnswerKey(IReadOnlyDictionary<int, string> answers)
    {
        ArgumentNullException.ThrowIfNull(answers);
        _answers = answers;
    }

    public int Count => _answers.Count;

    public bool IsCorrect(int questionNumber, string givenAnswer) =>
        _answers.TryGetValue(questionNumber, out var correct) &&
        string.Equals(correct, givenAnswer, StringComparison.OrdinalIgnoreCase);
}
