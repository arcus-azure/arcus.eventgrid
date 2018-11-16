using GuardNet;

namespace Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus
{
    /// <summary>
    ///     Configuration options for the <see cref="ServiceBusEventConsumerHost" />
    /// </summary>
    public class ServiceBusEventConsumerHostOptions
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="topicPath">Path of the topic relative to the namespace base address</param>
        /// <param name="connectionString">
        ///     Connection string of the Azure Service Bus namespace to use to consume
        ///     messages
        /// </param>
        public ServiceBusEventConsumerHostOptions(string topicPath, string connectionString)
        {
            Guard.NotNullOrWhitespace(topicPath, nameof(topicPath));
            Guard.NotNullOrWhitespace(connectionString, nameof(connectionString));

            TopicPath = topicPath;
            ConnectionString = connectionString;
        }

        /// <summary>
        ///     Path of the topic relative to the namespace base address
        /// </summary>
        public string TopicPath { get; }

        /// <summary>
        ///     Connection string of the Azure Service Bus namespace to use to consume
        ///     messages
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        ///     Defines the behavior of the subscription on closure. (Defaults to delete)
        /// </summary>
        public SubscriptionBehavior SubscriptionBehavior { get; set; } = SubscriptionBehavior.DeleteOnClosure;
    }
}