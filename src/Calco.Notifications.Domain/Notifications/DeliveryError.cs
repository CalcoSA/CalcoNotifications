namespace Calco.Notifications.Domain.Notifications
{
    public sealed record DeliveryError(string Code, string Message, bool IsRetryable);
}