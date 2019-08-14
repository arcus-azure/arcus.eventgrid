using Microsoft.Azure.EventGrid.Models;
using System;

namespace Arcus.EventGrid.EventHubs.Contracts.Events.v1.Data
{
    /// <summary>
    ///     Event data contract for Event Hubs Capture File
    /// </summary>
    [Obsolete("Azure Event Grid events are now being used in favor of specific Arcus event types, use " + nameof(EventHubCaptureFileCreatedEventData) + " instead" )]
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