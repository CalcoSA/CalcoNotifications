namespace Calco.Notifications.Domain.Notifications
{
    public sealed class DeliveryAttempt
    {
        private DeliveryAttempt() { }

        internal DeliveryAttempt(int attemptNumber, string provider, DateTimeOffset startedAtUtc)
        {
            if (attemptNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(attemptNumber));

            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentException("Provider is required.", nameof(provider));

            Id = DeliveryAttemptId.New();
            AttemptNumber = attemptNumber;
            Provider = provider.Trim();
            StartedAtUtc = startedAtUtc;
        }

        public DeliveryAttemptId Id { get; private set; }
        public int AttemptNumber { get; private set; }
        public string Provider { get; private set; } = string.Empty;
        public DateTimeOffset StartedAtUtc { get; private set; }
        public DateTimeOffset? FinishedAtUtc { get; private set; }
        public bool? WasSuccessful { get; private set; }
        public string? ProviderMessageId { get; private set; }
        public DeliveryError? Error { get; private set; }

        internal void CompleteSuccessfully(string? providerMessageId, DateTimeOffset finishedAtUtc)
        {
            if (FinishedAtUtc.HasValue)
                throw new InvalidOperationException("Delivery attempt has already been completed.");

            ProviderMessageId = providerMessageId;
            WasSuccessful = true;
            FinishedAtUtc = finishedAtUtc;
        }

        internal void CompleteWithFailure(DeliveryError error, DateTimeOffset finishedAtUtc)
        {
            if (FinishedAtUtc.HasValue)
                throw new InvalidOperationException("Delivery attempt has already been completed.");

            ArgumentNullException.ThrowIfNull(error);

            Error = error;
            WasSuccessful = false;
            FinishedAtUtc = finishedAtUtc;
        }
    }
}