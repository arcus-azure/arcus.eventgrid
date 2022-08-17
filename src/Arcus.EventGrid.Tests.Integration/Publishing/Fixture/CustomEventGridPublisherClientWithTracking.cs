using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.Security.Core;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.EventGrid.Tests.Integration.Publishing.Fixture
{
    public class CustomEventGridPublisherClientWithTracking : EventGridPublisherClientWithTracking
    {
        public CustomEventGridPublisherClientWithTracking(
            string topicEndpoint, 
            string authenticationKeySecretName, 
            ISecretProvider secretProvider, 
            ICorrelationInfoAccessor correlationAccessor,
            EventGridPublisherClientWithTrackingOptions options,
            ILogger<EventGridPublisherClient> logger) 
            : base(topicEndpoint, authenticationKeySecretName, secretProvider, correlationAccessor, options, logger)
        {
        }

        protected override CloudEvent SetCorrelationPropertyInCloudEvent(CloudEvent cloudEvent, string propertyName, string propertyValue)
        {
            propertyName = DetermineCustomPropertyName(propertyName);
            return base.SetCorrelationPropertyInCloudEvent(cloudEvent, propertyName, propertyValue);
        }

        protected override EventGridEvent SetCorrelationPropertyInEventGridEvent(EventGridEvent eventGridEvent, string propertyName, string propertyValue)
        {
            propertyName = DetermineCustomPropertyName(propertyName);
            return base.SetCorrelationPropertyInEventGridEvent(eventGridEvent, propertyName, propertyValue);
        }

        protected override BinaryData SetCorrelationPropertyInCustomEvent(BinaryData data, string propertyName, string propertyValue)
        {
            propertyName = DetermineCustomPropertyName(propertyName);
            return base.SetCorrelationPropertyInCustomEvent(data, propertyName, propertyValue);
        }

        private static string DetermineCustomPropertyName(string propertyName)
        {
            if (propertyName == "transactionId")
            {
                return "customTransactionId";
            }

            if (propertyName == "operationParentId")
            {
                return "customOperationParentId";
            }

            throw new InvalidOperationException($"Unknown property name: '{propertyName}'");
        }
    }
}
