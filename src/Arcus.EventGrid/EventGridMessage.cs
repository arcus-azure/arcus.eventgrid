using System;
using System.Collections.Generic;
using Arcus.EventGrid.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arcus.EventGrid
{
    public class EventGridMessage<TData>
    {
        /// <summary>
        /// Creates event grid message with passed session Id
        /// </summary>
        /// <param name="sessionId">Unique session id for all batched messages</param>
        public EventGridMessage(string sessionId)
        {
            Guard.AgainstNullOrEmptyValue(sessionId, nameof(sessionId));

            SessionId = sessionId;
        }

        /// <summary>
        ///     Unique session id for all batched messages
        /// </summary>
        public string SessionId { get; }

        /// <summary>
        ///     List of all events, belonging to the Event Grid message
        /// </summary>
        public List<Event<TData>> Events { get; } = new List<Event<TData>>();

        /// <summary>
        ///     Parses a string to a EventGridMessage with typed data payload
        /// </summary>
        /// <param name="rawJsonBody">Raw JSON body</param>
        /// <returns>Typed EventGridMessage</returns>
        public static EventGridMessage<TData> Parse(string rawJsonBody)
        {
            Guard.AgainstNullOrEmptyValue(rawJsonBody, nameof(rawJsonBody));
            var sessionId = Guid.NewGuid().ToString();

            var eventGridMessage = Parse(rawJsonBody, sessionId);
            return eventGridMessage;
        }

        /// <summary>
        ///     Parses a string to a EventGridMessage with typed data payload
        /// </summary>
        /// <param name="rawJsonBody">Raw JSON body</param>
        /// <param name="sessionId">Session id for event grid message</param>
        /// <returns>Typed EventGridMessage</returns>
        public static EventGridMessage<TData> Parse(string rawJsonBody, string sessionId)
        {
            var array = JArray.Parse(rawJsonBody);
            var result = new EventGridMessage<TData>(sessionId);

            foreach (var eventObject in array.Children<JObject>())
            {
                var rawEvent = eventObject.ToString();
                var gridEvent = JsonConvert.DeserializeObject<Event<TData>>(rawEvent);
                result.Events.Add(gridEvent);
            }

            return result;
        }
    }
}