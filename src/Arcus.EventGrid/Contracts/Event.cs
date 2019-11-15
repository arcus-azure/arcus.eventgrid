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
    public sealed class Event : IEvent
    {
        private readonly CloudEvent _cloudEvent;
        private readonly EventGridEvent _eventGridEvent;

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
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Event"/> class.
        /// </summary>
        public Event(EventGridEvent eventGridEvent)
        {
            Guard.NotNull(eventGridEvent, nameof(eventGridEvent));

            _eventGridEvent = eventGridEvent;
        }

        /// <summary>
        /// Represent this model as a <see cref="CloudEvent"/> or <c>null</c>.
        /// </summary>
        public CloudEvent AsCloudEvent()
        {
            if (_cloudEvent is null)
            {
                throw new InvalidOperationException("Cannot transform this event to a Cloud Event because it is an Event Grid Event");
            }

            return _cloudEvent;
        }

        /// <summary>
        /// Represent this model as an <see cref="EventGridEvent"/> or <c>null</c>.
        /// </summary>
        public EventGridEvent AsEventGridEvent()
        {
            if (_eventGridEvent is null)
            {
                throw new InvalidOperationException("Cannot transform this event to an Event Grid Event because it is a Cloud Event");
            }

            return _eventGridEvent;
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
                if (_eventGridEvent is null)
                {
                    throw new InvalidOperationException(
                        "Cannot get the data version of this event because it represents a Cloud Event; which don't have any schema version of the data object information");
                }

                return _eventGridEvent.DataVersion;
            }
        }

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
        public string MetadataVersion
        {
            get
            {
                if (_eventGridEvent is null)
                {
                    throw new InvalidOperationException(
                        "Cannot get the meta-data version of this event because it represents a Cloud Event, which don't have any schema version of the event meta-data information");
                }
                
                return _eventGridEvent.MetadataVersion;
            }
        }

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
