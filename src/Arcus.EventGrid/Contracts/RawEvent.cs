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
        public RawEvent(string eventId, string eventType, string eventData, string eventSubject, string eventVersion, DateTime eventTime)
            : base(eventId, eventSubject, ParseToken(eventData, "The event body is not a valid JSON payload"), eventVersion, eventType)
        {
            EventTime = eventTime;
        }

        private static JToken ParseToken(string payload, string message)
        {
            try
            {
                return JToken.Parse(payload);
            }
            catch
            {
                throw new ArgumentException(message);
            }
        }
    }
}