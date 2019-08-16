using System;
using GuardNet;
using Newtonsoft.Json.Linq;

namespace Arcus.EventGrid.Contracts
{
    /// <summary>
    /// Representation of an event with raw JSON content.
    /// </summary>
    public class RawEvent : EventGridEvent<object>
    {
        private RawEvent() { }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="eventId">Id of the event</param>
        /// <param name="eventType">Type of the event</param>
        /// <param name="eventData">Body of the event</param>
        /// <param name="eventSubject">Subject of the event</param>
        /// <param name="eventVersion">Data version of the event body</param>
        /// <param name="eventTime">Time when the event occured</param>
        public RawEvent(string eventId, string eventType, string eventData, string eventSubject, string eventVersion, DateTimeOffset eventTime) 
            : this(eventId, eventType, eventData, eventSubject, eventVersion, eventTime.DateTime) { }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="eventId">Id of the event</param>
        /// <param name="eventType">Type of the event</param>
        /// <param name="eventData">Body of the event</param>
        /// <param name="eventSubject">Subject of the event</param>
        /// <param name="eventVersion">Data version of the event body</param>
        /// <param name="eventTime">Time when the event occured</param>
        public RawEvent(string eventId, string eventType, string eventData, string eventSubject, string eventVersion, DateTime eventTime) : base(eventId, eventVersion, eventType)
        {
            Guard.NotNullOrWhitespace(eventId, nameof(eventId), "No event id was specified");
            Guard.NotNullOrWhitespace(eventType, nameof(eventType), "No event type was specified");
            Guard.NotNullOrWhitespace(eventData, nameof(eventData), "No event body was specified");
            Guard.NotNullOrWhitespace(eventVersion, nameof(eventVersion), "No data version body was specified");
            Guard.For<ArgumentException>(() => eventData.IsValidJson() == false, "The event body is not a valid JSON payload");

            var parsedBody = JToken.Parse(eventData);

            DataVersion = eventVersion;
            Subject = eventSubject;
            EventType = eventType;
            Id = eventId;
            Data = parsedBody;
            EventTime = eventTime;
        }
    }
}