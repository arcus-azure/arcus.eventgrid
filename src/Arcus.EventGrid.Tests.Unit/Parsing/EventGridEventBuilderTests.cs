using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Tests.Core.Events;
using Arcus.EventGrid.Tests.Core.Events.Data;
using Arcus.EventGrid.Tests.Unit.Artifacts;
using CloudNative.CloudEvents;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Parsing
{
    public class EventGridEventBuilderCloudEventTests
    {
        [Fact]
        public void ParseString_ValidStorageBlobCreatedCloudEvents_ShouldSucceed()
        {
            TestParseCloudEvent(rawEvent => EventGridParser.FromRawJson(rawEvent).ToCloudEvent());
        }

        [Fact]
        public void ParseByteArray_ValidStorageBlobCreatedCloudEvents_ShouldSucceed()
        {
            TestParseCloudEvent(rawEvent => EventGridParser.FromRawJson(Encoding.UTF8.GetBytes(rawEvent)).ToCloudEvent());
        }

        [Fact]
        public void ParseStringEvents_ValidStorageBlobCreatedCloudEvent_ShouldSucceed()
        {
            TestParseCloudEvent(rawEvent => EventGridParser.FromRawJson(rawEvent.Trim('[', ']')).ToCloudEvent());
        }

        [Fact]
        public void ParseByteArray_ValidStorageBlobCreatedCloudEvent_ShouldSucceed()
        {
            TestParseCloudEvent(rawEvent => EventGridParser.FromRawJson(Encoding.UTF8.GetBytes(rawEvent.Trim('[', ']'))).ToCloudEvent());
        }

        [Fact]
        public void ParseByteArray_ValidStorageBlobCreatedCloudEventWithSessionId_ShouldSucceed()
        {
            // Arrange
            var expectedSessionId = Guid.NewGuid().ToString();

            // Act
            EventGridEventBatch<Event> eventBatch = 
                TestParseCloudEvent(rawEvent => 
                    EventGridParser.FromRawJson(Encoding.UTF8.GetBytes(rawEvent.Trim('[', ']')))
                                   .WithSessionId(expectedSessionId)
                                   .ToCloudEvent());

            // Assert
            Assert.Equal(expectedSessionId, eventBatch.SessionId);
        }

        private static EventGridEventBatch<Event> TestParseCloudEvent(Func<string, EventGridEventBatch<CloudEvent>> act)
        {
            return TestParseCloudEvent(rawEvent =>
            {
                EventGridEventBatch<CloudEvent> eventGridEventBatch = act(rawEvent);
                return new EventGridEventBatch<Event>(
                    eventGridEventBatch.SessionId, 
                    eventGridEventBatch.Events.Select(ev => new Event(ev)).ToList());
            });
        }

        [Fact]
        public void ParseString_ValidStorageBlobCreatedEvents_ShouldSucceed()
        {
            TestParseCloudEvent(rawEvent => EventGridParser.FromRawJson(rawEvent).ToEvent());
        }

        [Fact]
        public void ParseByteArray_ValidStorageBlobCreatedEvents_ShouldSucceed()
        {
            TestParseCloudEvent(rawEvent => EventGridParser.FromRawJson(Encoding.UTF8.GetBytes(rawEvent)).ToEvent());
        }

        [Fact]
        public void ParseStringEvents_ValidStorageBlobCreatedEvent_ShouldSucceed()
        {
            TestParseCloudEvent(rawEvent => EventGridParser.FromRawJson(rawEvent.Trim('[', ']')).ToEvent());
        }

        [Fact]
        public void ParseByteArray_ValidStorageBlobCreatedEvent_ShouldSucceed()
        {
            TestParseCloudEvent(rawEvent => EventGridParser.FromRawJson(Encoding.UTF8.GetBytes(rawEvent.Trim('[', ']'))).ToEvent());
        }

        [Fact]
        public void ParseStringEvents_ValidStorageBlobCreatedEventWithSessionId_ShouldSucceed()
        {
            // Arrange
            var expectedSessionId = Guid.NewGuid().ToString();

            // Act
            EventGridEventBatch<Event> eventBatch = 
                TestParseCloudEvent(rawEvent => 
                    EventGridParser.FromRawJson(rawEvent)
                                   .WithSessionId(expectedSessionId)
                                   .ToEvent());

            // Assert
            Assert.Equal(expectedSessionId, eventBatch.SessionId);
        }

        private static EventGridEventBatch<Event> TestParseCloudEvent(Func<string, EventGridEventBatch<Event>> act)
        {
            const CloudEventsSpecVersion cloudEventsVersion = CloudEventsSpecVersion.V0_1;
            const string eventType = "Microsoft.Storage.BlobCreated",
                         source = "/subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.Storage/storageAccounts/{storage-account}#blobServices/default/containers/{storage-container}/blobs/{new-file}",
                         eventId = "173d9985-401e-0075-2497-de268c06ff25",
                         eventTime = "2018-04-28T02:18:47.1281675Z";

            const string api = "PutBlockList",
                         clientRequestId = "6d79dbfb-0e37-4fc4-981f-442c9ca65760",
                         requestId = "831e1650-001e-001b-66ab-eeb76e000000",
                         etag = "0x8D4BCC2E4835CD0",
                         contentType = "application/octet-stream",
                         blobType = "BlockBlob",
                         url = "https://oc2d2817345i60006.blob.core.windows.net/oc2d2817345i200097container/oc2d2817345i20002296blob",
                         sequencer = "00000000000004420000000000028963",
                         batchId = "b68529f3-68cd-4744-baa4-3c0498ec19f0";

            const long contentLength = 524_288;

            string rawEvent = EventSamples.AzureBlobStorageCreatedCloudEvent;

            // Act
            EventGridEventBatch<Event> eventBatch = act(rawEvent);

                // Assert
            Assert.NotNull(eventBatch);
            Assert.NotNull(eventBatch.Events);
            CloudEvent cloudEvent = Assert.Single(eventBatch.Events);
            Assert.NotNull(cloudEvent);
            Assert.Equal(cloudEventsVersion, cloudEvent.SpecVersion);
            Assert.Equal(eventType, cloudEvent.Type);
            Assert.Equal(source, cloudEvent.Source.OriginalString);
            Assert.Equal(eventId, cloudEvent.Id);
            Assert.Equal(eventTime, cloudEvent.Time.GetValueOrDefault().ToString("O"));

            var eventPayload = cloudEvent.GetPayload<StorageBlobCreatedEventData>();
            Assert.NotNull(eventPayload);
            Assert.Equal(api, eventPayload.Api);
            Assert.Equal(clientRequestId, eventPayload.ClientRequestId);
            Assert.Equal(requestId, eventPayload.RequestId);
            Assert.Equal(etag, eventPayload.ETag);
            Assert.Equal(contentType, eventPayload.ContentType);
            Assert.Equal(contentLength, eventPayload.ContentLength);
            Assert.Equal(blobType, eventPayload.BlobType);
            Assert.Equal(url, eventPayload.Url);
            Assert.Equal(sequencer, eventPayload.Sequencer);
            Assert.NotNull(eventPayload.StorageDiagnostics);
            var storageDiagnostics = Assert.IsType<JObject>(eventPayload.StorageDiagnostics);
            Assert.Equal(batchId, storageDiagnostics["batchId"]);

            return eventBatch;
        }

        [Fact]
        public void ParseString_ValidSubscriptionValidationEventDataEventGridEvents_ShouldSucceed()
        {
            TestParseEventGridEvent(rawEvent =>
            {
                return EventGridParser.FromRawJson(rawEvent)
                                      .ToEventGridEvent<SubscriptionValidationEventData>();
            });
        }

        [Fact]
        public void ParseByteArray_ValidSubscriptionValidationEventDataEventGridEvents_ShouldSucceed()
        {
            TestParseEventGridEvent(rawEvent =>
            {
                return EventGridParser.FromRawJson(Encoding.UTF8.GetBytes(rawEvent))
                                      .ToEventGridEvent<SubscriptionValidationEventData>();
            });
        }

        [Fact]
        public void ParseString_ValidSubscriptionValidationEventDataEventGridEvent_ShouldSucceed()
        {
            TestParseEventGridEvent(rawEvent =>
            {
                return EventGridParser.FromRawJson(rawEvent.Trim('[', ']'))
                                      .ToEventGridEvent<SubscriptionValidationEventData>();
            });
        }

        [Fact]
        public void ParseByteArray_ValidSubscriptionValidationEventDataEventGridEvent_ShouldSucceed()
        {
            TestParseEventGridEvent(rawEvent =>
            {
                return EventGridParser
                       .FromRawJson(Encoding.UTF8.GetBytes(rawEvent.Trim('[', ']')))
                       .ToEventGridEvent<SubscriptionValidationEventData>();
            });
        }

        [Fact]
        public void ParseStringEvents_ValidSubscriptionValidationEventDataEventGridEventWithSessionId_ShouldSucceed()
        {
            // Arrange
            var expectedSessionId = Guid.NewGuid().ToString();

            // Act
            EventGridEventBatch<EventGridEvent<SubscriptionValidationEventData>> eventBatch = 
                TestParseEventGridEvent(rawEvent => 
                    EventGridParser.FromRawJson(rawEvent)
                                   .WithSessionId(expectedSessionId)
                                   .ToEventGridEvent<SubscriptionValidationEventData>());

            // Assert
            Assert.Equal(expectedSessionId, eventBatch.SessionId);
        }

        private static EventGridEventBatch<EventGridEvent<SubscriptionValidationEventData>> TestParseEventGridEvent(
            Func<string, EventGridEventBatch<EventGridEvent<SubscriptionValidationEventData>>> act)
        {
            // Arrange
            string rawEvent = EventSamples.SubscriptionValidationEvent;
            const string eventId = "2d1781af-3a4c-4d7c-bd0c-e34b19da4e66";
            const string topic = "/subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx";
            const string subject = "Sample.Subject";
            const string eventType = "Microsoft.EventGrid.SubscriptionValidationEvent";
            const string validationCode = "512d38b6-c7b8-40c8-89fe-f46f9e9622b6";
            DateTimeOffset eventTime = DateTimeOffset.Parse("2017-08-06T22:09:30.740323Z");

            EventGridEventBatch<EventGridEvent<SubscriptionValidationEventData>> eventGridBatch = act(rawEvent);

            // Assert
            Assert.NotNull(eventGridBatch);
            Assert.NotNull(eventGridBatch.Events);
            
            EventGridEvent<SubscriptionValidationEventData> eventPayload = Assert.Single(eventGridBatch.Events);
            Assert.NotNull(eventPayload);
            Assert.Equal(eventId, eventPayload.Id);
            Assert.Equal(topic, eventPayload.Topic);
            Assert.Equal(subject, eventPayload.Subject);
            Assert.Equal(eventType, eventPayload.EventType);
            Assert.Equal(eventTime, eventPayload.EventTime);
            Assert.NotNull(eventPayload.Data);
            
            SubscriptionValidationEventData eventData = eventPayload.GetPayload();
            Assert.NotNull(eventData);
            Assert.Equal(validationCode, eventData?.ValidationCode);

            return eventGridBatch;
        }

        [Fact]
        public void ParseString_CustomEvents_ShouldSucceed()
        {
            TestParseCustomEvent(rawEvent => EventGridParser.FromRawJson(rawEvent).ToCustomEvent<NewCarRegistered>());
        }

        [Fact]
        public void ParseByteArray_CustomEvents_ShouldSucceed()
        {
            TestParseCustomEvent(rawEvent => EventGridParser.FromRawJson(Encoding.UTF8.GetBytes(rawEvent)).ToCustomEvent<NewCarRegistered>());
        }

        [Fact]
        public void ParseStringEvents_CustomEvent_ShouldSucceed()
        {
            TestParseCustomEvent(rawEvent => EventGridParser.FromRawJson(rawEvent.Trim('[', ']')).ToCustomEvent<NewCarRegistered>());
        }

        [Fact]
        public void ParseByteArray_CustomEvent_ShouldSucceed()
        {
            TestParseCustomEvent(rawEvent => EventGridParser.FromRawJson(Encoding.UTF8.GetBytes(rawEvent.Trim('[', ']'))).ToCustomEvent<NewCarRegistered>());
        }

        [Fact]
        public void ParseStringEvents_CustomEventWithSessionId_ShouldSucceed()
        {
            // Arrange
            var expectedSessionId = Guid.NewGuid().ToString();

            // Act
            EventGridEventBatch<NewCarRegistered> eventBatch = 
                TestParseCustomEvent(rawEvent => 
                    EventGridParser.FromRawJson(rawEvent)
                                   .WithSessionId(expectedSessionId)
                                   .ToCustomEvent<NewCarRegistered>());

            // Assert
            Assert.Equal(expectedSessionId, eventBatch.SessionId);
        }

        private static EventGridEventBatch<NewCarRegistered> TestParseCustomEvent(Func<string, EventGridEventBatch<NewCarRegistered>> act)
        {
            // Arrange
            const string eventId = "2d1781af-3a4c-4d7c-bd0c-e34b19da4e66";
            const string licensePlate = "1-TOM-337";
            var originalEvent = new NewCarRegistered(eventId, licensePlate);
            string rawEventBody = JsonConvert.SerializeObject(originalEvent.GetPayload());
            var rawEvent = new RawEvent(eventId, originalEvent.EventType, rawEventBody, originalEvent.Subject, originalEvent.DataVersion, originalEvent.EventTime);
            var events = new List<RawEvent> { rawEvent };

            string serializedRawEvents = JsonConvert.SerializeObject(events);

            // Act
            EventGridEventBatch<NewCarRegistered> eventGridMessage = act(serializedRawEvents);

            // Assert
            Assert.NotNull(eventGridMessage);
            Assert.NotEmpty(eventGridMessage.SessionId);
            Assert.NotNull(eventGridMessage.Events);
            
            NewCarRegistered eventPayload = Assert.Single(eventGridMessage.Events);
            Assert.NotNull(eventPayload);
            Assert.Equal(eventId, eventPayload.Id);
            Assert.Equal(originalEvent.Subject, eventPayload.Subject);
            Assert.Equal(originalEvent.EventType, eventPayload.EventType);
            Assert.Equal(originalEvent.EventTime, eventPayload.EventTime);
            Assert.Equal(originalEvent.DataVersion, eventPayload.DataVersion);
            Assert.NotNull(eventPayload.Data);

            CarEventData expectedEventData = originalEvent.GetPayload();
            CarEventData actualEventData = eventPayload.GetPayload();
            
            Assert.NotNull(expectedEventData);
            Assert.NotNull(actualEventData);
            Assert.Equal(expectedEventData, actualEventData);

            return eventGridMessage;
        }
    }
}
