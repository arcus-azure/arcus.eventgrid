using System;
using System.Threading.Tasks;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Arcus.Observability.Correlation;
using Azure;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Bogus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Arcus.EventGrid.Tests.Integration.Publishing
{
    [Trait("Category", "Integration")]
    [Collection(TestCollections.Integration)]
    public class EventGridPublisherClientWithTrackingResilienceTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public async Task SendToTopicEndpoint_WithExponentialRetry_TriesSeveralTimes()
        {
            // Arrange
            var topicEndpoint = "http://localhost:5000/some/unused/url/without/event-grid/topic";
            var authenticationKeySecretName = "My-Auth-Key";
            var retryCount = BogusGenerator.Random.Int(min: 1, max: 3);
            var services = new ServiceCollection();
            services.AddSecretStore(stores => stores.AddInMemory(authenticationKeySecretName, "some super secret auth key"));
            services.AddCorrelation();
            services.AddAzureClients(clients =>
            {
                clients.AddEventGridPublisherClient(
                    topicEndpoint,
                    authenticationKeySecretName,
                    options => options.WithExponentialRetry<Exception>(retryCount));
            });

            IServiceProvider provider = services.BuildServiceProvider();
            var correlationAccessor = provider.GetRequiredService<ICorrelationInfoAccessor>();
            correlationAccessor.SetCorrelationInfo(new CorrelationInfo("operation ID", "transaction ID"));

            var factory = provider.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();
            EventGridPublisherClient client = factory.CreateClient("Default");
            CloudEvent cloudEvent = GenerateCloudEvent();
            
            // Act / Assert
            var exception = await Assert.ThrowsAnyAsync<AggregateException>(() => client.SendEventAsync(cloudEvent));
            Assert.Equal(retryCount + 1, exception.InnerExceptions.Count);
            Assert.All(exception.InnerExceptions, ex => Assert.IsType<RequestFailedException>(ex));
        }

        private static CloudEvent GenerateCloudEvent()
        {
            var eventData = new CarEventData("1-ARCUS-337");
            var cloudEvent = new CloudEvent(
                source: BogusGenerator.Internet.UrlWithPath(),
                type: BogusGenerator.Commerce.Product(),
                jsonSerializableData: eventData)
            {
                Id = $"event-{Guid.NewGuid()}",
                Subject = BogusGenerator.Commerce.ProductName()
            };

            return cloudEvent;
        }

        [Fact]
        public async Task SendToTopicEndpoint_WithCircuitBreaker_TriesSeveralTimes()
        {
            // Arrange
            var topicEndpoint = "http://localhost:5000/some/unused/url/without/event-grid/topic";
            var authenticationKeySecretName = "My-Auth-Key";
            var exceptionsAllowedBeforeBreaking = BogusGenerator.Random.Int(min: 1, max: 3);
            var services = new ServiceCollection();
            services.AddSecretStore(stores => stores.AddInMemory(authenticationKeySecretName, "some super secret auth key"));
            services.AddCorrelation();
            services.AddAzureClients(clients =>
            {
                clients.AddEventGridPublisherClient(
                    topicEndpoint,
                    authenticationKeySecretName,
                    options => options.WithCircuitBreaker<Exception>(exceptionsAllowedBeforeBreaking, TimeSpan.FromMilliseconds(100)));
            });

            IServiceProvider provider = services.BuildServiceProvider();
            var correlationAccessor = provider.GetRequiredService<ICorrelationInfoAccessor>();
            correlationAccessor.SetCorrelationInfo(new CorrelationInfo("operation ID", "transaction ID"));

            var factory = provider.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();
            EventGridPublisherClient client = factory.CreateClient("Default");
            EventGridEvent eventGridEvent = GenerateEventGridEvent();
            
            // Act / Assert
            var exception = await Assert.ThrowsAnyAsync<AggregateException>(() => client.SendEventAsync(eventGridEvent));
            Assert.All(exception.InnerExceptions, ex => Assert.IsType<RequestFailedException>(ex));
        }

        private static EventGridEvent GenerateEventGridEvent()
        {
            var eventData = new CarEventData("1-ARCUS-337");
            var eventGridEvent = new EventGridEvent(
                subject: BogusGenerator.Commerce.ProductName(),
                eventType: BogusGenerator.Commerce.Product(),
                dataVersion: BogusGenerator.System.Version().ToString(),
                data: eventData)
            {
                Id = $"event-{Guid.NewGuid()}",
            };

            return eventGridEvent;
        }
    }
}
