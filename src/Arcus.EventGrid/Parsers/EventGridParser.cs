using System;
using System.Collections.Generic;
using Arcus.EventGrid.Contracts.Interfaces;
using GuardNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arcus.EventGrid.Parsers
{
    public class EventGridParser
    {
        /// <summary>
        ///     Parses a string to a EventGridMessage with typed data payload
        /// </summary>
        /// <param name="rawJsonBody">Raw JSON body</param>
        /// <returns>Typed EventGridMessage</returns>
        public static EventGridMessage<TEvent> Parse<TEvent>(string rawJsonBody)
            where TEvent : IEvent, new()
        {
            Guard.NotNullOrWhitespace(rawJsonBody, nameof(rawJsonBody));

            var sessionId = Guid.NewGuid().ToString();

            var eventGridMessage = Parse<TEvent>(rawJsonBody, sessionId);
            return eventGridMessage;
        }

        /// <summary>
        ///     Parses a string to a EventGridMessage with typed data payload
        /// </summary>
        /// <param name="rawJsonBody">Raw JSON body</param>
        /// <param name="sessionId">Session id for event grid message</param>
        /// <returns>Typed EventGridMessage</returns>
        public static EventGridMessage<TEvent> Parse<TEvent>(string rawJsonBody, string sessionId)
            where TEvent : IEvent, new()
        {
            Guard.NotNullOrWhitespace(rawJsonBody, nameof(rawJsonBody));
            Guard.NotNullOrWhitespace(sessionId, nameof(sessionId));

            var array = JArray.Parse(rawJsonBody);

            var deserializedEvents = new List<TEvent>();
            foreach (var eventObject in array.Children<JObject>())
            {
                var rawEvent = eventObject.ToString();
                var gridEvent = JsonConvert.DeserializeObject<TEvent>(rawEvent);
                deserializedEvents.Add(gridEvent);
            }

            var result = new EventGridMessage<TEvent>(sessionId, deserializedEvents);

            return result;
        }
    }
}
