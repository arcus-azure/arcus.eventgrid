using System;

namespace Arcus.EventGrid
{
    public class Event
    {
        public string Id { get; set; }
        public string Topic { get; set; }
        public string Subject { get; set; }
        public dynamic Data { get; set; }
        public string EventType { get; set; }
        public DateTime EventTime { get; set; }
        public string DataVersion { get; set; }
        public string MetadataVersion { get; set; }
    }

    public class Event<T> : Event
    {
        public new T Data { get; set; }
    }
}