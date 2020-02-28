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
        ///     Publish a raw JSON payload as EventGrid event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="eventBody">The body of the event.</param>
        Task PublishRawEventGridAsync(string eventId, string eventType, string eventBody);

        /// <summary>
        ///     Publish a raw JSON payload as EventGrid event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="eventBody">The body of the event.</param>
        /// <param name="eventSubject">The subject of the event.</param>
        /// <param name="dataVersion">The data version of the event body.</param>
        /// <param name="eventTime">The time when the event occured.</param>
        Task PublishRawEventGridAsync(string eventId, string eventType, string eventBody, string eventSubject, string dataVersion, DateTimeOffset eventTime);

        /// <summary>
        ///     Publish a raw JSON payload as CloudEvent event.
        /// </summary>
        /// <param name="specVersion">The version of the CloudEvents specification which the event uses.</param>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="source">The source that identifies the context in which an event happened.</param>
        /// <param name="eventBody">The body of the event.</param>
        Task PublishRawCloudEventAsync(
            CloudEventsSpecVersion specVersion,
            string eventId,
            string eventType,
            Uri source,
            string eventBody);

        /// <summary>
        ///     Publish a raw JSON payload as CloudEvent event.
        /// </summary>
        /// <param name="specVersion">The version of the CloudEvents specification which the event uses.</param>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="source">The source that identifies the context in which an event happened.</param>
        /// <param name="eventSubject">The value that describes the subject of the event in the context of the event producer.</param>
        /// <param name="eventBody">The body of the event.</param>
        /// <param name="eventTime">The timestamp of when the occurrence happened.</param>
        Task PublishRawCloudEventAsync(
            CloudEventsSpecVersion specVersion, 
            string eventId, 
            string eventType, 
            Uri source, 
            string eventSubject,
            string eventBody, 
            DateTimeOffset eventTime);

        /// <summary>
        ///     Publish an event grid message as CloudEvent.
        /// </summary>
        /// <param name="cloudEvent">The event to publish.</param>
        Task PublishAsync(CloudEvent cloudEvent);

        /// <summary>
        ///     Publish many event grid messages as CloudEvents.
        /// </summary>
        /// <param name="events">The events to publish.</param>
        Task PublishManyAsync(IEnumerable<CloudEvent> events);

        /// <summary>
        ///     Publish an event grid message
        /// </summary>
        /// <typeparam name="TEvent">The type of the specific EventData.</typeparam>
        /// <param name="event">The event to publish.</param>
        Task PublishAsync<TEvent>(TEvent @event)
            where TEvent : class, IEvent;

        /// <summary>
        ///     Publish an event grid message.
        /// </summary>
        /// <typeparam name="TEvent">The type of the specific EventData.</typeparam>
        /// <param name="events">The events to publish.</param>
        Task PublishManyAsync<TEvent>(IEnumerable<TEvent> events)
            where TEvent : class, IEvent;
    }
}