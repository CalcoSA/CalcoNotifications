namespace Calco.Notifications.Domain.Applications
{
    public readonly record struct ClientApplicationId(Guid Value)
    {
        public static ClientApplicationId New() => new(Guid.NewGuid());
        public override string ToString() => Value.ToString();
    }
}