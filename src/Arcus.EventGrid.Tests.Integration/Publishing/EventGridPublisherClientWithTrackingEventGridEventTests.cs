using System;
using System.Threading.Tasks;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Azure;
using Azure.Messaging.EventGrid;
using Xunit;
using Arcus.EventGrid.Tests.Core;
using Xunit.Abstractions;
using Arcus.EventGrid.Tests.Integration.Fixture;
using System.Collections.Generic;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Core;
using SendEventGridEventAsync = System.Func<Azure.Messaging.EventGrid.EventGridPublisherClient, Azure.Messaging.EventGrid.EventGridEvent, System.Threading.Tasks.Task<Azure.Response>>;

namespace Arcus.EventGrid.Tests.Integration.Publishing
{
    [Trait("Category", "Integration")]
    [Collection(TestCollections.Integration)]
    public class EventGridPublisherClientWithTrackingEventGridEventTests : EventGridPublisherClientWithTrackingTests
    {
        public EventGridPublisherClientWithTrackingEventGridEventTests(ITestOutputHelper testOutput) 
            : base(EventSchema.EventGrid, testOutput)
        {
        }

        public static IEnumerable<object[]> SendEventGridEventOverloads = new[]
        {
            new object[] { new SendEventGridEventAsync((client, eventGridEvent) => client.SendEventAsync(eventGridEvent)) },
            new object[] { new SendEventGridEventAsync((client, eventGridEvent) => Task.FromResult(client.SendEvent(eventGridEvent))) },
            new object[] { new SendEventGridEventAsync((client, eventGridEvent) => client.SendEventsAsync(new [] { eventGridEvent })) },
            new object[] { new SendEventGridEventAsync((client, eventGridEvent) => Task.FromResult(client.SendEvents(new [] { eventGridEvent }))) },
            new object[] { new SendEventGridEventAsync(async (client, eventGridEvent) =>
            {
                BinaryData data = BinaryData.FromObjectAsJson(eventGridEvent);
                return await client.SendEventsAsync(new[] { data });
            }) }, 
            new object[] { new SendEventGridEventAsync((client, eventGridEvent) =>
            {
                BinaryData data = BinaryData.FromObjectAsJson(eventGridEvent);
                return Task.FromResult(client.SendEvents(new[] { data }));
            }) }
        };

        private async Task TestWithEventConsumerHostWithTrackingAsync(Func<EventCorrelationFormat, EventGridTopicEndpoint, Task> testBody)
        {
            foreach (var format in new[] { EventCorrelationFormat.Hierarchical, EventCorrelationFormat.W3C })
            {
                await using (EventGridTopicEndpoint endpoint = await CreateEventConsumerHostWithTrackingAsync(format))
                {
                    await testBody(format, endpoint);
                }
            }
        }

        [Theory]
        [MemberData(nameof(SendEventGridEventOverloads))]
        public async Task SendEventGridEventAsync_WithoutOptions_Succeeds(SendEventGridEventAsync usePublisherAsync)
        {
            await TestWithEventConsumerHostWithTrackingAsync(async (format, endpoint) =>
            {
                EventGridPublisherClient client = CreateRegisteredClient(format);
                await TestSendEventGridEventAsync(client, usePublisherAsync, endpoint);
            });
        }

        [Theory]
        [MemberData(nameof(SendEventGridEventOverloads))]
        public async Task SendEventGridEventAsync_WithOptions_Succeeds(SendEventGridEventAsync usePublisherAsync)
        {
            await TestWithEventConsumerHostWithTrackingAsync(async (format, endpoint) =>
            {
                EventGridPublisherClient client = CreateRegisteredClientWithCustomOptions(format);
                await TestSendEventGridEventAsync(client, usePublisherAsync, endpoint);
            });
        }

        [Theory]
        [MemberData(nameof(SendEventGridEventOverloads))]
        public async Task SendEventGridEventAsync_WithCustomImplementation_Succeeds(SendEventGridEventAsync usePublisherAsync)
        {
            await TestWithEventConsumerHostWithTrackingAsync(async (format, endpoint) =>
            {
                EventGridPublisherClient client = CreateRegisteredClientWithCustomImplementation(format);
                await TestSendEventGridEventAsync(client, usePublisherAsync, endpoint);
            });
        }

        private async Task TestSendEventGridEventAsync(
            EventGridPublisherClient client, 
            SendEventGridEventAsync usePublisherAsync, 
            EventGridTopicEndpoint endpoint)
        {
            // Arrange
            EventGridEvent eventGridEvent = CreateEventGridEvent();

            // Act
            using (Response response = await usePublisherAsync(client, eventGridEvent))
            {
                Assert.False(response.IsError, response.ReasonPhrase);
            }

            // Assert
            AssertDependencyTracking();
            AssertEventGridEventForData(eventGridEvent, endpoint);
        }

        private static EventGridEvent CreateEventGridEvent()
        {
            var eventGridEvent = new EventGridEvent(
                subject: BogusGenerator.Commerce.ProductName(),
                eventType: BogusGenerator.Commerce.Product(),
                dataVersion: BogusGenerator.System.Version().ToString(),
                data: new CarEventData("1-ARCUS-337"))
            {
                Id = $"event-{Guid.NewGuid()}",
            };

            return eventGridEvent;
        }

        private static void AssertEventGridEventForData(EventGridEvent expected, EventGridTopicEndpoint endpoint)
        {
            Assert.NotNull(expected.Data);
            string actual = endpoint.ConsumerHost.GetReceivedEventOrFail(expected.Id);
            ArcusAssert.ReceivedNewCarRegisteredEvent(expected, actual);
        }
    }
}
