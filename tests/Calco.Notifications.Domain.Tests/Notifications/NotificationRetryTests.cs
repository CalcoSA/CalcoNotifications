using Calco.Notifications.Domain.Notifications;
using Calco.Notifications.Domain.Applications;
using Calco.Notifications.Domain.Recipients;
using Calco.Notifications.Domain.Templates;

namespace Calco.Notifications.Domain.Tests.Notifications
{
    public sealed class NotificationRetryTests
    {
        [Fact]
        public void RegisterFailure_ShouldSetNotificationAsFailed()
        {
            var createdAt = DateTimeOffset.UtcNow;
            var notification = CreateNotification(createdAt);

            notification.Queue(createdAt.AddSeconds(1));

            var attempt = notification.StartProcessing("SMTP", createdAt.AddSeconds(2));
            var error = new DeliveryError("SMTP_TIMEOUT", "SMTP connection timed out.", true);

            notification.RegisterFailure(attempt.Id, error, createdAt.AddSeconds(3));

            Assert.Equal(NotificationStatus.Failed, notification.Status);
            Assert.False(attempt.WasSuccessful);
            Assert.Equal(error, attempt.Error);
        }

        [Fact]
        public void ScheduleRetry_WhenFailureIsRetryable_ShouldScheduleRetry()
        {
            var createdAt = DateTimeOffset.UtcNow;
            var notification = CreateNotification(createdAt);

            notification.Queue(createdAt.AddSeconds(1));

            var attempt = notification.StartProcessing("SMTP", createdAt.AddSeconds(2));

            notification.RegisterFailure(attempt.Id, new DeliveryError("SMTP_TIMEOUT", "SMTP connection timed out.", true), createdAt.AddSeconds(3));

            var retryAt = createdAt.AddMinutes(1);

            notification.ScheduleRetry(retryAt);

            Assert.Equal(NotificationStatus.RetryScheduled, notification.Status);
            Assert.Equal(retryAt, notification.NextRetryAtUtc);
        }

        [Fact]
        public void ScheduleRetry_WhenFailureIsNotRetryable_ShouldThrowException()
        {
            var createdAt = DateTimeOffset.UtcNow;
            var notification = CreateNotification(createdAt);

            notification.Queue(createdAt.AddSeconds(1));

            var attempt = notification.StartProcessing("SMTP", createdAt.AddSeconds(2));

            notification.RegisterFailure(attempt.Id, new DeliveryError("INVALID_RECIPIENT", "Recipient is invalid.", false), createdAt.AddSeconds(3));

            Assert.Throws<InvalidOperationException>(() => notification.ScheduleRetry(createdAt.AddMinutes(1)));
        }

        [Fact]
        public void Queue_AfterRetryScheduled_ShouldAllowNewProcessingAttempt()
        {
            var createdAt = DateTimeOffset.UtcNow;
            var notification = CreateNotification(createdAt);

            notification.Queue(createdAt.AddSeconds(1));

            var firstAttempt = notification.StartProcessing("SMTP", createdAt.AddSeconds(2));

            notification.RegisterFailure(firstAttempt.Id, new DeliveryError("SMTP_TIMEOUT", "Temporary error.", true), createdAt.AddSeconds(3));
            notification.ScheduleRetry(createdAt.AddMinutes(1));
            notification.Queue(createdAt.AddMinutes(1));

            var secondAttempt = notification.StartProcessing("SMTP", createdAt.AddMinutes(1).AddSeconds(1));

            Assert.Equal(2, secondAttempt.AttemptNumber);
            Assert.Equal(NotificationStatus.Processing, notification.Status);
            Assert.Equal(2, notification.DeliveryAttempts.Count);
        }

        [Fact]
        public void ScheduleRetry_AfterExpiration_ShouldThrowException()
        {
            var createdAt = DateTimeOffset.UtcNow;
            var expiresAt = createdAt.AddMinutes(5);
            var notification = CreateNotification(createdAt, expiresAt);

            notification.Queue(createdAt.AddSeconds(1));

            var attempt = notification.StartProcessing("SMTP", createdAt.AddSeconds(2));

            notification.RegisterFailure(attempt.Id, new DeliveryError("SMTP_TIMEOUT", "Temporary error.", true), createdAt.AddMinutes(1));

            Assert.Throws<InvalidOperationException>(() => notification.ScheduleRetry(expiresAt));
        }

        [Fact]
        public void MarkDeadLettered_FromFailed_ShouldChangeStatus()
        {
            var createdAt = DateTimeOffset.UtcNow;
            var notification = CreateNotification(createdAt);

            notification.Queue(createdAt.AddSeconds(1));

            var attempt = notification.StartProcessing("SMTP", createdAt.AddSeconds(2));

            notification.RegisterFailure(attempt.Id, new DeliveryError("INVALID_RECIPIENT", "Recipient is invalid.", false), createdAt.AddSeconds(3));
            notification.MarkDeadLettered();

            Assert.Equal(NotificationStatus.DeadLettered, notification.Status);
        }

        private static Notification CreateNotification(DateTimeOffset createdAt, DateTimeOffset? expiresAt = null)
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