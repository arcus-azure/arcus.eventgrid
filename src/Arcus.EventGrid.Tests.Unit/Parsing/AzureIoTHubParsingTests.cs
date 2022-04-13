using System;
using System.Linq;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Tests.Unit.Artifacts;
using Microsoft.Azure.EventGrid.Models;
using Xunit;
#pragma warning disable CS0618 // Ignore deprecated types as we are testing them.

namespace Arcus.EventGrid.Tests.Unit.Parsing
{
    public class AzureIoTHubParsingTests
    {
        [Fact]
        public void Parse_ValidDeviceCreatedEvent_ShouldSucceed2()
        {
            // Arrange
            string rawEvent = EventSamples.IoTDeviceCreateEvent;
            const string id = "38a23a83-f9c2-493f-e6fb-4b57c7c43d28";
            const string topic = "/subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx";
            const string subject = "devices/grid-test-01";
            const string eventType = "Microsoft.Devices.DeviceCreated";
            const string eventTime = "2018-03-16T05:47:28.1359543Z";
            const string stamp = "03/16/2018 05:47:28";
            const string dataVersion = "1";
            const string metadataVersion = "1";
            const string hubName = "savanh-eventgrid-iothub";
            const string deviceId = "grid-test-01";
            const string etag = "AAAAAAAAAAE=";
            const string status = "enabled";
            const string connectionState = "Disconnected";
            const int cloudToDeviceMessageCount = 0;
            const string authenticationType = "sas";
            const string primaryThumbprint = "xyz";
            const string secondaryThumbprint = "abc";
            const int twinVersion = 2;
            const int twinPropertyVersion = 1;

            // Act
            var eventGridMessage = EventGridParser.ParseFromData<IotHubDeviceCreatedEventData>(rawEvent);

            // Assert
            Assert.NotNull(eventGridMessage);
            Assert.NotNull(eventGridMessage.Events);
            Assert.Single(eventGridMessage.Events);
            var eventGridEvent = eventGridMessage.Events.Single();
            Assert.Equal(id, eventGridEvent.Id);
            Assert.Equal(topic, eventGridEvent.Topic);
            Assert.Equal(subject, eventGridEvent.Subject);
            Assert.Equal(eventType, eventGridEvent.EventType);
            Assert.Equal(DateTimeOffset.Parse(eventTime), eventGridEvent.EventTime);
            Assert.Equal(dataVersion, eventGridEvent.DataVersion);
            Assert.Equal(metadataVersion, eventGridEvent.MetadataVersion);
            var eventGridEventData = eventGridEvent.GetPayload();
            Assert.NotNull(eventGridEventData);
            Assert.Equal(hubName, eventGridEventData.HubName);
            Assert.Equal(deviceId, eventGridEventData.DeviceId);
            var twin = eventGridEventData.Twin;
            Assert.NotNull(twin);
            Assert.Equal(deviceId, twin.DeviceId);
            Assert.Equal(etag, twin.Etag);
            Assert.Equal(status, twin.Status);
            Assert.Equal(stamp, twin.StatusUpdateTime);
            Assert.Equal(connectionState, twin.ConnectionState);
            Assert.Equal(stamp, twin.LastActivityTime);
            Assert.Equal(cloudToDeviceMessageCount, twin.CloudToDeviceMessageCount);
            Assert.Equal(authenticationType, twin.AuthenticationType);
            Assert.NotNull(twin.X509Thumbprint);
            Assert.Equal(primaryThumbprint, twin.X509Thumbprint.PrimaryThumbprint);
            Assert.Equal(secondaryThumbprint, twin.X509Thumbprint.SecondaryThumbprint);
            Assert.Equal(twinVersion, twin.Version);
            Assert.NotNull(twin.Properties);
            Assert.NotNull(twin.Properties.Desired);
            Assert.NotNull(twin.Properties.Desired.Metadata);
            Assert.Equal(twinPropertyVersion, twin.Properties.Desired.Version);
            Assert.NotNull(twin.Properties.Desired.Metadata.LastUpdated);
            var rawDesiredLastUpdated = twin.Properties.Desired.Metadata.LastUpdated;
            Assert.NotNull(rawDesiredLastUpdated);
            Assert.Equal(stamp, rawDesiredLastUpdated);
            Assert.NotNull(twin.Properties.Reported);
            Assert.NotNull(twin.Properties.Reported.Metadata);
            Assert.Equal(twinPropertyVersion, twin.Properties.Reported.Version);
            Assert.NotNull(twin.Properties.Reported.Metadata.LastUpdated);
            var rawReportedLastUpdated = twin.Properties.Reported.Metadata.LastUpdated;
            Assert.NotNull(rawReportedLastUpdated);
            Assert.Equal(stamp, rawDesiredLastUpdated);
        }
    }
}