using Calco.Notifications.Domain.Common;

namespace Calco.Notifications.Domain.Notifications.Events
{
    public sealed record NotificationFailedDomainEvent(NotificationId NotificationId, string ErrorCode, bool IsRetryable, DateTimeOffset OccurredAtUtc) : IDomainEvent;
}