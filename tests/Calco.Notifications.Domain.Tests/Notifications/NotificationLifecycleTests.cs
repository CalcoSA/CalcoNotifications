using Calco.Notifications.Domain.Notifications;
using Calco.Notifications.Domain.Applications;
using Calco.Notifications.Domain.Recipients;
using Calco.Notifications.Domain.Templates;

namespace Calco.Notifications.Domain.Tests.Notifications
{
    public sealed class NotificationLifecycleTests
    {
        [Fact]
        public void CreateEmail_ShouldCreateNotificationInCreatedStatus()
        {
            var createdAt = DateTimeOffset.UtcNow;
            var notification = CreateEmailNotification(createdAt);

            Assert.Equal(NotificationStatus.Created, notification.Status);
            Assert.Equal(NotificationChannel.Email, notification.Channel);
            Assert.NotNull(notification.EmailRecipient);
            Assert.Null(notification.PhoneRecipient);
            Assert.Empty(notification.DeliveryAttempts);
        }

        [Fact]
        public void Queue_FromCreated_ShouldChangeStatusToQueued()
        {
            var createdAt = DateTimeOffset.UtcNow;
            var notification = CreateEmailNotification(createdAt);
            var queuedAt = createdAt.AddSeconds(1);

            notification.Queue(queuedAt);

            Assert.Equal(NotificationStatus.Queued, notification.Status);
            Assert.Equal(queuedAt, notification.QueuedAtUtc);
        }

        [Fact]
        public void StartProcessing_FromQueued_ShouldCreateDeliveryAttempt()
        {
            var createdAt = DateTimeOffset.UtcNow;
            var notification = CreateEmailNotification(createdAt);

            notification.Queue(createdAt.AddSeconds(1));

            var attempt = notification.StartProcessing("SMTP", createdAt.AddSeconds(2));

            Assert.Equal(NotificationStatus.Processing, notification.Status);
            Assert.Single(notification.DeliveryAttempts);
            Assert.Equal(1, attempt.AttemptNumber);
            Assert.Equal("SMTP", attempt.Provider);
        }

        [Fact]
        public void MarkProviderAccepted_FromProcessing_ShouldChangeStatus()
        {
            var createdAt = DateTimeOffset.UtcNow;
            var notification = CreateEmailNotification(createdAt);

            notification.Queue(createdAt.AddSeconds(1));

            var attempt = notification.StartProcessing("SMTP", createdAt.AddSeconds(2));

            notification.MarkProviderAccepted(attempt.Id, "provider-message-123", createdAt.AddSeconds(3));

            Assert.Equal(NotificationStatus.ProviderAccepted, notification.Status);
            Assert.True(attempt.WasSuccessful);
            Assert.Equal("provider-message-123", attempt.ProviderMessageId);
        }

        [Fact]
        public void MarkDelivered_FromProviderAccepted_ShouldChangeStatusToDelivered()
        {
            var createdAt = DateTimeOffset.UtcNow;
            var notification = CreateEmailNotification(createdAt);

            notification.Queue(createdAt.AddSeconds(1));

            var attempt = notification.StartProcessing("SMTP", createdAt.AddSeconds(2));

            notification.MarkProviderAccepted(attempt.Id, "provider-message-123", createdAt.AddSeconds(3));

            var deliveredAt = createdAt.AddSeconds(4);

            notification.MarkDelivered(deliveredAt);

            Assert.Equal(NotificationStatus.Delivered, notification.Status);
            Assert.Equal(deliveredAt, notification.DeliveredAtUtc);
        }

        [Fact]
        public void MarkDelivered_FromCreated_ShouldThrowException()
        {
            var notification = CreateEmailNotification(DateTimeOffset.UtcNow);

            Assert.Throws<InvalidOperationException>(() => notification.MarkDelivered(DateTimeOffset.UtcNow));
        }

        private static Notification CreateEmailNotification(DateTimeOffset createdAt, DateTimeOffset? expiresAt = null)
        {
            return Notification.CreateEmail(
                ClientApplicationId.New(),
                EmailAddress.Create("notifications@calco.com"),
                TemplateKey.Create("test-template"),
                NotificationPriority.Normal,
                IdempotencyKey.Create(Guid.NewGuid().ToString()),
                createdAt,
                expiresAt);
        }
    }
}