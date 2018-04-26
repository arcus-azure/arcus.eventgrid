using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arcus.EventGrid
{
    public class EventGridMessage<T>
    {
        /// <summary>
        /// Parsing a string to an EventGridMessage with typed EventData
        /// </summary>
        /// <param name="jsonBody">json content</param>
        /// <param name="sessionId">optional session id for event grid message</param>
        /// <returns>Typed EventGridMessage</returns>
        public static EventGridMessage<T> Parse(string jsonBody, string sessionId = null)
        {
            var array = JArray.Parse(jsonBody);
            var result = new EventGridMessage<T>
            { SessionId = sessionId ?? Guid.NewGuid().ToString() };

            foreach (var eventObject in array.Children<JObject>())
            {
                var gridEvent = JsonConvert.DeserializeObject<Event<T>>(eventObject.ToString());
                result.Events.Add(gridEvent);
            }
            return result;
        }
        /// <summary>
        /// Unique session id for all batched messages
        /// </summary>
        public string SessionId { get; set; }
        /// <summary>
        /// List of all events, belonging to the Event Grid message
        /// </summary>
        public List<Event<T>> Events { get; set; } = new List<Event<T>>();
    }
}