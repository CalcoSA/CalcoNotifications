using Calco.Notifications.Domain.Recipients;

namespace Calco.Notifications.Domain.Tests.Recipients
{
    public sealed class RecipientValueObjectTests
    {
        [Fact]
        public void EmailAddress_WithValidEmail_ShouldBeCreated()
        {
            var email = EmailAddress.Create("usuario@calco.com");

            Assert.Equal("usuario@calco.com", email.Value);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("correo-invalido")]
        [InlineData("usuario@@calco.com")]
        public void EmailAddress_WithInvalidEmail_ShouldThrowException(string value)
        {
            Assert.Throws<ArgumentException>(() => EmailAddress.Create(value));
        }

        [Fact]
        public void PhoneNumber_WithValidE164_ShouldBeCreated()
        {
            var phone = PhoneNumber.Create("+573001234567");

            Assert.Equal("+573001234567", phone.Value);
        }

        [Theory]
        [InlineData("")]
        [InlineData("3001234567")]
        [InlineData("+57ABC")]
        [InlineData("573001234567")]
        public void PhoneNumber_WithInvalidFormat_ShouldThrowException(string value)
        {
            Assert.Throws<ArgumentException>(() => PhoneNumber.Create(value));
        }
    }
}