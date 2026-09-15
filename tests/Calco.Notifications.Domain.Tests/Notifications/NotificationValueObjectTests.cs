using Calco.Notifications.Domain.Notifications;
using Calco.Notifications.Domain.Templates;

namespace Calco.Notifications.Domain.Tests.Notifications
{
    public sealed class NotificationValueObjectTests
    {
        [Fact]
        public void TemplateKey_ShouldNormalizeValue()
        {
            var template = TemplateKey.Create("  INVENTORY-CLOSED  ");

            Assert.Equal("inventory-closed", template.Value);
        }

        [Fact]
        public void TemplateKey_WhenEmpty_ShouldThrowException()
        {
            Assert.Throws<ArgumentException>(() => TemplateKey.Create(""));
        }

        [Fact]
        public void IdempotencyKey_WithValidValue_ShouldBeCreated()
        {
            var key = IdempotencyKey.Create("SITRA-INVENTORY-1289-CLOSED");

            Assert.Equal("SITRA-INVENTORY-1289-CLOSED", key.Value);
        }

        [Fact]
        public void IdempotencyKey_WhenTooLong_ShouldThrowException()
        {
            var value = new string('A', 151);

            Assert.Throws<ArgumentException>(() => IdempotencyKey.Create(value));
        }
    }
}