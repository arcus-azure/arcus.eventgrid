using System.Collections.Generic;
using Arcus.EventGrid.Contracts.Interfaces;
using GuardNet;

namespace Arcus.EventGrid
{
    public class EventGridMessage<TEvent> where TEvent : IEvent, new()
    {
        /// <summary>
        /// Creates event grid message with passed session Id
        /// </summary>
        /// <param name="sessionId">Unique session id for all batched messages</param>
        public EventGridMessage(string sessionId)
        {
            Guard.NotNullOrWhitespace(sessionId, nameof(sessionId));

            SessionId = sessionId;
        }

        /// <summary>
        ///     Unique session id for all batched messages
        /// </summary>
        public string SessionId { get; }

        /// <summary>
        ///     List of all events, belonging to the Event Grid message
        /// </summary>
        public List<TEvent> Events { get; } = new List<TEvent>();
    }
}