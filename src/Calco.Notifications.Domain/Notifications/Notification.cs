using Calco.Notifications.Domain.Notifications.Events;
using Calco.Notifications.Domain.Recipients;
using Calco.Notifications.Domain.Templates;
using Calco.Notifications.Domain.Common;

namespace Calco.Notifications.Domain.Notifications
{
    public sealed class Notification : AggregateRoot
    {
        private readonly List<DeliveryAttempt> _deliveryAttempts = [];

        private Notification() { }

        private Notification(NotificationId id, ApplicationId applicationId, NotificationChannel channel, EmailAddress? emailRecipient, PhoneNumber? phoneRecipient, TemplateKey templateKey, NotificationPriority priority, IdempotencyKey idempotencyKey, DateTimeOffset createdAtUtc, DateTimeOffset? expiresAtUtc)
        {
            Id = id;
            ApplicationId = applicationId;
            Channel = channel;
            EmailRecipient = emailRecipient;
            PhoneRecipient = phoneRecipient;
            TemplateKey = templateKey;
            Priority = priority;
            IdempotencyKey = idempotencyKey;
            CreatedAtUtc = createdAtUtc;
            ExpiresAtUtc = expiresAtUtc;
            Status = NotificationStatus.Created;

            ValidateRecipient();
            RaiseDomainEvent(new NotificationCreatedDomainEvent(Id, createdAtUtc));
        }

        public NotificationId Id { get; private set; }
        public ApplicationId ApplicationId { get; private set; } = default!;
        public NotificationChannel Channel { get; private set; }
        public EmailAddress? EmailRecipient { get; private set; }
        public PhoneNumber? PhoneRecipient { get; private set; }
        public TemplateKey TemplateKey { get; private set; } = default!;
        public NotificationPriority Priority { get; private set; }
        public NotificationStatus Status { get; private set; }
        public IdempotencyKey IdempotencyKey { get; private set; } = default!;
        public DateTimeOffset CreatedAtUtc { get; private set; }
        public DateTimeOffset? QueuedAtUtc { get; private set; }
        public DateTimeOffset? ProcessingStartedAtUtc { get; private set; }
        public DateTimeOffset? ProviderAcceptedAtUtc { get; private set; }
        public DateTimeOffset? DeliveredAtUtc { get; private set; }
        public DateTimeOffset? FailedAtUtc { get; private set; }
        public DateTimeOffset? NextRetryAtUtc { get; private set; }
        public DateTimeOffset? ExpiresAtUtc { get; private set; }

        public IReadOnlyCollection<DeliveryAttempt> DeliveryAttempts => _deliveryAttempts.AsReadOnly();

        public static Notification CreateEmail(ApplicationId applicationId, EmailAddress recipient, TemplateKey templateKey, NotificationPriority priority, IdempotencyKey idempotencyKey, DateTimeOffset createdAtUtc, DateTimeOffset? expiresAtUtc = null)
        {
            ArgumentNullException.ThrowIfNull(recipient);
            ArgumentNullException.ThrowIfNull(templateKey);
            ArgumentNullException.ThrowIfNull(idempotencyKey);

            ValidateExpiration(createdAtUtc, expiresAtUtc);

            return new Notification(NotificationId.New(), applicationId, NotificationChannel.Email, recipient, null, templateKey, priority, idempotencyKey, createdAtUtc, expiresAtUtc);
        }

        public static Notification CreateSms(ApplicationId applicationId, PhoneNumber recipient, TemplateKey templateKey, NotificationPriority priority, IdempotencyKey idempotencyKey, DateTimeOffset createdAtUtc, DateTimeOffset? expiresAtUtc = null)
        {
            ArgumentNullException.ThrowIfNull(recipient);
            ArgumentNullException.ThrowIfNull(templateKey);
            ArgumentNullException.ThrowIfNull(idempotencyKey);

            ValidateExpiration(createdAtUtc, expiresAtUtc);

            return new Notification(NotificationId.New(), applicationId, NotificationChannel.Sms, null, recipient, templateKey, priority, idempotencyKey, createdAtUtc, expiresAtUtc);
        }

        public void Queue(DateTimeOffset queuedAtUtc)
        {
            EnsureNotExpired(queuedAtUtc);

            if (Status is not NotificationStatus.Created and not NotificationStatus.RetryScheduled)
            {
                throw new InvalidOperationException($"Notification cannot be queued from status {Status}.");
            }

            Status = NotificationStatus.Queued;
            QueuedAtUtc = queuedAtUtc;
            NextRetryAtUtc = null;

            RaiseDomainEvent(new NotificationQueuedDomainEvent(Id, queuedAtUtc));
        }

        public DeliveryAttempt StartProcessing(string provider, DateTimeOffset startedAtUtc)
        {
            EnsureNotExpired(startedAtUtc);

            if (Status != NotificationStatus.Queued)
            {
                throw new InvalidOperationException($"Notification cannot start processing from status {Status}.");
            }

            Status = NotificationStatus.Processing;
            ProcessingStartedAtUtc = startedAtUtc;

            var attempt = new DeliveryAttempt(_deliveryAttempts.Count + 1, provider, startedAtUtc);

            _deliveryAttempts.Add(attempt);

            return attempt;
        }

