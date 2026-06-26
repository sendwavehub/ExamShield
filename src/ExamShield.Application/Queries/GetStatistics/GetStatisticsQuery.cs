using MediatR;

namespace ExamShield.Application.Queries.GetStatistics;

public sealed record GetStatisticsQuery : IRequest<GetStatisticsResult>;

public sealed record GetStatisticsResult(
    int TotalPapersScored,
    double AveragePercentage,
    int HighestScore,
    int LowestScore);
