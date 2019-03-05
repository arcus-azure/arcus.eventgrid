using System;
using Arcus.EventGrid.Publishing.Interfaces;
using GuardNet;

namespace Arcus.EventGrid.Publishing
{
    /// <summary>
    /// Exposed builder to create <see cref="EventGridPublisher"/> instances in a fluent manner.
    /// </summary>
    /// <example>
    /// Shows how a <see cref="EventGridPublisher"/> should be created.
    /// <code>
    /// EventGridPublisher myPublisher =
    ///     EventGridPublisherBuilder
    ///         .ForTopic("http://myTopic")
    ///         .UsingAuthenticationKey("myAuthenticationKey")
    ///         .Build();
    /// </code>
    /// </example>
    public class EventGridPublisherBuilder : IEventGridPublisherBuilderWithAuthenticationKey
    {
        private readonly Uri _topicEndpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridPublisherBuilder"/> class.
        /// </summary>
        /// <param name="topicEndpoint">Url of the custom Event Grid topic</param>
        /// <exception cref="ArgumentException">The topic endpoint must not be empty and is required</exception>
        /// <exception cref="UriFormatException">The topic endpoint must be a HTTP endpoint.</exception>
        private EventGridPublisherBuilder(Uri topicEndpoint)
        {
            Guard.NotNull(topicEndpoint, nameof(topicEndpoint), "The topic endpoint must be specified");
            Guard.For<UriFormatException>(
                () => topicEndpoint.Scheme != Uri.UriSchemeHttp
                      && topicEndpoint.Scheme != Uri.UriSchemeHttps,
                $"The topic endpoint must be and HTTP or HTTPS endpoint but is: {topicEndpoint.Scheme}");

            _topicEndpoint = topicEndpoint;
        }

        /// <summary>
        /// Specifies the custom Event Grid <paramref name="topicEndpoint"/> for which a <see cref="EventGridPublisher"/> will be created.
        /// </summary>
        /// <param name="topicEndpoint">Url of the custom Event Grid topic</param>
        /// <exception cref="ArgumentException">The topic endpoint is required and must a valid URI</exception>
        /// <exception cref="UriFormatException">The topic endpoint must be a HTTP endpoint.</exception>
        /// <example>
        /// Shows how a <see cref="EventGridPublisher"/> should be created.
        /// <code>
        /// EventGridPublisher myPublisher =
        ///     EventGridPublisherBuilder
        ///         .ForTopic("http://myTopic")
        ///         .UsingAuthenticationKey("myAuthenticationKey")
        ///         .Build();
        /// </code>
        /// </example>
        public static EventGridPublisherBuilder ForTopic(string topicEndpoint)
        {
            Guard.NotNullOrWhitespace(topicEndpoint, nameof(topicEndpoint), "The topic endpoint must not be empty and is required");
            Guard.For<ArgumentException>(() => Uri.IsWellFormedUriString(topicEndpoint, UriKind.Absolute) == false, "The topic endpoint is not a valid URI");

            var topicEndpointUri = new Uri(topicEndpoint);
            Guard.For<ArgumentException>(() => topicEndpointUri.Scheme != Uri.UriSchemeHttp && topicEndpointUri.Scheme != Uri.UriSchemeHttps,
                $"The topic endpoint must be and HTTP or HTTPS endpoint but is: {topicEndpointUri.Scheme}");

            return ForTopic(topicEndpointUri);
        }

        /// <summary>
        ///     Specifies the custom Event Grid <paramref name="topicEndpoint"/> for which a <see cref="EventGridPublisher"/> will be created.
        /// </summary>
        /// <param name="topicEndpoint">Url of the custom Event Grid topic</param>
        /// <exception cref="ArgumentException">The topic endpoint must not be empty and is required</exception>
        /// <exception cref="UriFormatException">The topic endpoint must be a HTTP endpoint.</exception>
        /// <example>
        /// Shows how a <see cref="EventGridPublisher"/> should be created.
        /// <code>
        /// EventGridPublisher myPublisher =
        ///     EventGridPublisherBuilder
        ///         .ForTopic("http://myTopic")
        ///         .UsingAuthenticationKey("myAuthenticationKey")
        ///         .Build();
        /// </code>
        /// </example>
        public static EventGridPublisherBuilder ForTopic(Uri topicEndpoint)
        {
            Guard.NotNull(topicEndpoint, nameof(topicEndpoint), "The topic endpoint must be specified");
            Guard.For<UriFormatException>(
                () => topicEndpoint.Scheme != Uri.UriSchemeHttp
                      && topicEndpoint.Scheme != Uri.UriSchemeHttps,
                $"The topic endpoint must be and HTTP or HTTPS endpoint but is: {topicEndpoint.Scheme}");

            return new EventGridPublisherBuilder(topicEndpoint);
        }

        /// <summary>
        /// Specifies the <paramref name="authenticationKey"/> 
        /// for the custom Event Grid topic for Which a <see cref="EventGridPublisher"/> will be created.
        /// </summary>
        /// <param name="authenticationKey">Authentication key for the custom Event Grid topic</param>
        /// <exception cref="ArgumentException">The authentication key must not be empty and is required</exception>
        /// <returns>
        /// Finalized builder result that can directly create <see cref="EventGridPublisher"/> instances 
        /// via the <see cref="IBuilder.Build()"/> method or extend the publisher even further.
        /// </returns>
        /// <exception cref="ArgumentException">The authentication key must not be empty and is required.</exception>
        public IEventGridPublisherBuilderWithExponentialRetry UsingAuthenticationKey(string authenticationKey)
        {
            Guard.NotNullOrWhitespace(authenticationKey, nameof(authenticationKey), "The authentication key must not be empty and is required");

            return new EventGridPublisherBuilderResult(_topicEndpoint, authenticationKey);
        }
    }
}
