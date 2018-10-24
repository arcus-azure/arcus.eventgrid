using System.Collections.Generic;
using System.Threading.Tasks;
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
        ///     Publish an event grid message to the configured Event Grid topic
        /// </summary>
        /// <typeparam name="TEvent">Type of the specific event</typeparam>
        /// <param name="event">Event to publish</param>
        Task Publish<TEvent>(TEvent @event) where TEvent : class, IEvent, new();

        /// <summary>
        ///     Publish an event grid message to the configured Event Grid topic
        /// </summary>
        /// <typeparam name="TEvent">Type of the specific event</typeparam>
        /// <param name="events">Events to publish</param>
        Task PublishMany<TEvent>(IEnumerable<TEvent> events) where TEvent : class, IEvent, new();
    }
}