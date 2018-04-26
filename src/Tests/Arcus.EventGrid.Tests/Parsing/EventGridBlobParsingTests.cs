using Arcus.EventGrid.Storage;
using Xunit;

namespace Arcus.EventGrid.Tests.Parsing
{
    public class EventGridBlobParsingTests
    {
        [Fact]
        public void TestBlobCreateEvent()
        {
            var eventGridMessage = EventGridMessage<BlobEventData>.Parse(TestArtifacts.BlobCreateEvent);
            Assert.NotNull(eventGridMessage);
            Assert.True(eventGridMessage.Events.Count > 0);
            Assert.NotNull(eventGridMessage.Events[0].Data.Api);
        }

        //[Fact]
        //public void TestBlobGetFilename()
        //{
        //    var eventGridMessage = EventGridMessage<BlobEventData>.Parse(TestArtifacts.BlobCreateEvent);
        //    Assert.NotNull(eventGridMessage);
        //    Assert.True(eventGridMessage.Events.Count > 0);
        //    Assert.Equal("finnish.jpeg", eventGridMessage.Events[0].GetFileName());
        //}

        //[Fact]
        //public void TestBlobGetExtension()
        //{
        //    var eventGridMessage = EventGridMessage<BlobEventData>.Parse(TestArtifacts.BlobCreateEvent);
        //    Assert.NotNull(eventGridMessage);
        //    Assert.True(eventGridMessage.Events.Count > 0);
        //    Assert.Equal("jpeg", eventGridMessage.Events[0].GetExtension());
        //}

        //[Fact]
        //public void TestBlobGetWithoutExtension()
        //{
        //    var eventGridMessage = EventGridMessage<BlobEventData>.Parse(TestArtifacts.BlobCreateEvent);
        //    Assert.NotNull(eventGridMessage);
        //    Assert.True(eventGridMessage.Events.Count > 0);
        //    Assert.Equal("finnish", eventGridMessage.Events[0].GetFileNameWithoutExtension());
        //}

    }
}