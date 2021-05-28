using System;
using GuardNet;

namespace Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus
{
    /// <summary>
    /// Represents the configuration options for the <see cref="ServiceBusEventConsumerHost" />.
    /// </summary>
    public class ServiceBusEventConsumerHostOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusEventConsumerHostOptions"/> class.
        /// </summary>
        /// <param name="topicPath">The path of the Azure Service Bus topic relative to the Azure Service Bus namespace base address.</param>
        /// <param name="connectionString">The connection string, scoped to the Azure Service Bus namespace to authenticate with the Azure Service Bus topic.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicPath"/> or the <paramref name="connectionString"/> is blank.</exception>
        public ServiceBusEventConsumerHostOptions(string topicPath, string connectionString)
        {
            Guard.NotNullOrWhitespace(topicPath, nameof(topicPath), "Requires a non-blank Azure Service Bus topic name to consume events");
            Guard.NotNullOrWhitespace(connectionString, nameof(connectionString), "Requires a non-blank Azure Service Bus connection string, scoped to the Azure Service Bus namespace to authenticate with the topic");

            TopicPath = topicPath;
            ConnectionString = connectionString;
            SubscriptionName = $"Test-{Guid.NewGuid()}";
        }

        /// <summary>
        /// Gets the Azure Service Bus topic name, relative to the namespace base address.
        /// </summary>
        public string TopicPath { get; }

        /// <summary>
        /// Gets the Azure Service Bus topic subscription name, where the to-be-consumed events will be placed.
        /// </summary>
        public string SubscriptionName { get; }
        
        /// <summary>
        /// Gets the connection string, scoped to the Azure Service Bus namespace to authenticate with the Azure Service Bus topic.
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// Gets the the behavior of the Azure Service Bus topic subscription on closure. (Defaults to <see cref="ServiceBus.SubscriptionBehavior.DeleteOnClosure"/>)
        /// </summary>
        public SubscriptionBehavior SubscriptionBehavior { get; set; } = SubscriptionBehavior.DeleteOnClosure;
    }
}