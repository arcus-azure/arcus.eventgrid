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
        /// <param name="id">Id of the event</param>
        /// <param name="eventType">Type of the event</param>
        /// <param name="data">Body of the event</param>
        /// <param name="subject">Subject of the event</param>
        /// <param name="dataVersion">Data version of the event body</param>
        /// <param name="eventTime">Time when the event occured</param>
        public RawEvent(string id, string eventType, string data, string subject, string dataVersion, DateTimeOffset eventTime) 
            : this(id, eventType, data, subject, dataVersion, eventTime.DateTime) { }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="id">Id of the event</param>
        /// <param name="eventType">Type of the event</param>
        /// <param name="data">Body of the event</param>
        /// <param name="subject">Subject of the event</param>
        /// <param name="dataVersion">Data version of the event body</param>
        /// <param name="eventTime">Time when the event occured</param>
        public RawEvent(string id, string eventType, string data, string subject, string dataVersion, DateTime eventTime) : base(id, dataVersion, eventType)
        {
            Guard.NotNullOrWhitespace(id, nameof(id), "No event id was specified");
            Guard.NotNullOrWhitespace(eventType, nameof(eventType), "No event type was specified");
            Guard.NotNullOrWhitespace(data, nameof(data), "No event body was specified");
            Guard.NotNullOrWhitespace(dataVersion, nameof(dataVersion), "No data version body was specified");
            Guard.For<ArgumentException>(() => data.IsValidJson() == false, "The event body is not a valid JSON payload");

            var parsedBody = JToken.Parse(data);

            DataVersion = dataVersion;
            Subject = subject;
            EventType = eventType;
            Id = id;
            Data = parsedBody;
            EventTime = eventTime;
        }
    }
}