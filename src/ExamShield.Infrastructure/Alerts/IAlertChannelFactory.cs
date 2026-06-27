using ExamShield.Domain.Entities;

namespace ExamShield.Infrastructure.Alerts;

public interface IAlertChannelFactory
{
    IAlertChannel CreateChannel(string type, NotificationChannelSettings settings);
}
