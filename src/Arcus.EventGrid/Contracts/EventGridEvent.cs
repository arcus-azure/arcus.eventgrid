using Arcus.EventGrid.Contracts.Interfaces;
using GuardNet;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Arcus.EventGrid.Contracts
{
    /// <summary>
    ///     Represents an event published on a topic.
    ///     (Official schema documentation - https://docs.microsoft.com/en-us/azure/event-grid/event-schema#event-schema)
    /// </summary>
    /// <typeparam name="TData">Type of data payload</typeparam>
    public class EventGridEvent<TData> : EventGridEvent, IEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridEvent{TData}"/> class.
        /// </summary>
        protected EventGridEvent() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridEvent{TData}"/> class.
        /// </summary>
        /// <param name="id">The unique identifier of the event.</param>
        /// <param name="data">The event data specific to the resource provider.</param>
        /// <param name="dataVersion">The schema version of the data object. The publisher defines the schema version.</param>
        /// <param name="eventType">The one of the registered event types for this event source.</param>
        public EventGridEvent(string id, TData data, string dataVersion, string eventType)
            : base(id, subject: null, data: data, eventType: eventType, eventTime: DateTime.UtcNow, dataVersion: dataVersion)
        {
            Validate();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridEvent{TData}"/> class.
        /// </summary>
        /// <param name="id">The unique identifier of the event.</param>
        /// <param name="subject">The publisher-defined path to the event subject.</param>
        /// <param name="data">The event data specific to the resource provider.</param>
        /// <param name="dataVersion">The schema version of the data object. The publisher defines the schema version.</param>
        /// <param name="eventType">The one of the registered event types for this event source.</param>
        public EventGridEvent(string id, string subject, TData data, string dataVersion, string eventType)
            : base(id, subject, data: data, eventType: eventType, eventTime: DateTime.UtcNow, dataVersion: dataVersion, metadataVersion: "1")
        {
            Validate();
        }

        /// <summary>
        ///     Event data specific to the resource provider.
        /// </summary>
        public TData GetPayload()
        {
            if (base.Data is null)
            {
                return default(TData);
            }

            if (base.Data is TData data)
            {
                return data;
            }

            return JObject.Parse(base.Data.ToString()).ToObject<TData>();
        }

        /// <summary>
        ///     The time the event is generated based on the provider's UTC time.
        /// </summary>
        [JsonIgnore]
        [Obsolete("Only implemented for compatibility reasons, use " + nameof(EventGridEvent.EventTime) + " instead.")]
        DateTimeOffset IEvent.EventTime
        {
            get => base.EventTime;
        }
    }
}
