using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arcus.EventGrid.Contracts;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arcus.EventGrid.Parsers 
{
    /// <summary>
    /// Represents a builder to compose how the parsing of raw events should be handled.
    /// </summary>
    /// <seealso cref="EventGridParser"/>
    public class EventGridParserBuilder
    {
        private readonly string _rawJsonString;
        private readonly byte[] _rawJsonByteArray;

        private string _sessionId = Guid.NewGuid().ToString();

        private static readonly JsonEventFormatter JsonFormatter = new JsonEventFormatter();
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer
        {
            ConstructorHandling =  ConstructorHandling.AllowNonPublicDefaultConstructor
        };

        internal EventGridParserBuilder(string rawJsonString)
        {
            Guard.NotNullOrWhitespace(rawJsonString, nameof(rawJsonString), "Requires a non-blank raw JSON payload to be parsed to an event(s)");

            _rawJsonString = rawJsonString;
        }

        internal EventGridParserBuilder(byte[] rawJsonByteArray)
        {
            Guard.NotNull(rawJsonByteArray, nameof(rawJsonByteArray), "Requires a non-empty raw JSON payload to be parsed to an event(s)");
            Guard.For<ArgumentException>(() => rawJsonByteArray.Length == 0, "Requires a non-empty raw JSON payload to be parsed to an event(s)");

            _rawJsonByteArray = rawJsonByteArray;
        }

        /// <summary>
        /// Adds a personalized session identifier to the batch of events to parse.
        /// </summary>
        /// <param name="sessionId">The unique identifier.</param>
        public EventGridParserBuilder WithSessionId(string sessionId)
        {
            Guard.NotNullOrWhitespace(sessionId, nameof(sessionId), "Requires a non-blank session ID to append to the to-be-parsed event(s)");
            _sessionId = sessionId;
            
            return this;
        }

        /// <summary>
        /// Parse the raw event to an <seealso cref="EventGridEvent{TEventData}"/> representation.
        /// </summary>
        /// <typeparam name="TEventData">The event payload data of the <see cref="EventGridEvent{TEventData}"/>.</typeparam>
        public EventGridEventBatch<EventGridEvent<TEventData>> ToEventGridEvent<TEventData>()
        {
            return ToCustomEvent<EventGridEvent<TEventData>>();
        }

        /// <summary>
        /// Parse the raw event to a custom <typeparamref name="TEvent"/> representation.
        /// </summary>
        /// <typeparam name="TEvent">The type of the custom event.</typeparam>
        public EventGridEventBatch<TEvent> ToCustomEvent<TEvent>()
        {
            return ParseOneOrMany(jObject => jObject.ToObject<TEvent>(JsonSerializer));
        }

        /// <summary>
        /// Parse the raw event to a <see cref="CloudEvent"/> representation.
        /// </summary>
        public EventGridEventBatch<CloudEvent> ToCloudEvent()
        {
            return ParseOneOrMany(jObject => JsonFormatter.DecodeJObject(jObject));
        }

        /// <summary>
        /// Parse the raw event to a abstract <see cref="Event"/> representation that has a set of events either be an <see cref="EventGridEvent"/> or <see cref="CloudEvent"/> or both.
        /// </summary>
        public EventGridEventBatch<Event> ToEvent()
        {
            return ParseOneOrMany<Event>(jObject =>
            {
                bool isCloudEventV01 = jObject.ContainsKey("cloudEventsVersion");
                if (isCloudEventV01)
                {
                    CloudEvent cloudEvent = JsonFormatter.DecodeJObject(jObject);
                    return cloudEvent;
                }

                var eventGridEvent = jObject.ToObject<EventGridEvent>(JsonSerializer);
                return eventGridEvent;
            });
        }

        private EventGridEventBatch<TEvent> ParseOneOrMany<TEvent>(Func<JObject, TEvent> decodeJObject)
        {
            JToken jToken = LoadRawInput();

            if (jToken.Type == JTokenType.Array)
            {
                List<TEvent> deserializedEvents = 
                    jToken.Children<JObject>()
                          .Select(decodeJObject)
                          .ToList();

                var result = new EventGridEventBatch<TEvent>(_sessionId, deserializedEvents);
                return result;
            }
            else if (jToken.Type == JTokenType.Object)
            {
                TEvent @event = decodeJObject((JObject) jToken);
                var deserializedEvents = new List<TEvent> { @event };

                var result = new EventGridEventBatch<TEvent>(_sessionId, deserializedEvents);
                return result;
            }

            throw new InvalidOperationException(
                "Couldn't find a correct JSON structure (array or object) to parse the EventGridEvent/CloudEvents from");
        }

        private JToken LoadRawInput()
        {
            if (_rawJsonString != null)
            {
                return JToken.Parse(_rawJsonString);
            }

            if (_rawJsonByteArray != null)
            {
                return JToken.Parse(Encoding.UTF8.GetString(_rawJsonByteArray));
            }

            throw new InvalidOperationException(
                $"The {nameof(EventGridParserBuilder)} was not configured with the correct initial to-be-parsed input");
        }
    }
}