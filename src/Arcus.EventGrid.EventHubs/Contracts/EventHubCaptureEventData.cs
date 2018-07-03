using System;

namespace Arcus.EventGrid.EventHubs.Contracts
{
    /// <summary>
    ///     Event data contract for Event Hubs Capture File
    /// </summary>
    public class EventHubCaptureEventData
    {
        public int EventCount { get; set; }
        public string FileType { get; set; }
        public string FileUrl { get; set; }
        public DateTime FirstEnqueueTime { get; set; }
        public int FirstSequenceNumber { get; set; }
        public DateTime LastEnqueueTime { get; set; }
        public int LastSequenceNumber { get; set; }
        public string PartitionId { get; set; }
        public int SizeInBytes { get; set; }
    }
}