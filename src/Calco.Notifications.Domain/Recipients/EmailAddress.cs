using System.Net.Mail;

namespace Calco.Notifications.Domain.Recipients
{
    public sealed record EmailAddress
    {
        public string Value { get; }
        private EmailAddress(string value)
        {
            Value = value;
        }

        public static EmailAddress Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email address is required.", nameof(value));

            try
            {
                var address = new MailAddress(value.Trim());

                if (!string.Equals(address.Address, value.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Invalid email address.", nameof(value));
                }

                return new EmailAddress(address.Address.ToLowerInvariant());
            }
            catch (FormatException)
            {
                throw new ArgumentException("Invalid email address.", nameof(value));
            }
        }

        public override string ToString() => Value;
    }
}