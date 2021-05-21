namespace Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus
{
    /// <summary>
    /// Represents the available options in behavior when Azure Service Bus topic subscriptions are managed during the lifetime of the <see cref="ServiceBusEventConsumerHost"/>.
    /// </summary>
    public enum SubscriptionBehavior
    {
        /// <summary>
        /// Keeps all the Azure Service Bus topic subscriptions that were created during the lifetime of the <see cref="ServiceBusEventConsumerHost"/>
        /// when the consumer host stops receiving traffic.
        /// </summary>
        KeepOnClosure,
        
        /// <summary>
        /// Deletes all the Azure Service Bus topic subscriptions that were created during the lifetime of the <see cref="ServiceBusEventConsumerHost"/>
        /// when the consumer host stops receiving traffic.
        /// </summary>
        DeleteOnClosure
    }
}