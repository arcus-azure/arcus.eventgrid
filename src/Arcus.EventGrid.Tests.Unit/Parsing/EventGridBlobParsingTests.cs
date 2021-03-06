﻿using System;
using System.Linq;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Tests.Unit.Artifacts;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Parsing
{
    public class EventGridBlobParsingTests
    {
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
            var eventGridMessage = EventGridParser.ParseFromData<StorageBlobCreatedEventData>(rawEvent);

            // Assert
            Assert.NotNull(eventGridMessage);
            Assert.NotNull(eventGridMessage.Events);
            Assert.Single(eventGridMessage.Events);
            var eventGridEvent = eventGridMessage.Events.Single();
            Assert.Equal(topic, eventGridEvent.Topic);
            Assert.Equal(subject, eventGridEvent.Subject);
            Assert.Equal(eventType, eventGridEvent.EventType);
            Assert.Equal(eventTime, eventGridEvent.EventTime);
            Assert.Equal(id, eventGridEvent.Id);
            Assert.Equal(dataVersion, eventGridEvent.DataVersion);
            Assert.Equal(metadataVersion, eventGridEvent.MetadataVersion);
            Assert.NotNull(eventGridEvent.Data);
            StorageBlobCreatedEventData eventPayload = eventGridEvent.GetPayload();
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
    }
}