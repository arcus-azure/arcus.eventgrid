using System;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Tests.Unit.Artifacts;
using CloudNative.CloudEvents;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Parsing
{
    public class EventParsingTests
    {
        [Fact]
        public void ParseExplicitAsCloudEvent_ValidBlobCreatedEvent_ShouldSucceed()
        {
            // Arrange
            const string topic = "/subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
                         subject = "/blobServices/default/containers/event-container/blobs/finnishjpeg",
                         eventType = "Microsoft.Storage.BlobCreated",
                         id = "5647b67c-b01e-002d-6a47-bc01ac063360",
                         api = "PutBlockList",
                         clientRequestId = "5c24a322-35c9-4b46-8ef5-245a81af7037",
                         requestId = "5647b67c-b01e-002d-6a47-bc01ac000000",
                         eTag = "0x8D58A5F0C6722F9",
                         contentType = "image/jpeg",
                         blobType = "BlockBlob",
                         url = "https://sample.blob.core.windows.net/event-container/finnish.jpeg",
                         sequencer = "00000000000000000000000000000094000000000017d503",
                         batchId = "69cd1576-e430-4aff-8153-570934a1f6e1";

            const int contentLength = 29342;
            var eventTime = DateTimeOffset.Parse("2018-03-15T10:25:17.7535274Z");

            string rawEvent = EventSamples.BlobCreateEvent;
            var eventGridEventBatch = EventParser.Parse(rawEvent);
            Assert.NotNull(eventGridEventBatch);
            Event @event = Assert.Single(eventGridEventBatch.Events);
            Assert.NotNull(@event);

            // Act
            CloudEvent cloudEvent = @event.AsCloudEvent();

            // Assert
            Assert.NotNull(cloudEvent);
            Assert.Equal(topic, @event.Topic);
            Assert.Equal(subject, cloudEvent.Subject);
            Assert.Equal(eventType, cloudEvent.Type);
            Assert.Equal(eventTime, cloudEvent.Time.GetValueOrDefault());
            Assert.Equal(id, cloudEvent.Id);

            var eventPayload = cloudEvent.GetPayload<StorageBlobCreatedEventData>();
            Assert.NotNull(eventPayload);
            Assert.Equal(api, eventPayload.Api);
            Assert.Equal(clientRequestId, eventPayload.ClientRequestId);
            Assert.Equal(requestId, eventPayload.RequestId);
            Assert.Equal(eTag, eventPayload.ETag);
            Assert.Equal(contentType, eventPayload.ContentType);
            Assert.Equal(contentLength, eventPayload.ContentLength);
            Assert.Equal(blobType, eventPayload.BlobType);
            Assert.Equal(url, eventPayload.Url);
            Assert.Equal(sequencer, eventPayload.Sequencer);
            Assert.NotNull(eventPayload.StorageDiagnostics);
            var storageDiagnostics = Assert.IsType<JObject>(eventPayload.StorageDiagnostics);
            Assert.Equal(batchId, storageDiagnostics["batchId"]);
        }

        [Fact]
        public void ParseExplicitAsEventGridEvent_ValidStorageBlobCreatedCloudEvent_ShouldSucceed()
        {
            // Arrange
            const string eventType = "Microsoft.Storage.BlobCreated",
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
            EventGridEventBatch<Event> eventBatch = EventParser.Parse(rawEvent);
            Assert.NotNull(eventBatch);
            Event @event = Assert.Single(eventBatch.Events);
            Assert.NotNull(@event);

            // Act
            EventGridEvent eventGridEvent = @event.AsEventGridEvent();

            // Assert
            Assert.NotNull(eventGridEvent);
            Assert.Equal(eventType, eventGridEvent.EventType);
            Assert.Equal(eventId, eventGridEvent.Id);
            Assert.Equal(eventTime, eventGridEvent.EventTime.ToString("O"));

            var eventPayload = eventGridEvent.GetPayload<StorageBlobCreatedEventData>();
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
        }

        [Fact]
        public void ParseNewtonsoftJson_ValidBlobCreatedEvent_ShouldSucceed()
        {
            // Arrange
            const string topic = "/subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
                         subject = "/blobServices/default/containers/event-container/blobs/finnishjpeg",
                         eventType = "Microsoft.Storage.BlobCreated",
                         id = "5647b67c-b01e-002d-6a47-bc01ac063360",
                         dataVersion = "1",
                         metadataVersion = "1",
                         api = "PutBlockList",
                         clientRequestId = "5c24a322-35c9-4b46-8ef5-245a81af7037",
                         requestId = "5647b67c-b01e-002d-6a47-bc01ac000000",
                         eTag = "0x8D58A5F0C6722F9",
                         contentType = "image/jpeg",
                         blobType = "BlockBlob",
                         url = "https://sample.blob.core.windows.net/event-container/finnish.jpeg",
                         sequencer = "00000000000000000000000000000094000000000017d503",
                         batchId = "69cd1576-e430-4aff-8153-570934a1f6e1";

            const int contentLength = 29342;
            var eventTime = DateTimeOffset.Parse("2018-03-15T10:25:17.7535274Z");

            string rawEvent = EventSamples.BlobCreateEvent.Trim('[', ']');

            // Act
            var eventGridEvent = JsonConvert.DeserializeObject<Event>(rawEvent);

            // Assert
            Assert.Equal(topic, eventGridEvent.Topic);
            Assert.Equal(subject, eventGridEvent.Subject);
            Assert.Equal(eventType, eventGridEvent.EventType);
            Assert.Equal(eventTime, eventGridEvent.EventTime);
            Assert.Equal(id, eventGridEvent.Id);
            Assert.Equal(dataVersion, eventGridEvent.DataVersion);
            Assert.Equal(metadataVersion, eventGridEvent.MetadataVersion);

            var eventPayload = eventGridEvent.GetPayload<StorageBlobCreatedEventData>();
            Assert.NotNull(eventPayload);
            Assert.Equal(api, eventPayload.Api);
            Assert.Equal(clientRequestId, eventPayload.ClientRequestId);
            Assert.Equal(requestId, eventPayload.RequestId);
            Assert.Equal(eTag, eventPayload.ETag);
            Assert.Equal(contentType, eventPayload.ContentType);
            Assert.Equal(contentLength, eventPayload.ContentLength);
            Assert.Equal(blobType, eventPayload.BlobType);
            Assert.Equal(url, eventPayload.Url);
            Assert.Equal(sequencer, eventPayload.Sequencer);
            Assert.NotNull(eventPayload.StorageDiagnostics);
            var storageDiagnostics = Assert.IsType<JObject>(eventPayload.StorageDiagnostics);
            Assert.Equal(batchId, storageDiagnostics["batchId"]);
        }

        [Fact]
        public void ParseNewtonsoftJson_ValidStorageBlobCreatedCloudEvent_ShouldSucceed()
        {
            // Arrange
            const string eventType = "Microsoft.Storage.BlobCreated",
                         eventId = "173d9985-401e-0075-2497-de268c06ff25";

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

            string rawEvent = EventSamples.AzureBlobStorageCreatedCloudEvent.Trim('[', ']');

            // Act
            var cloudEvent = JsonConvert.DeserializeObject<Event>(rawEvent);

            // Assert
            Assert.NotNull(cloudEvent);
            Assert.Equal(eventType, cloudEvent.EventType);
            Assert.Equal(eventId, cloudEvent.Id);

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
        }

       [Fact]
        public void Parse_ValidBlobCreatedEvent_ShouldSucceed()
        {
            // Arrange
            const string topic = "/subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx";
            const string subject = "/blobServices/default/containers/event-container/blobs/finnishjpeg";
            const string eventType = "Microsoft.Storage.BlobCreated";
            const string id = "5647b67c-b01e-002d-6a47-bc01ac063360";
            const string dataVersion = "1";
            const string metadataVersion = "1";
            const string api = "PutBlockList";
            const string clientRequestId = "5c24a322-35c9-4b46-8ef5-245a81af7037";
            const string requestId = "5647b67c-b01e-002d-6a47-bc01ac000000";
            const string eTag = "0x8D58A5F0C6722F9";
            const string contentType = "image/jpeg";
            const int contentLength = 29342;
            const string blobType = "BlockBlob";
            const string url = "https://sample.blob.core.windows.net/event-container/finnish.jpeg";
            const string sequencer = "00000000000000000000000000000094000000000017d503";
            const string batchId = "69cd1576-e430-4aff-8153-570934a1f6e1";
            string rawEvent = EventSamples.BlobCreateEvent;
            var eventTime = DateTimeOffset.Parse("2018-03-15T10:25:17.7535274Z");

            // Act
            var eventGridMessage = EventGridParser.Parse(rawEvent);

            // Assert
            Assert.NotNull(eventGridMessage);
            Assert.NotNull(eventGridMessage.Events);
            EventGridEvent eventGridEvent = Assert.Single(eventGridMessage.Events);
            Assert.Equal(topic, eventGridEvent.Topic);
            Assert.Equal(subject, eventGridEvent.Subject);
            Assert.Equal(eventType, eventGridEvent.EventType);
            Assert.Equal(eventTime, eventGridEvent.EventTime);
            Assert.Equal(id, eventGridEvent.Id);
            Assert.Equal(dataVersion, eventGridEvent.DataVersion);
            Assert.Equal(metadataVersion, eventGridEvent.MetadataVersion);
            Assert.NotNull(eventGridEvent.Data);
            var eventPayload = eventGridEvent.GetPayload<StorageBlobCreatedEventData>();
            Assert.NotNull(eventPayload);
            Assert.Equal(api, eventPayload.Api);
            Assert.Equal(clientRequestId, eventPayload.ClientRequestId);
            Assert.Equal(requestId, eventPayload.RequestId);
            Assert.Equal(eTag, eventPayload.ETag);
            Assert.Equal(contentType, eventPayload.ContentType);
            Assert.Equal(contentLength, eventPayload.ContentLength);
            Assert.Equal(blobType, eventPayload.BlobType);
            Assert.Equal(url, eventPayload.Url);
            Assert.Equal(sequencer, eventPayload.Sequencer);
            Assert.NotNull(eventPayload.StorageDiagnostics);
            var storageDiagnostics = Assert.IsType<JObject>(eventPayload.StorageDiagnostics);
            Assert.Equal(batchId, storageDiagnostics["batchId"]);
        }

        [Fact]
        public void ParseExplicit_ValidBlobCreatedEvent_ShouldSucceed()
        {
            // Arrange
            const string topic = "/subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx";
            const string subject = "/blobServices/default/containers/event-container/blobs/finnishjpeg";
            const string eventType = "Microsoft.Storage.BlobCreated";
            const string id = "5647b67c-b01e-002d-6a47-bc01ac063360";
            const string dataVersion = "1";
            const string metadataVersion = "1";
            const string api = "PutBlockList";
            const string clientRequestId = "5c24a322-35c9-4b46-8ef5-245a81af7037";
            const string requestId = "5647b67c-b01e-002d-6a47-bc01ac000000";
            const string eTag = "0x8D58A5F0C6722F9";
            const string contentType = "image/jpeg";
            const int contentLength = 29342;
            const string blobType = "BlockBlob";
            const string url = "https://sample.blob.core.windows.net/event-container/finnish.jpeg";
            const string sequencer = "00000000000000000000000000000094000000000017d503";
            const string batchId = "69cd1576-e430-4aff-8153-570934a1f6e1";
            string rawEvent = EventSamples.BlobCreateEvent;
            var eventTime = DateTimeOffset.Parse("2018-03-15T10:25:17.7535274Z");

            // Act
            var eventGridMessage = EventParser.Parse(rawEvent);

            // Assert
            Assert.NotNull(eventGridMessage);
            Assert.NotNull(eventGridMessage.Events);
            EventGridEvent eventGridEvent = Assert.Single(eventGridMessage.Events);
            Assert.Equal(topic, eventGridEvent.Topic);
            Assert.Equal(subject, eventGridEvent.Subject);
            Assert.Equal(eventType, eventGridEvent.EventType);
            Assert.Equal(eventTime, eventGridEvent.EventTime);
            Assert.Equal(id, eventGridEvent.Id);
            Assert.Equal(dataVersion, eventGridEvent.DataVersion);
            Assert.Equal(metadataVersion, eventGridEvent.MetadataVersion);
            Assert.NotNull(eventGridEvent.Data);
            var eventPayload = eventGridEvent.GetPayload<StorageBlobCreatedEventData>();
            Assert.NotNull(eventPayload);
            Assert.Equal(api, eventPayload.Api);
            Assert.Equal(clientRequestId, eventPayload.ClientRequestId);
            Assert.Equal(requestId, eventPayload.RequestId);
            Assert.Equal(eTag, eventPayload.ETag);
            Assert.Equal(contentType, eventPayload.ContentType);
            Assert.Equal(contentLength, eventPayload.ContentLength);
            Assert.Equal(blobType, eventPayload.BlobType);
            Assert.Equal(url, eventPayload.Url);
            Assert.Equal(sequencer, eventPayload.Sequencer);
            Assert.NotNull(eventPayload.StorageDiagnostics);
            var storageDiagnostics = Assert.IsType<JObject>(eventPayload.StorageDiagnostics);
            Assert.Equal(batchId, storageDiagnostics["batchId"]);
        }

        [Fact]
        public void ParseImplicit_ValidBlobCreatedEvent_ShouldSucceed()
        {
            // Arrange
            const string topic = "/subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx";
            const string subject = "/blobServices/default/containers/event-container/blobs/finnishjpeg";
            const string eventType = "Microsoft.Storage.BlobCreated";
            const string id = "5647b67c-b01e-002d-6a47-bc01ac063360";
            const string dataVersion = "1";
            const string metadataVersion = "1";
            const string api = "PutBlockList";
            const string clientRequestId = "5c24a322-35c9-4b46-8ef5-245a81af7037";
            const string requestId = "5647b67c-b01e-002d-6a47-bc01ac000000";
            const string eTag = "0x8D58A5F0C6722F9";
            const string contentType = "image/jpeg";
            const int contentLength = 29342;
            const string blobType = "BlockBlob";
            const string url = "https://sample.blob.core.windows.net/event-container/finnish.jpeg";
            const string sequencer = "00000000000000000000000000000094000000000017d503";
            const string batchId = "69cd1576-e430-4aff-8153-570934a1f6e1";
            string rawEvent = EventSamples.BlobCreateEvent;
            var eventTime = DateTimeOffset.Parse("2018-03-15T10:25:17.7535274Z");

            // Act
            EventGridEventBatch<Event> eventGridMessage = EventParser.Parse(rawEvent);

            // Assert
            Assert.NotNull(eventGridMessage);
            Assert.NotNull(eventGridMessage.Events);
            Event eventGridEvent = Assert.Single(eventGridMessage.Events);
            Assert.NotNull(eventGridEvent);
            Assert.Equal(topic, eventGridEvent.Topic);
            Assert.Equal(subject, eventGridEvent.Subject);
            Assert.Equal(eventType, eventGridEvent.EventType);
            Assert.Equal(eventTime, eventGridEvent.EventTime);
            Assert.Equal(id, eventGridEvent.Id);
            Assert.Equal(dataVersion, eventGridEvent.DataVersion);
            Assert.Equal(metadataVersion, eventGridEvent.MetadataVersion);

            var eventPayload = eventGridEvent.GetPayload<StorageBlobCreatedEventData>();
            Assert.NotNull(eventPayload);
            Assert.Equal(api, eventPayload.Api);
            Assert.Equal(clientRequestId, eventPayload.ClientRequestId);
            Assert.Equal(requestId, eventPayload.RequestId);
            Assert.Equal(eTag, eventPayload.ETag);
            Assert.Equal(contentType, eventPayload.ContentType);
            Assert.Equal(contentLength, eventPayload.ContentLength);
            Assert.Equal(blobType, eventPayload.BlobType);
            Assert.Equal(url, eventPayload.Url);
            Assert.Equal(sequencer, eventPayload.Sequencer);
            Assert.NotNull(eventPayload.StorageDiagnostics);
            var storageDiagnostics = Assert.IsType<JObject>(eventPayload.StorageDiagnostics);
            Assert.Equal(batchId, storageDiagnostics["batchId"]);
        }

        [Fact]
        public void Parse_ValidStorageBlobCreatedCloudEvent_ShouldSucceed()
        {
            // Arrange
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
            EventGridEventBatch<Event> eventBatch = EventGridParser.Parse(rawEvent);

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
        }

        [Fact]
        public void ParseEventExplicit_ValidStorageBlobCreatedCloudEvent_ShouldSucceed()
        {
            // Arrange
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
            EventGridEventBatch<Event> eventBatch = EventParser.Parse(rawEvent);

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
        }

        [Fact]
        public void ParseEventImplicit_ValidStorageBlobCreatedCloudEvent_ShouldSucceed()
        {
            // Arrange
            const string eventType = "Microsoft.Storage.BlobCreated",
                         source = "/subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.Storage/storageAccounts/{storage-account}#blobServices/default/containers/{storage-container}/blobs/{new-file}",
                         eventId = "173d9985-401e-0075-2497-de268c06ff25",
                         eventTime = "2018-04-28T02:18:47.1281675+00:00";

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
            EventGridEventBatch<Event> eventBatch = EventParser.Parse(rawEvent);

            // Assert
            Assert.NotNull(eventBatch);
            Assert.NotNull(eventBatch.Events);
            Event cloudEvent = Assert.Single(eventBatch.Events);
            Assert.NotNull(cloudEvent);
            Assert.Equal(eventType, cloudEvent.EventType);
            Assert.Equal(eventId, cloudEvent.Id);
            Assert.Equal(eventTime, cloudEvent.EventTime.ToString("O"));

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
        }

        [Fact]
        public void Parse_WithNullRawJsonEventBody_ShouldThrowArgumentException()
        {
            Assert.Throws<ArgumentException>(() => EventGridParser.Parse(rawJsonBody: null));
        }

        [Fact]
        public void Parse_WithNullSessionId_ShouldThrowArgumentException()
        {
            Assert.Throws<ArgumentException>(() => EventGridParser.Parse(rawJsonBody: "not empty", sessionId: null));
        }

        [Fact]
        public void Parse_WithSessionIdAndNullRawJsonEventBody_ShouldThrowArgumentException()
        {
            Assert.Throws<ArgumentException>(() => EventGridParser.Parse(rawJsonBody: null, sessionId: "not empty"));
        }
    }
}
