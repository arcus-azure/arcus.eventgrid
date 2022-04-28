using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.EventGrid.Tests.Core;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Arcus.EventGrid.Tests.Integration.Fixture;
using CloudNative.CloudEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.EventGrid.Tests.Integration.Publishing
{
    // ReSharper disable once InconsistentNaming
    public class IServiceCollectionExtensionsTests : IAsyncLifetime
    {
        private readonly TestConfig _config = TestConfig.Create();
        private readonly ITestOutputHelper _testOutput;
        private EventGridTopicEndpoint _endpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEventPublishing"/> class.
        /// </summary>
        public IServiceCollectionExtensionsTests(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
        }

        /// <summary>
        /// Called immediately after the class has been created, before it is used.
        /// </summary>
        public async Task InitializeAsync()
        {
            _endpoint = await EventGridTopicEndpoint.CreateForCloudEventAsync(_config, _testOutput);
        }

        [Fact]
        public async Task PublishSingleCloudEvent_WithServiceRegistration_Succeeds()
        {
            // Arrange
            const string authenticationKeySecretName = "MyAuthenticationKey", 
                         eventSubject = "integration-test", 
                         licensePlate = "1-ARCUS-337";
            
            var eventId = Guid.NewGuid().ToString();
            var @event = new CloudEvent(CloudEventsSpecVersion.V1_0, "NewCarRegistered", new Uri("http://test-host"), eventSubject, eventId)
            {
                Data = new CarEventData(licensePlate),
                DataContentType = new ContentType("application/json")
            };

            string topicEndpoint = _config.GetEventGridTopicEndpoint(EventSchema.CloudEvent);
            string endpointKey = _config.GetEventGridEndpointKey(EventSchema.CloudEvent);
            var services = new ServiceCollection();
            services.AddSecretStore(stores => stores.AddInMemory(authenticationKeySecretName, endpointKey))
                    .AddEventGridPublishing(topicEndpoint, authenticationKeySecretName);

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var publisher = serviceProvider.GetRequiredService<IEventGridPublisher>();

            // Act
            await publisher.PublishAsync(@event);

            // Assert
            string receivedEvent = _endpoint.ServiceBusEventConsumerHost.GetReceivedEvent(eventId);
            ArcusAssert.ReceivedNewCarRegisteredEvent(eventId, @event.Type, eventSubject, licensePlate, receivedEvent);
        }

        [Fact]
        public async Task PublishSingleCloudEvent_WithExponentialRetryServiceRegistration_Succeeds()
        {
            // Arrange
            const string authenticationKeySecretName = "MyAuthenticationKey",
                eventSubject = "integration-test",
                licensePlate = "1-ARCUS-337";

            var eventId = Guid.NewGuid().ToString();
            var @event = new CloudEvent(CloudEventsSpecVersion.V1_0, "NewCarRegistered", new Uri("http://test-host"), eventSubject, eventId)
            {
                Data = new CarEventData(licensePlate),
                DataContentType = new ContentType("application/json")
            };

            string authenticationKey = _config.GetEventGridEndpointKey(EventSchema.CloudEvent);
            var services = new ServiceCollection();
            services.AddSecretStore(stores => stores.AddInMemory(authenticationKeySecretName, authenticationKey))
                    .AddEventGridPublishing("https://invalid-topic-endpoint", authenticationKeySecretName)
                    .WithExponentialRetry<HttpRequestException>(retryCount: 2);

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var publisher = serviceProvider.GetRequiredService<IEventGridPublisher>();

            // Act / Assert
            var stopWatch = Stopwatch.StartNew();
            await Assert.ThrowsAnyAsync<HttpRequestException>(
                () => publisher.PublishAsync(@event));

            Assert.True(TimeSpan.FromSeconds(5) < stopWatch.Elapsed, $"5s < stop watch: {stopWatch.Elapsed}");
        }

        [Fact]
        public async Task PublishSingleCloudEvent_WithCircuitBreakerServiceRegistration_Succeeds()
        {
            // Arrange
            const string authenticationKeySecretName = "MyAuthenticationKey",
                eventSubject = "integration-test",
                licensePlate = "1-ARCUS-337";

            var eventId = Guid.NewGuid().ToString();
            var @event = new CloudEvent(CloudEventsSpecVersion.V1_0, "NewCarRegistered", new Uri("http://test-host"), eventSubject, eventId)
            {
                Data = new CarEventData(licensePlate),
                DataContentType = new ContentType("application/json")
            };

            string authenticationKey = _config.GetEventGridEndpointKey(EventSchema.CloudEvent);
            var services = new ServiceCollection();
            services.AddSecretStore(stores => stores.AddInMemory(authenticationKeySecretName, authenticationKey))
                    .AddEventGridPublishing("https://invalid-topic-endpoint", authenticationKeySecretName)
                    .WithCircuitBreaker<HttpRequestException>(exceptionsAllowedBeforeBreaking: 1, durationOfBreak: TimeSpan.FromSeconds(5));

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var publisher = serviceProvider.GetRequiredService<IEventGridPublisher>();

            // Act / Assert
            var stopWatch = Stopwatch.StartNew();
            await Assert.ThrowsAnyAsync<HttpRequestException>(
                () => publisher.PublishAsync(@event));

            Assert.True(TimeSpan.FromSeconds(5) < stopWatch.Elapsed, $"5s < stop watch: {stopWatch.Elapsed}");
        }

        /// <summary>
        /// Called when an object is no longer needed. Called just before <see cref="M:System.IDisposable.Dispose" />
        /// if the class also implements that.
        /// </summary>
        public async Task DisposeAsync()
        {
            if (_endpoint != null)
            {
                await _endpoint.StopAsync();
            }
        }
    }
}
