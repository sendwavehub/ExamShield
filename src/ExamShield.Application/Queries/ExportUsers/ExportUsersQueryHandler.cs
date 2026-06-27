using System.Text;
using ExamShield.Domain.Enums;
using ExamShield.Domain.Interfaces;
using MediatR;

namespace ExamShield.Application.Queries.ExportUsers;

public sealed record ExportUsersResult(string Csv, string Filename);

public sealed record ExportUsersQuery(
    string? Search = null,
    string? Role = null)
    : IRequest<ExportUsersResult>;

public sealed class ExportUsersQueryHandler(IUserRepository users)
    : IRequestHandler<ExportUsersQuery, ExportUsersResult>
{
    private static readonly string[] Header =
        ["UserId", "Email", "Role", "IsActive", "CreatedAt"];

    public async Task<ExportUsersResult> Handle(ExportUsersQuery query, CancellationToken ct)
    {
        var all = await users.ListAllAsync(ct);

        var filtered = all.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(query.Search))
            filtered = filtered.Where(u => u.Email.Value.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(query.Role) && Enum.TryParse<UserRole>(query.Role, out var parsedRole))
            filtered = filtered.Where(u => u.Role == parsedRole);

        var csv = new StringBuilder();
        csv.AppendLine(string.Join(",", Header));

        foreach (var u in filtered.OrderBy(x => x.Email.Value))
        {
            csv.AppendLine(string.Join(",",
                u.Id.Value,
                EscapeCsv(u.Email.Value),
                u.Role.ToString(),
                u.IsActive,
                u.CreatedAt.ToString("O")));
        }

        var filename = $"users-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";
        return new ExportUsersResult(csv.ToString(), filename);
    }

    private static string EscapeCsv(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
