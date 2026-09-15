namespace Calco.Notifications.Domain.Common
{
    public interface IDomainEvent
    {
        DateTimeOffset OccurredAtUtc { get; }
    }
}