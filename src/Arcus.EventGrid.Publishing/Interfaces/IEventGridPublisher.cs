using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Contracts.Interfaces;
using CloudNative.CloudEvents;

namespace Arcus.EventGrid.Publishing.Interfaces
{
    /// <summary>
    ///     Contract for Event Grid publisher implementations which are the result of the <see cref="IBuilder.Build" />.
    /// </summary>
    public interface IEventGridPublisher
    {
        /// <summary>
        ///     Url of the custom Event Grid topic
        /// </summary>
        string TopicEndpoint { get; }

        /// <summary>
        ///     Publish an event grid message
        /// </summary>
        /// <param name="cloudEvent">Event to publish</param>
        Task PublishAsync(CloudEvent cloudEvent);

        /// <summary>
        ///     Publish an event grid message
        /// </summary>
        /// <param name="events">Events to publish</param>
        Task PublishManyAsync(IEnumerable<CloudEvent> events);

        /// <summary>
        ///     Publish a raw JSON payload as event
        /// </summary>
        /// <param name="eventId">Id of the event</param>
        /// <param name="eventType">Type of the event</param>
        /// <param name="eventBody">Body of the event</param>
        /// <param name="eventSchema">The schema in which the event should be published.</param>
        Task PublishRawAsync(string eventId, string eventType, string eventBody, EventSchema eventSchema = EventSchema.EventGrid);

        /// <summary>
        ///     Publish a raw JSON payload as event
        /// </summary>
        /// <param name="rawEvent">The event to publish</param>
        /// <param name="eventSchema">The schema in which the <paramref name="rawEvent"/> should be published.</param>
        Task PublishRawAsync(RawEvent rawEvent, EventSchema eventSchema = EventSchema.EventGrid);

        /// <summary>
        ///     Publish a many raw JSON payload as events
        /// </summary>
        /// <param name="rawEvents">The events to publish.</param>
        /// <param name="eventSchema">The schema in which the <paramref name="rawEvents"/> should be published.</param>
        Task PublishManyRawAsync(IEnumerable<RawEvent> rawEvents, EventSchema eventSchema = EventSchema.EventGrid);

        /// <summary>
        ///     Publish an event grid message
        /// </summary>
        /// <typeparam name="TEvent">Type of the specific EventData</typeparam>
        /// <param name="event">Event to publish</param>
        /// <param name="eventSchema">The schema in which the <paramref name="event"/> should be published.</param>
        Task PublishAsync<TEvent>(TEvent @event, EventSchema eventSchema = EventSchema.EventGrid)
            where TEvent : class, IEvent;

        /// <summary>
        ///     Publish an event grid message
        /// </summary>
        /// <typeparam name="TEvent">Type of the specific EventData</typeparam>
        /// <param name="events">Events to publish</param>
        /// <param name="eventSchema">The schema in which the <paramref name="events"/> should be published.</param>
        Task PublishManyAsync<TEvent>(IEnumerable<TEvent> events, EventSchema eventSchema = EventSchema.EventGrid)
            where TEvent : class, IEvent;

        /// <summary>
        ///     Publish a raw JSON payload as event
        /// </summary>
        /// <param name="eventId">Id of the event</param>
        /// <param name="eventType">Type of the event</param>
        /// <param name="eventBody">Body of the event</param>
        /// <param name="eventSubject">Subject of the event</param>
        /// <param name="dataVersion">Data version of the event body</param>
        /// <param name="eventTime">Time when the event occured</param>
        /// <param name="eventSchema">The schema in which the event should be published.</param>
        Task PublishRawAsync(string eventId, string eventType, string eventBody, string eventSubject, string dataVersion, DateTimeOffset eventTime, EventSchema eventSchema = EventSchema.EventGrid);
    }
}