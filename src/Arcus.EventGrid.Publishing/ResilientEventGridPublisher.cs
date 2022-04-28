using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts.Interfaces;
using Arcus.EventGrid.Publishing.Interfaces;
using CloudNative.CloudEvents;
using GuardNet;
using Polly;

namespace Arcus.EventGrid.Publishing
{
    /// <summary>
    /// Represents a decorated <see cref="IEventGridPublisher"/> that provides additional resilient functionality during the event publishing.
    /// </summary>
    internal class ResilientEventGridPublisher : IEventGridPublisher
    {
        private readonly AsyncPolicy _resilientPolicy;
        private readonly IEventGridPublisher _implementation;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResilientEventGridPublisher"/> class.
        /// </summary>
        /// <param name="resilientPolicy">The resilient policy run when publishing events.</param>
        /// <param name="implementation">The actual implementation that needs extra resilience.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="resilientPolicy"/> or the <paramref name="implementation"/> is <c>null</c>.</exception>
        internal ResilientEventGridPublisher(AsyncPolicy resilientPolicy, IEventGridPublisher implementation)
        {
            Guard.NotNull(resilientPolicy, nameof(resilientPolicy), "Requires a resilient policy when publishing events");
            Guard.NotNull(implementation, nameof(implementation), "Requires an Azure EventGrid publisher implementation to provide resilience");

            _resilientPolicy = resilientPolicy;
            _implementation = implementation;
        }

        /// <summary>
        ///  Gets the URL of the custom Azure Event Grid topic.
        /// </summary>
        public string TopicEndpoint => _implementation.TopicEndpoint;

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
        public async Task PublishRawEventGridEventAsync(string eventId, string eventType, string eventBody)
        {
            await _resilientPolicy.ExecuteAsync(() => _implementation.PublishRawEventGridEventAsync(eventId, eventType, eventBody));
        }

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
        public async Task PublishRawEventGridEventAsync(string eventId, string eventType, string eventBody, string eventSubject)
        {
            await _resilientPolicy.ExecuteAsync(() => _implementation.PublishRawEventGridEventAsync(eventId, eventType, eventBody, eventSubject));
        }

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
        public async Task PublishRawEventGridEventAsync(string eventId, string eventType, string eventBody, string eventSubject, string dataVersion, DateTimeOffset eventTime)
        {
            await _resilientPolicy.ExecuteAsync(() => _implementation.PublishRawEventGridEventAsync(eventId, eventType, eventBody, eventSubject, dataVersion, eventTime));
        }

        /// <summary>
        /// Publish a raw JSON payload as CloudEvent event.
        /// </summary>
        /// <param name="specVersion">The version of the CloudEvents specification which the event uses.</param>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="source">The source that identifies the context in which an event happened.</param>
        /// <param name="eventBody">The body of the event.</param>
        public async Task PublishRawCloudEventAsync(CloudEventsSpecVersion specVersion, string eventId, string eventType, Uri source, string eventBody)
        {
            await _resilientPolicy.ExecuteAsync(() => _implementation.PublishRawCloudEventAsync(specVersion, eventId, eventType, source, eventBody));
        }

        /// <summary>
        /// Publish a raw JSON payload as CloudEvent event.
        /// </summary>
        /// <param name="specVersion">The version of the CloudEvents specification which the event uses.</param>
        /// <param name="eventId">The unique identifier of the event.</param>
        /// <param name="eventType">The type of the event.</param>
        /// <param name="source">The source that identifies the context in which an event happened.</param>
        /// <param name="eventBody">The body of the event.</param>
        /// <param name="eventSubject">The value that describes the subject of the event in the context of the event producer.</param>
        public async Task PublishRawCloudEventAsync(CloudEventsSpecVersion specVersion, string eventId, string eventType, Uri source, string eventBody, string eventSubject)
        {
            await _resilientPolicy.ExecuteAsync(() => _implementation.PublishRawCloudEventAsync(specVersion, eventId, eventType, source, eventBody, eventSubject));
        }

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
        public async Task PublishRawCloudEventAsync(CloudEventsSpecVersion specVersion, string eventId, string eventType, Uri source, string eventBody, string eventSubject, DateTimeOffset eventTime)
        {
            await _resilientPolicy.ExecuteAsync(() => _implementation.PublishRawCloudEventAsync(specVersion, eventId, eventType, source, eventBody, eventSubject, eventTime));
        }

        /// <summary>
        /// Publish an Azure Event Grid event as CloudEvent.
        /// </summary>
        /// <param name="cloudEvent">The event to publish.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="cloudEvent"/> is <c>null</c>.</exception>
        public async Task PublishAsync(CloudEvent cloudEvent)
        {
            await _resilientPolicy.ExecuteAsync(() => _implementation.PublishAsync(cloudEvent));
        }

        /// <summary>
        /// Publish many Azure Event Grid events as CloudEvents.
        /// </summary>
        /// <param name="events">The events to publish.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="events"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="events"/> is empty or contains <c>null</c> elements.</exception>
        public async Task PublishManyAsync(IEnumerable<CloudEvent> events)
        {
            await _resilientPolicy.ExecuteAsync(() => _implementation.PublishManyAsync(events));
        }

        /// <summary>
        /// Publish an Azure Event Grid event.
        /// </summary>
        /// <typeparam name="TEvent">The type of the specific EventData.</typeparam>
        /// <param name="event">The event to publish.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="event"/> is <c>null</c>.</exception>
        public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class, IEvent
        {
            await _resilientPolicy.ExecuteAsync(() => _implementation.PublishAsync(@event));
        }

        /// <summary>
        /// Publish an Azure Event Grid event.
        /// </summary>
        /// <typeparam name="TEvent">The type of the specific EventData.</typeparam>
        /// <param name="events">The events to publish.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="events"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="events"/> is empty or contains <c>null</c> elements.</exception>
        public async Task PublishManyAsync<TEvent>(IEnumerable<TEvent> events) where TEvent : class, IEvent
        {
            await _resilientPolicy.ExecuteAsync(() => _implementation.PublishManyAsync(events));
        }
    }
}
