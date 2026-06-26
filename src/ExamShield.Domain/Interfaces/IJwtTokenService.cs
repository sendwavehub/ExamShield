using ExamShield.Domain.Entities;

namespace ExamShield.Domain.Interfaces;

public interface IJwtTokenService
{
    string Generate(User user);
}
