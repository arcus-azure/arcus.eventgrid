using System;
using System.Linq;
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
        /// <summary>
        /// Initializes a new instance of the <see cref="Event"/> class.
        /// </summary>
        /// <param name="id">The unique identifier of the event.</param>
        /// <param name="subject">The publisher-defined path to the event subject.</param>
        /// <param name="eventType">The registered event types for this event's source.</param>
        /// <param name="eventTime">The time the event is generated based on the provider's UTC time.</param>
        /// <param name="data">The payload of the event.</param>
        /// <param name="source">The origin of the event.</param>
        /// <param name="topic">The full resource path to the event source. This field is not writable. Event Grid provides this value.</param>
        /// <param name="dataVersion">The schema version of the data object. The publisher defines the schema version.</param>
        /// <param name="metaDataVersion">The schema version of the event metadata.</param>
        public Event(
            string id,
            string subject,
            string eventType,
            DateTime? eventTime,
            object data,
            Uri source = null,
            string topic = null,
            string dataVersion = null,
            string metaDataVersion = null)
        {
            Guard.NotNull(id, nameof(id));
            Guard.NotNull(eventType, nameof(eventType));
            Guard.NotNull(data, nameof(data));

            Id = id;
            Subject = subject;
            EventType = eventType;
            EventTime = eventTime ?? default(DateTimeOffset);
            Data = data;
            Source = source;
            Topic = topic;
            DataVersion = dataVersion;
            MetadataVersion = metaDataVersion;
        }

        /// <summary>
        /// Represent this model as a <see cref="CloudEvent"/> or <c>null</c>.
        /// </summary>
        public CloudEvent AsCloudEvent(
            Uri source = null,
            CloudEventsSpecVersion specVersion = CloudEventsSpecVersion.V0_1)
        {
            return new CloudEvent(specVersion, EventType, Source ?? source, Subject, Id, EventTime.DateTime)
            {
                Data = Data,
            };
        }

        /// <summary>
        /// Represent this model as an <see cref="EventGridEvent"/> or <c>null</c>.
        /// </summary>
        public EventGridEvent AsEventGridEvent()
        {
            return new EventGridEvent(Id, Subject, Data, EventType, EventTime.DateTime, DataVersion, Topic, MetadataVersion);
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
            return new Event(
                id: cloudEvent.Id,
                subject: cloudEvent.Subject,
                eventType: cloudEvent.Type,
                eventTime: cloudEvent.Time,
                data: cloudEvent.Data,
                source: cloudEvent.Source,
                topic: cloudEvent.Source?.OriginalString.Split('#').FirstOrDefault(),
                metaDataVersion: cloudEvent.SpecVersion.ToString());
        }

        /// <summary>
        /// Represent an <see cref="EventGridEvent"/> as a <see cref="Event"/> representation.
        /// </summary>
        public static implicit operator Event(EventGridEvent eventGridEvent)
        {
            return new Event(
                id: eventGridEvent.Id,
                subject: eventGridEvent.Subject,
                eventType: eventGridEvent.EventType,
                eventTime: eventGridEvent.EventTime,
                data: eventGridEvent.Data,
                topic: eventGridEvent.Topic,
                dataVersion: eventGridEvent.DataVersion,
                metaDataVersion: eventGridEvent.MetadataVersion);
        }

        /// <summary>
        ///     Gets the unique identifier for the event.
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Gets one of the registered event types for this event's source.
        /// </summary>
        public string EventType { get; }

        /// <summary>
        ///     Gets the publisher-defined path to the event's subject.
        /// </summary>
        public string Subject { get; }

        /// <summary>
        ///     Gets the time the event is generated based on the provider's UTC time.
        /// </summary>
        public DateTimeOffset EventTime { get; }

        /// <summary>
        ///     Gets the schema version of the data object. The publisher defines the schema version.
        /// </summary>
        public string DataVersion { get; }

        /// <summary>
        ///     Gets the schema version of the event metadata. Event Grid defines the schema of the top-level properties. Event Grid
        ///     provides this value.
        /// </summary>
        public string MetadataVersion { get; }

        /// <summary>
        ///     Gets the full resource path to the event source. This field is not writable. Event Grid provides this value.
        /// </summary>
        public string Topic { get; }

        /// <summary>
        ///     Gets the origin of the event.
        /// </summary>
        public Uri Source { get; }

        /// <summary>
        ///     Gets the payload of the event.
        /// </summary>
        public object Data { get; }

        /// <summary>
        /// Gets the typed data payload from the abstracted event.
        /// </summary>
        /// <typeparam name="TData">The type of the payload the event is assumed to have.</typeparam>
        public TData GetPayload<TData>()
        {
            if (Data is null)
            {
                return default(TData);
            }

            if (Data is TData data)
            {
                return data;
            }

            return JObject.Parse(Data.ToString()).ToObject<TData>();
        }
    }
}
