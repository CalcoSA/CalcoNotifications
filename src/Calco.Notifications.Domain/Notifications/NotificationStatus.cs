namespace Calco.Notifications.Domain.Notifications
{
    public enum NotificationStatus
    {
        Created = 1,
        Queued = 2,
        Processing = 3,
        ProviderAccepted = 4,
        Delivered = 5,
        RetryScheduled = 6,
        Failed = 7,
        DeadLettered = 8,
        Expired = 9
    }
}