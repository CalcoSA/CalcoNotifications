using Calco.Notifications.Domain.Common;

namespace Calco.Notifications.Domain.Notifications.Events
{
    public sealed record NotificationQueuedDomainEvent(NotificationId NotificationId, DateTimeOffset OccurredAtUtc) : IDomainEvent;
}