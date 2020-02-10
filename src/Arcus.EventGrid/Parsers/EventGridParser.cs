using System;
using System.Collections.Generic;
using System.Text;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Contracts.Interfaces;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arcus.EventGrid.Parsers
{
    /// <summary>
    /// Expose parsing operations on raw Event Grid events with custom <see cref="IEvent"/> implementations.
    /// </summary>
    [Obsolete("Use " + nameof(EventParser) + " instead to parse raw JSON payloads to abstracted events")]
    public static class EventGridParser
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };

        /// <summary>
        ///     Parses a string to a <see cref="EventGridEventBatch{TEvent}"/> with typed data payload
        /// </summary>
        /// <param name="rawJsonBody">Raw JSON body</param>
        public static EventGridEventBatch<EventGridEvent<TEventData>> ParseFromData<TEventData>(string rawJsonBody)
            where TEventData : class
        {
            Guard.NotNullOrWhitespace(rawJsonBody, nameof(rawJsonBody));

            var eventGridMessage = Parse<EventGridEvent<TEventData>>(rawJsonBody);
            return eventGridMessage;
        }

        /// <summary>
        ///     Parses a string to a <see cref="EventGridEventBatch{TEvent}"/> with typed data payload
        /// </summary>
        /// <param name="rawJsonBody">Raw JSON body</param>
        /// <param name="sessionId">Session id for event grid message</param>
        public static EventGridEventBatch<EventGridEvent<TEventData>> ParseFromData<TEventData>(string rawJsonBody, string sessionId)
            where TEventData : class
        {
            Guard.NotNullOrWhitespace(rawJsonBody, nameof(rawJsonBody));
            Guard.NotNullOrWhitespace(sessionId, nameof(sessionId));

            var eventGridMessage = Parse<EventGridEvent<TEventData>>(rawJsonBody, sessionId);
            return eventGridMessage;
        }

        /// <summary>
        ///     Parses a string to a <see cref="EventGridEventBatch{TEvent}"/> with a custom <typeparamref name="TEvent"/> event implementation.
        /// </summary>
        /// <param name="rawJsonBody">Raw JSON body</param>
        public static EventGridEventBatch<TEvent> Parse<TEvent>(string rawJsonBody)
            where TEvent : IEvent
        {
            Guard.NotNullOrWhitespace(rawJsonBody, nameof(rawJsonBody));

            var sessionId = Guid.NewGuid().ToString();

            var eventGridMessage = Parse<TEvent>(rawJsonBody, sessionId);
            return eventGridMessage;
        }

        /// <summary>
        ///     Parses a string to a <see cref="EventGridEventBatch{TEvent}"/> with a custom <typeparamref name="TEvent"/> event implementation.
        /// </summary>
        /// <param name="rawJsonBody">Raw JSON body</param>
        /// <param name="sessionId">Session id for event grid message</param>
        public static EventGridEventBatch<TEvent> Parse<TEvent>(string rawJsonBody, string sessionId)
            where TEvent : IEvent
        {
            Guard.NotNullOrWhitespace(rawJsonBody, nameof(rawJsonBody));
            Guard.NotNullOrWhitespace(sessionId, nameof(sessionId));

            var array = JArray.Parse(rawJsonBody);
            
            var deserializedEvents = new List<TEvent>();
            foreach (var eventObject in array.Children<JObject>())
            {
                var rawEvent = eventObject.ToString();

                var gridEvent = JsonConvert.DeserializeObject<TEvent>(rawEvent, JsonSerializerSettings);
                deserializedEvents.Add(gridEvent);
            }

            var result = new EventGridEventBatch<TEvent>(sessionId, deserializedEvents);
            return result;
        }

        /// <summary>
        ///     Parses a string to a <see cref="EventGridEventBatch{TEvent}"/> from either a <see cref="CloudEvent"/> or <see cref="EventGridEvent"/> implementation.
        /// </summary>
        /// <param name="rawJsonBody">Raw JSON body</param>
        public static EventGridEventBatch<Event> Parse(string rawJsonBody)
        {
            string sessionId = Guid.NewGuid().ToString();

            var eventGridEventBatch = Parse(rawJsonBody, sessionId);
            return eventGridEventBatch;
        }

        /// <summary>
        ///     Parses a string to a <see cref="EventGridEventBatch{TEvent}"/> from either a <see cref="CloudEvent"/> or <see cref="EventGridEvent"/> implementation.
        /// </summary>
        /// <param name="rawJsonBody">Raw JSON body</param>
        /// <param name="sessionId">Session id for event grid message</param>
        public static EventGridEventBatch<Event> Parse(string rawJsonBody, string sessionId)
        {
            Guard.NotNullOrWhitespace(rawJsonBody, nameof(rawJsonBody));
            Guard.NotNullOrWhitespace(sessionId, nameof(sessionId));

            var array = JArray.Parse(rawJsonBody);

            var deserializedEvents = new List<Event>();
            foreach (var eventObject in array.Children<JObject>())
            {
                var rawEvent = eventObject.ToString();

                if (eventObject.ContainsKey("cloudEventsVersion"))
                {
                    var jsonFormatter = new JsonEventFormatter();
                    var cloudEvent = jsonFormatter.DecodeStructuredEvent(Encoding.UTF8.GetBytes(rawEvent));

                    deserializedEvents.Add(cloudEvent);
                }
                else
                {
                    var gridEvent = JsonConvert.DeserializeObject<EventGridEvent>(rawEvent, JsonSerializerSettings);
                    deserializedEvents.Add(gridEvent);
                }
            }

            var result = new EventGridEventBatch<Event>(sessionId, deserializedEvents);
            return result;
        }
    }
}