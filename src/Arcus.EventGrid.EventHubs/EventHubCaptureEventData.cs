using System;

namespace Arcus.EventGrid.EventHubs
{

    public class EventHubCaptureEventData
    {
        public string FileUrl { get; set; }
        public string FileType { get; set; }
        public string PartitionId { get; set; }
        public int SizeInBytes { get; set; }
        public int EventCount { get; set; }
        public int FirstSequenceNumber { get; set; }
        public int LastSequenceNumber { get; set; }
        public DateTime FirstEnqueueTime { get; set; }
        public DateTime LastEnqueueTime { get; set; }
    }

}
