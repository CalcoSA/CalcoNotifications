using Calco.Notifications.Domain.Common;

namespace Calco.Notifications.Domain.Notifications.Events
{
    public sealed record NotificationCreatedDomainEvent(NotificationId NotificationId, DateTimeOffset OccurredAtUtc) : IDomainEvent;
}