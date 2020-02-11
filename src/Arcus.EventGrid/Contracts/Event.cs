using System;
using System.Linq;
using System.Net.Mime;
using System.Text;
using Arcus.EventGrid.Contracts.Interfaces;
using Arcus.EventGrid.Parsers;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arcus.EventGrid.Contracts
{
    /// <summary>
    /// Representation of a supported event in Azure Event Grid.
    /// This can either be an <see cref="CloudEvent"/> or <see cref="EventGridEvent"/> instance.
    /// </summary>
    /// <remarks>
    ///     This model is not build for custom events.
    ///     Create your own event by inheriting from <see cref="EventGridEvent{TData}"/>.
    /// </remarks>
    [JsonConverter(typeof(EventConverter))]
    public sealed class Event : IEvent
    {
        private readonly JObject _rawInput;

        private CloudEvent _cloudEvent;
        private EventGridEvent _eventGridEvent;
        private bool _isCloudEvent, _isEventGridEvent, _isPending;

        private static readonly JsonEventFormatter JsonFormatter = new JsonEventFormatter();

        /// <summary>
        /// Initializes a new instance of the <see cref="Event"/> class.
        /// </summary>
        public Event(CloudEvent cloudEvent)
        {
            Guard.NotNull(cloudEvent, nameof(cloudEvent));
            Guard.For<ArgumentException>(
                () => String.Equals(cloudEvent.DataContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase),
                "Only Cloud Events with a 'application/json' content type are supported");

            _cloudEvent = cloudEvent;
            _isCloudEvent = true;
            _isPending = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Event"/> class.
        /// </summary>
        public Event(EventGridEvent eventGridEvent)
        {
            Guard.NotNull(eventGridEvent, nameof(eventGridEvent));

            _eventGridEvent = eventGridEvent;
            _isEventGridEvent = true;
            _isPending = false;
        }

        internal Event(JObject rawInput)
        {
            Guard.NotNull(rawInput, nameof(rawInput));

            _rawInput = rawInput;
            _isPending = true;
        }

        /// <summary>
        /// Represent this model as a <see cref="CloudEvent"/> or <c>null</c>.
        /// </summary>
        public CloudEvent AsCloudEvent()
        {
            LoadRawInput();

            if (_isCloudEvent)
            {
                return _cloudEvent;
            }

            return EventGridEventToCloudEvent(_eventGridEvent);
        }

        private static CloudEvent EventGridEventToCloudEvent(EventGridEvent eventGridEvent)
        {
            // TODO: how do we define the source?
            string source = $"/{eventGridEvent.Subject}/#{eventGridEvent.Topic}/{eventGridEvent.MetadataVersion}";
            var cloudEvent = new CloudEvent(
                eventGridEvent.EventType,
                new Uri(source),
                eventGridEvent.Id,
                eventGridEvent.EventTime)
            {
                Subject = eventGridEvent.Subject,
                Data = eventGridEvent.Data
            };

            return cloudEvent;
        }

        /// <summary>
        /// Represent this model as an <see cref="EventGridEvent"/> or <c>null</c>.
        /// </summary>
        public EventGridEvent AsEventGridEvent()
        {
            LoadRawInput();

            if (_isEventGridEvent)
            {
                return _eventGridEvent;
            }

            EventGridEvent eventGridEvent = CloudEventToEventGridEvent(_cloudEvent);
            return eventGridEvent;
        }

        private static EventGridEvent CloudEventToEventGridEvent(CloudEvent cloudEvent)
        {
            string topic = cloudEvent.Source?.OriginalString.Split('#').FirstOrDefault();
            var eventGridEvent = new EventGridEvent(
                cloudEvent.Id,
                cloudEvent.Subject,
                cloudEvent.Data,
                cloudEvent.Type,
                cloudEvent.Time ?? default(DateTime),
                dataVersion: null,
                topic: topic);

            return eventGridEvent;
        }

        /// <summary>
        /// Represent this model as a <see cref="CloudEvent"/> or <c>null</c>.
        /// </summary>
        public static implicit operator CloudEvent(Event @event)
        {
            return @event.AsCloudEvent();
        }

        /// <summary>
        /// Represent this model as an <see cref="EventGridEvent"/> or <c>null</c>.
        /// </summary>
        public static implicit operator EventGridEvent(Event @event)
        {
            return @event.AsEventGridEvent();
        }

        /// <summary>
        /// Represent a <see cref="CloudEvent"/> as a <see cref="Event"/> representation.
        /// </summary>
        public static implicit operator Event(CloudEvent cloudEvent)
        {
            return new Event(cloudEvent);
        }

        /// <summary>
        /// Represent an <see cref="EventGridEvent"/> as a <see cref="Event"/> representation.
        /// </summary>
        public static implicit operator Event(EventGridEvent eventGridEvent)
        {
            return new Event(eventGridEvent);
        }

        /// <summary>
        ///     The schema version of the data object. The publisher defines the schema version.
        /// </summary>
        public string DataVersion
        {
            get
            {
                LoadRawInput();

                if (_isEventGridEvent)
                {
                    return _eventGridEvent.DataVersion;
                }

                throw new InvalidOperationException(
                    "Cannot get the data version of this event because it represents a Cloud Event; which don't have any schema version of the data object information");

            }
        }

        /// <summary>
        ///     The time the event is generated based on the provider's UTC time.
        /// </summary>
        public DateTimeOffset EventTime
        {
            get
            {
                LoadRawInput();

                if (_isCloudEvent)
                {
                    return _cloudEvent.Time ?? default(DateTimeOffset);
                }
                else
                {
                    return _eventGridEvent.EventTime;
                }
            }
        }

        /// <summary>
        ///     One of the registered event types for this event source.
        /// </summary>
        public string EventType
        {
            get
            {
                LoadRawInput();

                if (_isCloudEvent)
                {
                    return _cloudEvent.Type;
                }
                else
                {
                    return _eventGridEvent.EventType;
                }
            }
        }

        /// <summary>
        ///     Unique identifier for the event.
        /// </summary>
        public string Id
        {
            get
            {
                LoadRawInput();

                if (_isCloudEvent)
                {
                    return _cloudEvent.Id;
                }
                else
                {
                    return _eventGridEvent.Id;
                }
            }
        }

        /// <summary>
        ///     The schema version of the event metadata. Event Grid defines the schema of the top-level properties. Event Grid
        ///     provides this value.
        /// </summary>
        public string MetadataVersion
        {
            get
            {
                LoadRawInput();

                if (_isEventGridEvent)
                {
                    return _eventGridEvent.MetadataVersion;
                }

                throw new InvalidOperationException(
                    "Cannot get the meta-data version of this event because it represents a Cloud Event, which don't have any schema version of the event meta-data information");

            }
        }

        /// <summary>
        ///     Publisher-defined path to the event subject.
        /// </summary>
        public string Subject
        {
            get
            {
                LoadRawInput();

                if (_isCloudEvent)
                {
                    return _cloudEvent.Subject;
                }
                else
                {
                    return _eventGridEvent.Subject;
                }
            }
        }

        /// <summary>
        ///     Full resource path to the event source. This field is not writable. Event Grid provides this value.
        /// </summary>
        public string Topic
        {
            get
            {
                LoadRawInput();

                if (_isCloudEvent)
                {
                    return _cloudEvent.Source?.OriginalString.Split('#').FirstOrDefault();
                }
                else
                {
                    return _eventGridEvent.Topic;
                }
            }
        }

        private void LoadRawInput()
        {
            if (_isPending)
            {
                bool isCloudEventV01 = _rawInput.ContainsKey("cloudEventsVersion");
                if (isCloudEventV01)
                {
                    _cloudEvent = JsonFormatter.DecodeJObject(_rawInput);
                    _isCloudEvent = true;
                }
                else
                {
                    _eventGridEvent = _rawInput.ToObject<EventGridEvent>();
                    _isEventGridEvent = true;
                }
            }

            _isPending = false;
        }

        /// <summary>
        /// Gets the typed data payload from the abstracted event.
        /// </summary>
        /// <typeparam name="TData">The type of the payload the event is assumed to have.</typeparam>
        public TData GetPayload<TData>()
        {
            LoadRawInput();

            if (_isCloudEvent)
            {
                return _cloudEvent.GetPayload<TData>();
            }
            else
            {
                return _eventGridEvent.GetPayload<TData>();
            }
        }

        /// <summary>
        /// Serializes the abstracted event to a series of bytes.
        /// </summary>
        public byte[] SerializeAsBytes()
        {
            if (_isPending)
            {
                string json = _rawInput.ToString(Formatting.None);
                return Encoding.UTF8.GetBytes(json);
            }

            if (_isEventGridEvent)
            {
                string json = JsonConvert.SerializeObject(_eventGridEvent);
                return Encoding.UTF8.GetBytes(json);
            }

            if (_isCloudEvent)
            {
                return JsonFormatter.EncodeStructuredEvent(_cloudEvent, out ContentType contentType);
            }

            throw new InvalidOperationException(
                "Can't serialize event to a series of bytes because the type of the event is not either an EventGrid event or a CloudEvent event");
        }

        /// <summary>
        /// Serializes the abstracted event to a <c>string</c>.
        /// </summary>
        public string SerializeAsString()
        {
            if (_isPending)
            {
                return _rawInput.ToString(Formatting.None);
            }

            if (_isEventGridEvent)
            {
                return JsonConvert.SerializeObject(_eventGridEvent);
            }

            if (_isCloudEvent)
            {
                byte[] bytes = JsonFormatter.EncodeStructuredEvent(_cloudEvent, out ContentType contentType);
                return Encoding.UTF8.GetString(bytes);
            }

            throw new InvalidOperationException(
                "Can't serialize event to a string because the type of the event is not either an EventGrid event or a CloudEvent event");
        }

        /// <summary>
        /// Writes the serialized version of the abstracted event to a JSON writer.
        /// </summary>
        /// <param name="writer">The writer instance to send the abstracted event representation to.</param>
        public void WriteTo(JsonWriter writer)
        {
            if (_isPending)
            {
                _rawInput.WriteTo(writer);
            }

            if (_isEventGridEvent)
            {
                JObject.FromObject(_eventGridEvent).WriteTo(writer);
            }

            if (_isCloudEvent)
            {
                byte[] bytes = JsonFormatter.EncodeStructuredEvent(_cloudEvent, out ContentType contentType);
                string json = Encoding.UTF8.GetString(bytes);
                JObject.Parse(json).WriteTo(writer);
            }

            throw new InvalidOperationException(
                "Can't serialize event to a series of bytes because the type of the event is not either an EventGrid event or a CloudEvent event");
        }
    }
}
