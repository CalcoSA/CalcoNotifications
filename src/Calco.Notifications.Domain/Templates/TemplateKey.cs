namespace Calco.Notifications.Domain.Templates
{
    public sealed record TemplateKey
    {
        public string Value { get; }
        private TemplateKey(string value)
        {
            Value = value;
        }

        public static TemplateKey Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Template key is required.", nameof(value));

            var normalized = value
                .Trim()
                .ToLowerInvariant();

            return new TemplateKey(normalized);
        }

        public override string ToString() => Value;
    }
}