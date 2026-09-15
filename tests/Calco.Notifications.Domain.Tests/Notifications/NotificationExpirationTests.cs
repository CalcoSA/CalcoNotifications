using Calco.Notifications.Domain.Notifications;
using Calco.Notifications.Domain.Applications;
using Calco.Notifications.Domain.Recipients;
using Calco.Notifications.Domain.Templates;

namespace Calco.Notifications.Domain.Tests.Notifications
{
    public sealed class NotificationExpirationTests
    {
        [Fact]
        public void CreateNotification_WithExpirationBeforeCreation_ShouldThrowException()
        {
            var createdAt = DateTimeOffset.UtcNow;

            Assert.Throws<ArgumentException>(() =>
                Notification.CreateEmail(
                    ClientApplicationId.New(),
                    EmailAddress.Create("notifications@calco.com"),
                    TemplateKey.Create("login-otp"),
                    NotificationPriority.Critical,
                    IdempotencyKey.Create(Guid.NewGuid().ToString()),
                    createdAt,
                    createdAt.AddMinutes(-1)));
        }

        [Fact]
        public void Queue_WhenNotificationHasExpired_ShouldThrowException()
        {
            var createdAt = DateTimeOffset.UtcNow;
            var expiresAt = createdAt.AddMinutes(5);

            var notification = Notification.CreateEmail(
                ClientApplicationId.New(),
                EmailAddress.Create("notifications@calco.com"),
                TemplateKey.Create("login-otp"),
                NotificationPriority.Critical,
                IdempotencyKey.Create(Guid.NewGuid().ToString()),
                createdAt,
                expiresAt);

            Assert.Throws<InvalidOperationException>(() => notification.Queue(expiresAt.AddSeconds(1)));
            Assert.Equal(NotificationStatus.Expired, notification.Status);
        }

        [Fact]
        public void Expire_WhenExpirationTimeWasReached_ShouldSetExpired()
        {
            var createdAt = DateTimeOffset.UtcNow;
            var expiresAt = createdAt.AddMinutes(5);

            var notification = Notification.CreateEmail(
                ClientApplicationId.New(),
                EmailAddress.Create("notifications@calco.com"),
                TemplateKey.Create("login-otp"),
                NotificationPriority.Critical,
                IdempotencyKey.Create(Guid.NewGuid().ToString()),
                createdAt,
                expiresAt);

            notification.Expire(expiresAt);

            Assert.Equal(NotificationStatus.Expired, notification.Status);
        }
    }
}