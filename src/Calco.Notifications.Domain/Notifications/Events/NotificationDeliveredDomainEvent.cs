using Calco.Notifications.Domain.Common;

namespace Calco.Notifications.Domain.Notifications.Events
{
    public sealed record NotificationDeliveredDomainEvent(NotificationId NotificationId, DateTimeOffset OccurredAtUtc) : IDomainEvent;
}