using System;
using System.Linq;
using Arcus.EventGrid.Contracts.Interfaces;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Azure.EventGrid.Models;

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
    public sealed class CloudOrEventGridEvent : IEvent
    {
        private readonly CloudEvent _cloudEvent;
        private readonly EventGridEvent _eventGridEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudOrEventGridEvent"/> class.
        /// </summary>
        public CloudOrEventGridEvent(CloudEvent cloudEvent)
        {
            Guard.NotNull(cloudEvent, nameof(cloudEvent));

            _cloudEvent = cloudEvent;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudOrEventGridEvent"/> class.
        /// </summary>
        public CloudOrEventGridEvent(EventGridEvent eventGridEvent)
        {
            Guard.NotNull(eventGridEvent, nameof(eventGridEvent));

            _eventGridEvent = eventGridEvent;
        }

        /// <summary>
        /// Represent this model as a <see cref="CloudEvent"/> or <c>null</c>.
        /// </summary>
        public CloudEvent AsCloudEvent()
        {
            return _cloudEvent;
        }

        /// <summary>
        /// Represent this model as an <see cref="EventGridEvent"/> or <c>null</c>.
        /// </summary>
        public EventGridEvent AsEventGridEvent()
        {
            return _eventGridEvent;
        }

        /// <summary>
        /// Represent this model as a <see cref="CloudEvent"/> or <c>null</c>.
        /// </summary>
        public static implicit operator CloudEvent(CloudOrEventGridEvent @event)
        {
            return @event._cloudEvent;
        }

        /// <summary>
        /// Represent this model as an <see cref="EventGridEvent"/> or <c>null</c>.
        /// </summary>
        public static implicit operator EventGridEvent(CloudOrEventGridEvent @event)
        {
            return @event._eventGridEvent;
        }

        /// <summary>
        /// Represent a <see cref="CloudEvent"/> as a <see cref="CloudOrEventGridEvent"/> representation.
        /// </summary>
        public static implicit operator CloudOrEventGridEvent(CloudEvent cloudEvent)
        {
            return new CloudOrEventGridEvent(cloudEvent);
        }

        /// <summary>
        /// Represent an <see cref="EventGridEvent"/> as a <see cref="CloudOrEventGridEvent"/> representation.
        /// </summary>
        public static implicit operator CloudOrEventGridEvent(EventGridEvent eventGridEvent)
        {
            return new CloudOrEventGridEvent(eventGridEvent);
        }

        /// <summary>
        ///     The schema version of the data object. The publisher defines the schema version.
        /// </summary>
        public string DataVersion => _eventGridEvent?.DataVersion;

        /// <summary>
        ///     The time the event is generated based on the provider's UTC time.
        /// </summary>
        public DateTimeOffset EventTime => _eventGridEvent?.EventTime ?? _cloudEvent?.Time ?? default(DateTimeOffset);

        /// <summary>
        ///     One of the registered event types for this event source.
        /// </summary>
        public string EventType => _eventGridEvent?.EventType ?? _cloudEvent?.Type;

        /// <summary>
        ///     Unique identifier for the event.
        /// </summary>
        public string Id => _eventGridEvent?.Id ?? _cloudEvent?.Id;

        /// <summary>
        ///     The schema version of the event metadata. Event Grid defines the schema of the top-level properties. Event Grid
        ///     provides this value.
        /// </summary>
        public string MetadataVersion => _eventGridEvent?.MetadataVersion;

        /// <summary>
        ///     Publisher-defined path to the event subject.
        /// </summary>
        public string Subject => _eventGridEvent?.Subject ?? _cloudEvent?.Subject;

        /// <summary>
        ///     Full resource path to the event source. This field is not writable. Event Grid provides this value.
        /// </summary>
        public string Topic => _eventGridEvent?.Topic ?? _cloudEvent?.Source?.OriginalString.Split('#').FirstOrDefault();
    }
}
