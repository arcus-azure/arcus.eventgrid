using System;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Tests.Unit.Artifacts;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Parsing
{
    public class CloudEventParsingTests
    {
        [Fact]
        public void Parse_ValidStorageBlobCreatedCloudEvent_ShouldSucceed()
        {
            // Arrange
            const string cloudEventsVersion = "0.1",
                         eventType = "Microsoft.Storage.BlobCreated",
                         eventTypeVersion = "v1",
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
            EventGridEventBatch<CloudEvent<StorageBlobCreatedEventData>> eventBatch = EventGridParser.Parse<CloudEvent<StorageBlobCreatedEventData>>(rawEvent);

            // Assert
            Assert.NotNull(eventBatch);
            Assert.NotNull(eventBatch.Events);
            CloudEvent<StorageBlobCreatedEventData> cloudEvent = Assert.Single(eventBatch.Events);
            Assert.NotNull(cloudEvent);
            Assert.Equal(cloudEventsVersion, cloudEvent.CloudEventsVersion);
            Assert.Equal(eventType, cloudEvent.EventType);
            Assert.Equal(eventTypeVersion, cloudEvent.EventTypeVersion);
            Assert.Equal(source, cloudEvent.Source);
            Assert.Equal(source, cloudEvent.Topic + "#" + cloudEvent.Subject);
            Assert.Equal(eventId, cloudEvent.Id);
            Assert.Equal(DateTimeOffset.Parse(eventTime), cloudEvent.EventTime);

            StorageBlobCreatedEventData eventPayload = cloudEvent.GetPayload();
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
    }
}
