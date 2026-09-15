namespace Calco.Notifications.Domain.Notifications
{
    public readonly record struct DeliveryAttemptId(Guid Value)
    {
        public static DeliveryAttemptId New() => new(Guid.NewGuid());
    }
}