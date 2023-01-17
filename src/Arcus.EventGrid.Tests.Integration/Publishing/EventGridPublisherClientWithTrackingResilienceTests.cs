using System;
using System.Threading.Tasks;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Arcus.EventGrid.Tests.Integration.Publishing.Fixture;
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
            await using (var endpoint = await SabotageMockTopicEndpoint.StartAsync())
            {
                var authenticationKeySecretName = "My-Auth-Key";
                var retryCount = BogusGenerator.Random.Int(min: 1, max: 3);
                var services = new ServiceCollection();
                services.AddSecretStore(stores => stores.AddInMemory(authenticationKeySecretName, "some super secret auth key"));
                services.AddCorrelation();
                services.AddAzureClients(clients =>
                {
                    clients.AddEventGridPublisherClient(
                        endpoint.HostingUrl,
                        authenticationKeySecretName,
                        options =>
                        {
                            options.Retry.MaxRetries = 0;
                            options.WithExponentialRetry<RequestFailedException>(retryCount);
                        });
                });

                IServiceProvider provider = services.BuildServiceProvider();
                var correlationAccessor = provider.GetRequiredService<ICorrelationInfoAccessor>();
                correlationAccessor.SetCorrelationInfo(new CorrelationInfo("operation ID", "transaction ID"));

                var factory = provider.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();
                EventGridPublisherClient client = factory.CreateClient("Default");
                CloudEvent cloudEvent = GenerateCloudEvent();

                // Act / Assert
                await Assert.ThrowsAnyAsync<RequestFailedException>(() => client.SendEventAsync(cloudEvent));
                Assert.Equal(retryCount + 1, endpoint.EndpointCallCount);
            }
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
            await using (var endpoint = await SabotageMockTopicEndpoint.StartAsync())
            {
                var authenticationKeySecretName = "My-Auth-Key";
                var exceptionsAllowedBeforeBreaking = BogusGenerator.Random.Int(min: 1, max: 3);
                var services = new ServiceCollection();
                services.AddSecretStore(stores => stores.AddInMemory(authenticationKeySecretName, "some super secret auth key"));
                services.AddCorrelation();
                services.AddAzureClients(clients =>
                {
                    clients.AddEventGridPublisherClient(
                        endpoint.HostingUrl,
                        authenticationKeySecretName,
                        options => options.WithCircuitBreaker<RequestFailedException>(exceptionsAllowedBeforeBreaking, TimeSpan.FromMilliseconds(100)));
                });

                IServiceProvider provider = services.BuildServiceProvider();
                var correlationAccessor = provider.GetRequiredService<ICorrelationInfoAccessor>();
                correlationAccessor.SetCorrelationInfo(new CorrelationInfo("operation ID", "transaction ID"));

                var factory = provider.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();
                EventGridPublisherClient client = factory.CreateClient("Default");
                EventGridEvent eventGridEvent = GenerateEventGridEvent();
            
                // Act / Assert
                await Assert.ThrowsAnyAsync<RequestFailedException>(() => client.SendEventAsync(eventGridEvent));
                Assert.True(endpoint.EndpointCallCount > 2);
            }
        }

        [Fact]
        public async Task SendToTopicEndpoint_WithExponentialRetryWithCircuitBreaker_TriesSeveralTimes()
        {
            // Arrange
            await using (var endpoint = await SabotageMockTopicEndpoint.StartAsync())
            {
                var authenticationKeySecretName = "My-Auth-Key";
                var retryCount = BogusGenerator.Random.Int(min: 1, max: 3);
                var exceptionsAllowedBeforeBreaking = BogusGenerator.Random.Int(min: 1, max: 3);
                var services = new ServiceCollection();
                services.AddSecretStore(stores => stores.AddInMemory(authenticationKeySecretName, "some super secret auth key"));
                services.AddCorrelation();
                services.AddAzureClients(clients =>
                {
                    clients.AddEventGridPublisherClient(
                        endpoint.HostingUrl,
                        authenticationKeySecretName,
                        options =>
                        {
                            options.Retry.MaxRetries = 0;
                            options.WithExponentialRetry<RequestFailedException>(retryCount);
                            options.WithCircuitBreaker<RequestFailedException>(exceptionsAllowedBeforeBreaking, TimeSpan.FromMilliseconds(100));
                        });
                });

                IServiceProvider provider = services.BuildServiceProvider();
                var correlationAccessor = provider.GetRequiredService<ICorrelationInfoAccessor>();
                correlationAccessor.SetCorrelationInfo(new CorrelationInfo("operation ID", "transaction ID"));

                var factory = provider.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();
                EventGridPublisherClient client = factory.CreateClient("Default");
                CloudEvent cloudEvent = GenerateCloudEvent();

                // Act / Assert
                await Assert.ThrowsAnyAsync<RequestFailedException>(() => client.SendEventAsync(cloudEvent));
                Assert.Equal(retryCount + 1, endpoint.EndpointCallCount);
            }
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
