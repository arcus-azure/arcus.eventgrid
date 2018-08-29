using System;
using Arcus.EventGrid.Publishing.Interfaces;

namespace Arcus.EventGrid.Publishing
{
    using Guard;

    /// <summary>
    /// Result of the minimum required values to create <see cref="EventGridPublisher"/> instances, but also startpoint of extending the instance.
    /// The required and optional values are therefore split in separate classes and cannot be manipulated with casting.
    /// </summary>
    internal class EventGridPublisherBuilderResult : IBuilder
    {
        private readonly string _topicEndpoint;
        private readonly string _authenticationKey;
         
        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridPublisherBuilderResult"/> class.
        /// </summary>
        /// <param name="topicEndpoint">Url of the custom Event Grid topic</param>
        /// <param name="authenticationKey">Authentication key for the custom Event Grid topic</param>
        /// <exception cref="ArgumentException">The topic endpoint must not be empty and is required</exception>
        /// <exception cref="ArgumentException">The authentication key must not be empty and is required</exception>
        public EventGridPublisherBuilderResult(string topicEndpoint, string authenticationKey)
        {
            Guard.NotNullOrWhitespace(topicEndpoint, nameof(topicEndpoint), "The topic endpoint must not be empty and is required");
            Guard.NotNullOrWhitespace(authenticationKey, nameof(authenticationKey), "The authentication key must not be empty and is required");

            /* TODO:
             * shouldn't the `topicEndpoint` and the `authenticationKey` be domain models
             * instead of primitives so we can centrilize these validations (and possible others)
             * and not 'wait' till the 'Publish() throws? */

            _topicEndpoint = topicEndpoint;
            _authenticationKey = authenticationKey;
        }

        /// <summary>
        /// Creates a <see cref="EventGridPublisher"/> instance for the specified builder values.
        /// </summary>
        public EventGridPublisher Build()
        {
            return new EventGridPublisher(_topicEndpoint, _authenticationKey);
        }
    }
}
