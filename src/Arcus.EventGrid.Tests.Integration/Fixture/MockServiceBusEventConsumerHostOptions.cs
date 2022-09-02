using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Azure.Messaging.ServiceBus;

namespace Arcus.EventGrid.Tests.Integration.Fixture
{
    public class MockServiceBusEventConsumerHostOptions : ServiceBusEventConsumerHostOptions
    {
        private readonly ICollection<Action<ProcessMessageEventArgs>> _assertions = new Collection<Action<ProcessMessageEventArgs>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MockServiceBusEventConsumerHostOptions"/> class.
        /// </summary>
        /// <param name="topicPath">The path of the Azure Service Bus topic relative to the Azure Service Bus namespace base address.</param>
        /// <param name="connectionString">The connection string, scoped to the Azure Service Bus namespace to authenticate with the Azure Service Bus topic.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicPath"/> or the <paramref name="connectionString"/> is blank.</exception>
        public MockServiceBusEventConsumerHostOptions(string topicPath, string connectionString) : base(topicPath, connectionString)
        {
        }

        public void AddMessageAssertion(Action<ProcessMessageEventArgs> assertion)
        {
            _assertions.Add(assertion);
        }

        public void AssertMessage(ProcessMessageEventArgs message)
        {
            var exceptions = new Collection<Exception>();
            foreach (Action<ProcessMessageEventArgs> assertion in _assertions)
            {
                try
                {
                    assertion(message);
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
                }
            }

            if (exceptions.Count is 1)
            {
                throw exceptions[0];
            }

            if (exceptions.Count > 1)
            {
                throw new AggregateException("Received message failed to be asserted by all the registered message assertions", exceptions);
            }
        }
    }
}
