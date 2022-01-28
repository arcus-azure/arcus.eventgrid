using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Mime;
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
    public sealed class Event : IEvent, IEquatable<Event>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Event"/> class based on <see cref="CloudEvent"/> information.
        /// </summary>
        /// <param name="id">The unique identifier of the event.</param>
        /// <param name="subject">The publisher-defined path to the event subject.</param>
        /// <param name="eventType">The registered event types for this event's source.</param>
        /// <param name="eventTime">The time the event is generated based on the provider's UTC time.</param>
        /// <param name="data">The payload of the event.</param>
        /// <param name="source">The origin of the event.</param>
        /// <param name="topic">The full resource path to the event source. This field is not writable. Event Grid provides this value.</param>
        /// <param name="specVersion">The version of the CloudEvent specification.</param>
        /// <param name="attributes">The attributes of this event.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="id"/>, the <paramref name="eventType"/>, or the <paramref name="data"/> is <c>null</c>.
        /// </exception>
        public Event(
            string id,
            string subject,
            string eventType,
            DateTime? eventTime,
            object data,
            Uri source,
            string topic,
            CloudEventsSpecVersion specVersion,
            IDictionary<string, object> attributes)
        {
            Guard.NotNull(id, nameof(id), "Requires an ID to identify the event");
            Guard.NotNull(eventType, nameof(eventType), "Requires a type of the event");
            Guard.NotNull(data, nameof(data), "Requires payload data of the event");

            Id = id;
            Subject = subject;
            EventType = eventType;
            EventTime = eventTime ?? default(DateTimeOffset);
            Data = data;
            Source = source;
            Topic = topic;
            SpecVersion = specVersion;
            Attributes = new ReadOnlyDictionary<string, object>(attributes ?? new Dictionary<string, object>());
            IsCloudEvent = true;
            IsEventGridEvent = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Event"/> class based on <see cref="EventGridEvent"/> information.
        /// </summary>
        /// <param name="id">The unique identifier of the event.</param>
        /// <param name="subject">The publisher-defined path to the event subject.</param>
        /// <param name="eventType">The registered event types for this event's source.</param>
        /// <param name="eventTime">The time the event is generated based on the provider's UTC time.</param>
        /// <param name="data">The payload of the event.</param>
        /// <param name="topic">The full resource path to the event source. This field is not writable. Event Grid provides this value.</param>
        /// <param name="dataVersion">The schema version of the data object. The publisher defines the schema version.</param>
        /// <param name="metaDataVersion">The schema version of the event metadata.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="id"/>, the <paramref name="eventType"/>, or the <paramref name="data"/> is <c>null</c>.
        /// </exception>
        public Event(
            string id,
            string subject,
            string eventType,
            DateTime? eventTime,
            object data,
            string topic,
            string dataVersion,
            string metaDataVersion)
        {
            Guard.NotNull(id, nameof(id), "Requires an ID to identify the event");
            Guard.NotNull(eventType, nameof(eventType), "Requires a type of the event");
            Guard.NotNull(data, nameof(data), "Requires payload data of the event");

            Id = id;
            Subject = subject;
            EventType = eventType;
            EventTime = eventTime ?? default(DateTimeOffset);
            Data = data;
            Topic = topic;
            DataVersion = dataVersion;
            MetadataVersion = metaDataVersion;
            Attributes = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());
            IsEventGridEvent = true;
            IsCloudEvent = false;
        }

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
        /// <param name="attributes">The attributes of this event.</param>
        [Obsolete("Use one of the other constructors to be more specific of the event type (CloudEvent of EventGridEvent)")]
        public Event(
            string id,
            string subject,
            string eventType,
            DateTime? eventTime,
            object data,
            Uri source = null,
            string topic = null,
            string dataVersion = null,
            string metaDataVersion = null,
            IDictionary<string, object> attributes = null)
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
            Attributes = new ReadOnlyDictionary<string, object>(attributes ?? new Dictionary<string, object>());
        }

        /// <summary>
        /// Gets the unique identifier for the event.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets one of the registered event types for this event's source.
        /// </summary>
        public string EventType { get; }

        /// <summary>
        /// Gets the publisher-defined path to the event's subject.
        /// </summary>
        public string Subject { get; }

        /// <summary>
        /// Gets the time the event is generated based on the provider's UTC time.
        /// </summary>
        public DateTimeOffset EventTime { get; }

        /// <summary>
        /// Gets the schema version of the data object. The publisher defines the schema version.
        /// </summary>
        public string DataVersion { get; }

        /// <summary>
        /// Gets the schema version of the event metadata. Event Grid defines the schema of the top-level properties. Event Grid provides this value.
        /// </summary>
        public string MetadataVersion { get; }

        /// <summary>
        /// Gets the full resource path to the event source. This field is not writable. Event Grid provides this value.
        /// </summary>
        public string Topic { get; }

        /// <summary>
        /// Gets the origin of the event.
        /// </summary>
        public Uri Source { get; }

        /// <summary>
        /// Gets the attributes of this event.
        /// </summary>
        public IReadOnlyDictionary<string, object> Attributes { get; }

        /// <summary>
        /// Gets the payload of the event.
        /// </summary>
        public object Data { get; }

        /// <summary>
        /// Gets the optional specifications version of the <see cref="CloudEvent"/>.
        /// </summary>
        /// <remarks>
        ///     Only available when the <see cref="Event"/> is an <see cref="CloudEvent"/>.
        /// </remarks>
        public CloudEventsSpecVersion? SpecVersion { get; }

        /// <summary>
        /// Gets the flag indicating whether or not the current event is a <see cref="CloudEvent"/>.
        /// </summary>
        public bool IsCloudEvent { get; }

        /// <summary>
        /// Gets the flag indicating whether or not the current event is an <see cref="EventGridEvent"/>.
        /// </summary>
        public bool IsEventGridEvent { get; }

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

        /// <summary>
        /// Represent this model as a <see cref="CloudEvent"/> or <c>null</c>.
        /// </summary>
        public CloudEvent AsCloudEvent(
            Uri source = null,
            CloudEventsSpecVersion? specVersion = null,
            IEnumerable<ICloudEventExtension> extensions = null)
        {
            CloudEventsSpecVersion version = (specVersion ?? SpecVersion).GetValueOrDefault(CloudEventsSpecVersion.Default);
            if (Attributes.Count > 0)
            {
                var cloudEvent = new CloudEvent(version, extensions);
                IDictionary<string, object> attributes = cloudEvent.GetAttributes();

                foreach (KeyValuePair<string, object> keyValuePair in Attributes)
                {
                    attributes[keyValuePair.Key] = keyValuePair.Value;
                }

                return cloudEvent;
            }

            return new CloudEvent(
                version,
                EventType,
                Source ?? source,
                Subject ?? String.Empty,
                Id,
                EventTime.DateTime,
                extensions?.ToArray())
            {
                Data = Data,
                DataContentType = new ContentType("application/json")
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
            return @event?.AsCloudEvent();
        }

        /// <summary>
        /// Represent this model as an <see cref="EventGridEvent"/> or <c>null</c>.
        /// </summary>
        public static implicit operator EventGridEvent(Event @event)
        {
            return @event?.AsEventGridEvent();
        }

        /// <summary>
        /// Represent a <see cref="CloudEvent"/> as a <see cref="Event"/> representation.
        /// </summary>
        public static implicit operator Event(CloudEvent cloudEvent)
        {
            if (cloudEvent is null)
            {
                return null;
            }

            return new Event(
                id: cloudEvent.Id,
                subject: cloudEvent.Subject,
                eventType: cloudEvent.Type,
                eventTime: cloudEvent.Time,
                data: cloudEvent.Data,
                source: cloudEvent.Source,
                topic: cloudEvent.Source?.OriginalString.Split('#').FirstOrDefault(),
                specVersion: cloudEvent.SpecVersion,
                attributes: cloudEvent.GetAttributes());
        }

        /// <summary>
        /// Represent an <see cref="EventGridEvent"/> as a <see cref="Event"/> representation.
        /// </summary>
        public static implicit operator Event(EventGridEvent eventGridEvent)
        {
            if (eventGridEvent is null)
            {
                return null;
            }

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
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
        public bool Equals(Event other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Id == other.Id;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is Event other && Equals(other);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>Returns a value that indicates whether the values of two <see cref="T:Arcus.EventGrid.Contracts.Event" /> objects are equal.</summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns>true if the <paramref name="left" /> and <paramref name="right" /> parameters have the same value; otherwise, false.</returns>
        public static bool operator ==(Event left, Event right)
        {
            return Equals(left, right);
        }

        /// <summary>Returns a value that indicates whether two <see cref="T:Arcus.EventGrid.Contracts.Event" /> objects have different values.</summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns>true if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, false.</returns>
        public static bool operator !=(Event left, Event right)
        {
            return !Equals(left, right);
        }
    }
}
