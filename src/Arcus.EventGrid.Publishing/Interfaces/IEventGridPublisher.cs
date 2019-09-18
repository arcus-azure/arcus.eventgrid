using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Contracts.Interfaces;

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
        ///     Publish a raw JSON payload as event
        /// </summary>
        /// <param name="eventId">Id of the event</param>
        /// <param name="eventType">Type of the event</param>
        /// <param name="eventBody">Body of the event</param>
        Task PublishRawAsync(string eventId, string eventType, string eventBody);

        /// <summary>
        ///     Publish a raw JSON payload as event
        /// </summary>
        /// <param name="eventId">Id of the event</param>
        /// <param name="eventType">Type of the event</param>
        /// <param name="eventBody">Body of the event</param>
        /// <param name="eventSubject">Subject of the event</param>
        /// <param name="dataVersion">Data version of the event body</param>
        /// <param name="eventTime">Time when the event occured</param>
        Task PublishRawAsync(string eventId, string eventType, string eventBody, string eventSubject, string dataVersion, DateTimeOffset eventTime);

        /// <summary>
        ///     Publish a raw JSON payload as event
        /// </summary>
        /// <param name="rawEvent">The event to publish</param>
        Task PublishRawAsync(RawEvent rawEvent);

        /// <summary>
        ///     Publish an event grid message
        /// </summary>
        /// <typeparam name="TEvent">Type of the specific event</typeparam>
        /// <param name="event">Event to publish</param>
        Task PublishAsync<TEvent>(TEvent @event) where TEvent : class, IEvent;

        /// <summary>
        ///     Publish a many raw JSON payload as events
        /// </summary>
        /// <param name="rawEvents">The events to publish.</param>
        Task PublishManyRawAsync(IEnumerable<RawEvent> rawEvents);

        /// <summary>
        ///     Publish an event grid message
        /// </summary>
        /// <typeparam name="TEvent">Type of the specific event</typeparam>
        /// <param name="events">Events to publish</param>
        Task PublishManyAsync<TEvent>(IEnumerable<TEvent> events) where TEvent : class, IEvent;
    }
}