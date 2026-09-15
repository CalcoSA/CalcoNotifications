namespace Calco.Notifications.Domain.Notifications
{
    public sealed record IdempotencyKey
    {
        public string Value { get; }
        private IdempotencyKey(string value)
        {
            Value = value;
        }

        public static IdempotencyKey Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Idempotency key is required.", nameof(value));

            var normalized = value.Trim();

            if (normalized.Length > 150)
                throw new ArgumentException("Idempotency key cannot exceed 150 characters.", nameof(value));

            return new IdempotencyKey(normalized);
        }

        public override string ToString() => Value;
    }
}