using System;
using Arcus.EventGrid.Testing.Infrastructure.Hosts;
using Arcus.EventGrid.Testing.Logging;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Arcus.EventGrid.Tests.Unit.Testing.Fixture;
using Arcus.Testing.Logging;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Bogus;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.EventGrid.Tests.Unit.Testing
{
    public class EventConsumerHostTests
    {
        private readonly ILogger _logger;
        private static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="EventConsumerHostTests" /> class.
        /// </summary>
        public EventConsumerHostTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public void GetReceivedEventByEventIdWithRetryCount_WithAvailableEvent_Succeeds()
        {
            // Arrange
            var eventId = Guid.NewGuid().ToString();
            CloudEvent expected = GenerateCloudEvent(eventId);
            var host = new InMemoryEventConsumerHost(_logger);
            host.ReceiveEvent(expected);

            // Act
            string receivedEvent = host.GetReceivedEvent(eventId);

            // Assert
            CloudEvent actual = CloudEvent.Parse(BinaryData.FromString(receivedEvent));
            Assert.NotNull(actual);
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Source, actual.Source);
            Assert.Equal(expected.Type, actual.Type);
            
            var expectedData = expected.Data.ToObjectFromJson<CarEventData>();
            var actualData = actual.Data.ToObjectFromJson<CarEventData>();
            Assert.Equal(expectedData.LicensePlate, actualData.LicensePlate);
        }

        [Fact]
        public void GetReceivedEventByEventIdWithRetryCount_WithoutAvailableEvent_Fails()
        {
            // Arrange
            var host = new InMemoryEventConsumerHost(_logger);
            var eventId = Guid.NewGuid().ToString();

            // Act / Assert
            Assert.Throws<TimeoutException>(() => host.GetReceivedEvent(eventId, retryCount: 1));
        }

        [Fact]
        public void GetReceivedEventByEventIdWithTimeSpan_WithAvailableEvent_Succeeds()
        {
            // Arrange
            var eventId = Guid.NewGuid().ToString();
            EventGridEvent expected = GenerateEventGridEvent(eventId);
            var host = new InMemoryEventConsumerHost(_logger);
            host.ReceiveEvent(expected);

            // Act
            string receivedEvent = host.GetReceivedEvent(eventId, timeout: TimeSpan.FromMilliseconds(100));

            // Assert
            EventGridEvent actual = EventGridEvent.Parse(BinaryData.FromString(receivedEvent));
            Assert.NotNull(actual);
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Subject, actual.Subject);
            Assert.Equal(expected.EventType, actual.EventType);
            Assert.Equal(expected.DataVersion, actual.DataVersion);

            var expectedData = expected.Data.ToObjectFromJson<CarEventData>();
            var actualData = actual.Data.ToObjectFromJson<CarEventData>();
            Assert.Equal(expectedData.LicensePlate, actualData.LicensePlate);
        }

        [Fact]
        public void GetReceivedEventByEventIdWithTimeSpan_WithoutAvailableEvent_Fails()
        {
            // Arrange
            var host = new InMemoryEventConsumerHost(_logger);
            var eventId = Guid.NewGuid().ToString();

            // Act / Assert
            Assert.Throws<TimeoutException>(() => host.GetReceivedEvent(eventId, timeout: TimeSpan.FromMilliseconds(100)));
        }

        [Fact]
        public void GetReceivedEventByCloudEventFilter_WithAvailableEvent_Succeeds()
        {
            // Arrange
            var eventId = Guid.NewGuid().ToString();
            CloudEvent expected = GenerateCloudEvent(eventId);
            var host = new InMemoryEventConsumerHost(_logger);
            host.ReceiveEvent(expected);

            // Act
            CloudEvent actual = host.GetReceivedEvent(
                (CloudEvent ev) => ev.Id == expected.Id, timeout: TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Source, actual.Source);
            Assert.Equal(expected.Type, actual.Type);
            
            var expectedData = expected.Data.ToObjectFromJson<CarEventData>();
            var actualData = actual.Data.ToObjectFromJson<CarEventData>();
            Assert.Equal(expectedData.LicensePlate, actualData.LicensePlate);
        }

        [Fact]
        public void GetReceivedEventByCloudEventFilter_WithoutAvailableEvent_Fails()
        {
            // Arrange
            var host = new InMemoryEventConsumerHost(_logger);
            var eventId = Guid.NewGuid().ToString();

            // Act / Assert
            Assert.Throws<TimeoutException>(
                () => host.GetReceivedEvent((CloudEvent ev) => ev.Id == eventId, timeout: TimeSpan.FromMilliseconds(100)));
        }

        [Fact]
        public void GetReceivedEventByEventGridEventFilter_WithAvailableEvent_Succeeds()
        {
            // Arrange
            EventGridEvent expected = GenerateEventGridEvent();
            var host = new InMemoryEventConsumerHost(_logger);
            host.ReceiveEvent(expected);

            // Act
            EventGridEvent actual = host.GetReceivedEvent(
                (EventGridEvent ev) => ev.Id == expected.Id, timeout: TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Subject, actual.Subject);
            Assert.Equal(expected.EventType, actual.EventType);
            Assert.Equal(expected.DataVersion, actual.DataVersion);

            var expectedData = expected.Data.ToObjectFromJson<CarEventData>();
            var actualData = actual.Data.ToObjectFromJson<CarEventData>();
            Assert.Equal(expectedData.LicensePlate, actualData.LicensePlate);
        }

        [Fact]
        public void GetReceivedEventByEventGridEventFilter_WithoutAvailableEvent_Fails()
        {
            // Arrange
            var host = new InMemoryEventConsumerHost(_logger);
            var eventId = Guid.NewGuid().ToString();

            // Act / Assert
            Assert.Throws<TimeoutException>(
                () => host.GetReceivedEvent((CloudEvent ev) => ev.Id == eventId, timeout: TimeSpan.FromMilliseconds(100)));
        }

        private static CloudEvent GenerateCloudEvent(string eventId = null)
        {
            return new CloudEvent(
                source: BogusGenerator.Lorem.Word(),
                type: BogusGenerator.Lorem.Word(),
                jsonSerializableData: new CarEventData(BogusGenerator.Vehicle.Vin()))
            {
                Id = eventId ?? Guid.NewGuid().ToString(),
                Time = DateTimeOffset.UtcNow
            };
        }

        private static EventGridEvent GenerateEventGridEvent(string eventId = null)
        {
            return new EventGridEvent(
                subject: BogusGenerator.Lorem.Word(),
                eventType: BogusGenerator.Lorem.Word(),
                dataVersion: BogusGenerator.System.Version().ToString(),
                data: new CarEventData(BogusGenerator.Vehicle.Vin()))
            {
                Id = eventId ?? Guid.NewGuid().ToString(),
                EventTime = DateTimeOffset.UtcNow
            };
        }

        [Theory]
        [InlineData("00:00:00")]
        [InlineData("-00:01:25")]
        [InlineData("-04:21:48")]
        public void GetReceivedEvent_WithNegativeOrZeroTimeRange_FailsWithArgumentOutOfRangeException(string timeout)
        {
            // Arrange
            var consumer = new EventConsumerHost(new ConsoleLogger());

            // Act / Assert
            Assert.Throws<ArgumentOutOfRangeException>(
                () => consumer.GetReceivedEvent(eventId: Guid.NewGuid().ToString(), timeout: TimeSpan.Parse(timeout)));
        }
    }
}
