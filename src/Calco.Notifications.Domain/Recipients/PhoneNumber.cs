using System.Text.RegularExpressions;

namespace Calco.Notifications.Domain.Recipients
{
    public sealed record PhoneNumber
    {
        private static readonly Regex E164Regex = new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled);
        public string Value { get; }
        private PhoneNumber(string value)
        {
            Value = value;
        }

        public static PhoneNumber Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Phone number is required.", nameof(value));

            var normalized = value.Trim();

            if (!E164Regex.IsMatch(normalized))
                throw new ArgumentException("Phone number must use E.164 format.", nameof(value));

            return new PhoneNumber(normalized);
        }

        public override string ToString() => Value;
    }
}