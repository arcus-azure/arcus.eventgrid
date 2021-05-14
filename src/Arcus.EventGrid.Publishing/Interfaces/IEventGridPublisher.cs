using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts.Interfaces;
using CloudNative.CloudEvents;

namespace Arcus.EventGrid.Publishing.Interfaces
{
    /// <summary>
    /// Represents the contract for Azure Event Grid publisher implementations which are the result of the <see cref="EventGridPublisherBuilder" />.
    /// </summary>
    public interface IEventGridPublisher
    {
        /// <summary>
        ///  Gets the URL of the custom Azure Event Grid topic.
        /// </summary>
        string TopicEndpoint { get; }


        /// <summary>
        /// Publish a raw JSON payload as EventGrid event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="eventBody">The body of the event.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="eventId"/>, the <paramref name="eventType"/>, or the <paramref name="eventBody"/> is blank;
        ///     or the <paramref name="eventBody"/> is not a valid JSON payload.
        /// </exception>
        Task PublishRawEventGridEventAsync(string eventId, string eventType, string eventBody);

        /// <summary>
        /// Publish a raw JSON payload as EventGrid event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="eventBody">The body of the event.</param>
        /// <param name="eventSubject">The subject of the event.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="eventId"/>, the <paramref name="eventType"/>, or the <paramref name="eventBody"/> is blank;
        ///     or the <paramref name="eventBody"/> is not a valid JSON payload.
        /// </exception>
        Task PublishRawEventGridEventAsync(string eventId, string eventType, string eventBody, string eventSubject);
        
        /// <summary>
        /// Publish a raw JSON payload as EventGrid event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="eventBody">The body of the event.</param>
        /// <param name="eventSubject">The subject of the event.</param>
        /// <param name="dataVersion">The data version of the event body.</param>
        /// <param name="eventTime">The time when the event occurred.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="eventId"/>, the <paramref name="eventType"/>, the <paramref name="eventBody"/>, or the <paramref name="dataVersion"/> is blank;
        ///     or the <paramref name="eventBody"/> is not a valid JSON payload.
        /// </exception>
        Task PublishRawEventGridEventAsync(string eventId, string eventType, string eventBody, string eventSubject, string dataVersion, DateTimeOffset eventTime);

        /// <summary>
        /// Publish a raw JSON payload as CloudEvent event.
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
        /// Publish a raw JSON payload as CloudEvent event.
        /// </summary>
        /// <param name="specVersion">The version of the CloudEvents specification which the event uses.</param>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="source">The source that identifies the context in which an event happened.</param>
        /// <param name="eventBody">The body of the event.</param>
        /// <param name="eventSubject">The value that describes the subject of the event in the context of the event producer.</param>
        Task PublishRawCloudEventAsync(
            CloudEventsSpecVersion specVersion,
            string eventId,
            string eventType,
            Uri source,
            string eventBody,
            string eventSubject);

        /// <summary>
        /// Publish a raw JSON payload as CloudEvent event.
        /// </summary>
        /// <param name="specVersion">The version of the CloudEvents specification which the event uses.</param>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="source">The source that identifies the context in which an event happened.</param>
        /// <param name="eventBody">The body of the event.</param>
        /// <param name="eventSubject">The value that describes the subject of the event in the context of the event producer.</param>
        /// <param name="eventTime">The timestamp of when the occurrence happened.</param>
        Task PublishRawCloudEventAsync(
            CloudEventsSpecVersion specVersion,
            string eventId,
            string eventType,
            Uri source,
            string eventBody,
            string eventSubject,
            DateTimeOffset eventTime);

        /// <summary>
        /// Publish an Azure Event Grid event as CloudEvent.
        /// </summary>
        /// <param name="cloudEvent">The event to publish.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cloudEvent"/> is <c>null</c>.</exception>
        Task PublishAsync(CloudEvent cloudEvent);

        /// <summary>
        /// Publish many Azure Event Grid events as CloudEvents.
        /// </summary>
        /// <param name="events">The events to publish.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="events"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="events"/> is empty or contains <c>null</c> elements.</exception>
        Task PublishManyAsync(IEnumerable<CloudEvent> events);

        /// <summary>
        /// Publish an Azure Event Grid event.
        /// </summary>
        /// <typeparam name="TEvent">The type of the specific EventData.</typeparam>
        /// <param name="event">The event to publish.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="event"/> is <c>null</c>.</exception>
        Task PublishAsync<TEvent>(TEvent @event)
            where TEvent : class, IEvent;

        /// <summary>
        /// Publish an Azure Event Grid event.
        /// </summary>
        /// <typeparam name="TEvent">The type of the specific EventData.</typeparam>
        /// <param name="events">The events to publish.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="events"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="events"/> is empty or contains <c>null</c> elements.</exception>
        Task PublishManyAsync<TEvent>(IEnumerable<TEvent> events)
            where TEvent : class, IEvent;
    }
}