        public void MarkProviderAccepted(DeliveryAttemptId attemptId, string? providerMessageId, DateTimeOffset acceptedAtUtc)
        {
            if (Status != NotificationStatus.Processing)
            {
                throw new InvalidOperationException($"Provider acceptance is not valid from status {Status}.");
            }

            var attempt = GetAttempt(attemptId);

            attempt.CompleteSuccessfully(providerMessageId, acceptedAtUtc);

            Status = NotificationStatus.ProviderAccepted;
            ProviderAcceptedAtUtc = acceptedAtUtc;
        }

        public void MarkDelivered(DateTimeOffset deliveredAtUtc)
        {
            if (Status != NotificationStatus.ProviderAccepted)
            {
                throw new InvalidOperationException($"Notification cannot be delivered from status {Status}.");
            }

            Status = NotificationStatus.Delivered;
            DeliveredAtUtc = deliveredAtUtc;

            RaiseDomainEvent(new NotificationDeliveredDomainEvent(Id, deliveredAtUtc));
        }

        public void RegisterFailure(DeliveryAttemptId attemptId, DeliveryError error, DateTimeOffset failedAtUtc)
        {
            ArgumentNullException.ThrowIfNull(error);

            if (Status != NotificationStatus.Processing)
            {
                throw new InvalidOperationException($"Failure cannot be registered from status {Status}.");
            }

            var attempt = GetAttempt(attemptId);

            attempt.CompleteWithFailure(error, failedAtUtc);

            Status = NotificationStatus.Failed;
            FailedAtUtc = failedAtUtc;

            RaiseDomainEvent(new NotificationFailedDomainEvent(Id, error.Code, error.IsRetryable, failedAtUtc));
        }

        public void ScheduleRetry(DateTimeOffset nextRetryAtUtc)
        {
            if (Status != NotificationStatus.Failed)
            {
                throw new InvalidOperationException($"Retry cannot be scheduled from status {Status}.");
            }

            var lastAttempt = _deliveryAttempts.LastOrDefault() ?? throw new InvalidOperationException("Notification does not have a delivery attempt.");

            if (lastAttempt.Error is null || !lastAttempt.Error.IsRetryable)
            {
                throw new InvalidOperationException("The last failure is not retryable.");
            }

            if (ExpiresAtUtc.HasValue && nextRetryAtUtc >= ExpiresAtUtc.Value)
            {
                throw new InvalidOperationException("Retry cannot be scheduled after notification expiration.");
            }

            Status = NotificationStatus.RetryScheduled;
            NextRetryAtUtc = nextRetryAtUtc;
        }

        public void MarkDeadLettered()
        {
            if (Status is not NotificationStatus.Failed
                and not NotificationStatus.RetryScheduled)
            {
                throw new InvalidOperationException(
                    $"Notification cannot be dead-lettered from status {Status}.");
            }

            Status = NotificationStatus.DeadLettered;
        }

        public void Expire(DateTimeOffset nowUtc)
        {
            if (!ExpiresAtUtc.HasValue || nowUtc < ExpiresAtUtc.Value)
            {
                throw new InvalidOperationException("Notification has not expired.");
            }

            if (Status is NotificationStatus.Delivered or NotificationStatus.DeadLettered)
            {
                throw new InvalidOperationException($"Notification cannot expire from status {Status}.");
            }

            Status = NotificationStatus.Expired;
        }

        private DeliveryAttempt GetAttempt(DeliveryAttemptId attemptId)
        {
            return _deliveryAttempts.SingleOrDefault(x => x.Id == attemptId) ?? throw new InvalidOperationException("Delivery attempt does not belong to this notification.");
        }

        private void ValidateRecipient()
        {
            switch (Channel)
            {
                case NotificationChannel.Email
                    when EmailRecipient is null || PhoneRecipient is not null:
                    throw new InvalidOperationException("Email notification must have only an email recipient.");

                case NotificationChannel.Sms
                    when PhoneRecipient is null || EmailRecipient is not null:
                    throw new InvalidOperationException("SMS notification must have only a phone recipient.");
            }
        }

        private void EnsureNotExpired(DateTimeOffset nowUtc)
        {
            if (ExpiresAtUtc.HasValue && nowUtc >= ExpiresAtUtc.Value)
            {
                Status = NotificationStatus.Expired;

                throw new InvalidOperationException("Notification has expired.");
            }
        }

        private static void ValidateExpiration(DateTimeOffset createdAtUtc, DateTimeOffset? expiresAtUtc)
        {
            if (expiresAtUtc.HasValue && expiresAtUtc.Value <= createdAtUtc)
            {
                throw new ArgumentException("Expiration must be later than creation time.", nameof(expiresAtUtc));
            }
        }
    }
}