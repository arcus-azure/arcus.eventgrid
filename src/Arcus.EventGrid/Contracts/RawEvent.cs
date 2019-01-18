using System;
using GuardNet;
using Newtonsoft.Json.Linq;

namespace Arcus.EventGrid.Contracts
{
    public class RawEvent : Event<object>
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="id">Id of the event</param>
        /// <param name="type">Type of the event</param>
        /// <param name="body">Body of the event</param>
        /// <param name="subject">Subject of the event</param>
        /// <param name="dataVersion">Data version of the event body</param>
        /// <param name="eventTime">Time when the event occured</param>
        public RawEvent(string id, string type, string body, string subject, string dataVersion, DateTimeOffset eventTime)
        {
            Guard.NotNullOrWhitespace(id, nameof(id), "No event id was specified");
            Guard.NotNullOrWhitespace(type, nameof(type), "No event type was specified");
            Guard.NotNullOrWhitespace(body, nameof(body), "No event body was specified");
            Guard.NotNullOrWhitespace(dataVersion, nameof(dataVersion), "No data version body was specified");
            Guard.For<ArgumentException>(() => body.IsValidJson() == false, "The event body is not a valid JSON payload");

            var parsedBody = JToken.Parse(body);

            DataVersion = dataVersion;
            Subject = subject;
            EventType = type;
            Id = id;
            Data = parsedBody;
            EventTime = eventTime;
        }

        public RawEvent()
        {
        }

        public override string DataVersion { get; }
        public override string EventType { get; }
    }
}