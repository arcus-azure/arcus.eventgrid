using System;
using System.Collections;
using System.Linq;
using Arcus.EventGrid.Contracts.Interfaces;
using GuardNet;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arcus.EventGrid.Contracts
{
    public class CloudEvent<TData> : IEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEvent{TData}"/> class.
        /// </summary>
        /// <param name="eventId">The ID of the event.</param>
        /// <param name="source">The event producer of the event.</param>
        /// <param name="cloudEventsVersion">The version of the CloudEvents specification the event uses.</param>
        /// <param name="eventType">The type of occurrence that happened for this event.</param>
        public CloudEvent(string eventId, string source, string cloudEventsVersion, string eventType)
            : this(eventId, source, data: null, cloudEventsVersion: cloudEventsVersion, eventType: eventType, eventTypeVersion: null, eventTime: DateTime.UtcNow)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEvent{TData}"/> class.
        /// </summary>
        /// <param name="eventId">The ID of the event.</param>
        /// <param name="source">The event producer of the event.</param>
        /// <param name="data">The event payload.</param>
        /// <param name="cloudEventsVersion">The version of the CloudEvents specification the event uses.</param>
        /// <param name="eventType">The type of occurrence that happened for this event.</param>
        /// <param name="eventTypeVersion">The version of the <paramref name="eventType"/>.</param>
        /// <param name="eventTime">The timestamp of when the event happened.</param>
        public CloudEvent(string eventId, string source, object data, string cloudEventsVersion, string eventType, string eventTypeVersion, DateTime eventTime)
        {
            Guard.NotNullOrWhitespace(eventId, nameof(eventId), "ID of cloud event cannot be null or blank");
            Guard.NotNullOrWhitespace(source, nameof(source), "Source of cloud event cannot be null or blank");
            Guard.NotNullOrWhitespace(cloudEventsVersion, nameof(cloudEventsVersion), "Version of the CloudEvents specification cannot be null or blank");
            Guard.NotNullOrWhitespace(eventType, nameof(eventType), "Type of occurence that happened cannot be null or blank");

            Id = eventId;
            Source = source;
            Data = data;

            (string topic, string subject) = SelectTopicSubjectFromSource(source);
            Topic = topic;
            Subject = subject;

            CloudEventsVersion = cloudEventsVersion;
            EventType = eventType;
            EventTypeVersion = eventTypeVersion;
            EventTime = eventTime;
        }

        private static (string, string) SelectTopicSubjectFromSource(string source)
        {
            string[] subjectTopic = source.Split('#');
            Guard.For<ArgumentException>(
                () => subjectTopic.Length > 2 || subjectTopic.Length == 0, 
                "Cannot determine event subject and topic from cloud event source (= 'topic#source') because source contains more than one hashtag ('#')");
            
            string topic = subjectTopic[0];
            string subject = subjectTopic.Length == 2 ? subjectTopic[1] : null;

            return (topic, subject);
        }

        /// <summary>
        ///     Gets the unique identifier for the event.
        /// </summary>
        [JsonProperty("eventID")]
        public string Id { get; set; }

        /// <summary>
        ///     Gets the version of the CloudEvents specification the event uses.
        /// </summary>
        public string CloudEventsVersion { get; }

        /// <summary>
        ///     Gets the event producer of the cloud event.
        /// </summary>
        public string Source { get; }

        /// <summary>
        ///     Gets the schema version of the data object. The publisher defines the schema version.
        /// </summary>
        [Obsolete("Only implemented for compatibility reasons, use " + nameof(EventTypeVersion) + " instead.")]
        string IEvent.DataVersion => EventTypeVersion;

        /// <summary>
        ///     Gets the schema version of the data object. The publisher defines the schema version.
        /// </summary>
        public string EventTypeVersion { get; }

        /// <summary>
        ///     Gets the time the event is generated based on the provider's UTC time.
        /// </summary>
        [Obsolete("Only implemented for compatibility reasons, use " + nameof(EventTime) + " instead.")]
        DateTimeOffset IEvent.EventTime => EventTime;

        /// <summary>
        ///     Gets the time the event is generated based on the provider's UTC time.
        /// </summary>
        public DateTime EventTime { get; }

        /// <summary>
        ///     Gets type of occurence that happened for this cloud event.
        /// </summary>
        public string EventType { get; }

        /// <summary>
        ///     Gets the schema version of the event metadata. Event Grid defines the schema of the top-level properties. Event Grid
        ///     provides this value.
        /// </summary>
        [Obsolete("Only implemented for compatibility reasons, use " + nameof(CloudEventsVersion) + " or " + nameof(EventTypeVersion) + " instead.")]
        string IEvent.MetadataVersion { get; }

        /// <summary>
        ///     Gets the publisher-defined path to the event subject (second part of the cloud event source: 'topic#subject').
        /// </summary>
        public string Subject { get; }

        /// <summary>
        ///     Gets the full resource path to the event source. This field is not writable. Event Grid provides this value (first part of the cloud event source: 'topic#subject').
        /// </summary>
        public string Topic { get; }

        /// <summary>
        ///     Gets the raw event of this cloud event.
        /// </summary>
        public object Data { get; }

        /// <summary>
        ///     Gets the event data specific to the resource provider.
        /// </summary>
        public TData GetPayload()
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
