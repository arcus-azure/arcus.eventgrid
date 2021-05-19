using System;
using Arcus.EventGrid.Publishing.Interfaces;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.EventGrid.Publishing
{
    /// <summary>
    /// Represents a builder to create <see cref="IEventGridPublisher"/> implementations in a fluent manner.
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
        private readonly ILogger _logger;
        private readonly Action<EventGridPublisherOptions> _configureOptions;
        
        private EventGridPublisherBuilder(
            Uri topicEndpoint,
            ILogger logger,
            Action<EventGridPublisherOptions> configureOptions)
        {
            Guard.NotNull(topicEndpoint, nameof(topicEndpoint), "The topic endpoint must be specified");
            Guard.For<UriFormatException>(
                () => topicEndpoint.Scheme != Uri.UriSchemeHttp
                      && topicEndpoint.Scheme != Uri.UriSchemeHttps,
                $"The topic endpoint must be and HTTP or HTTPS endpoint but is: {topicEndpoint.Scheme}");

            _topicEndpoint = topicEndpoint;
            _logger = logger ?? NullLogger.Instance;
            _configureOptions = configureOptions;
        }

        /// <summary>
        /// Specifies the custom Event Grid <paramref name="topicEndpoint"/> for which a <see cref="IEventGridPublisher"/> will be created.
        /// </summary>
        /// <param name="topicEndpoint">The URL of the custom Event Grid topic.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicEndpoint"/> is blank.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="topicEndpoint"/> is not a correct URL.</exception>
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
            Guard.NotNullOrWhitespace(topicEndpoint, nameof(topicEndpoint), "Requires a non-blank topic endpoint for the Azure Event Grid publisher");
            Guard.For(() => Uri.IsWellFormedUriString(topicEndpoint, UriKind.Absolute) == false, 
                new ArgumentException("Requires a URI-valid topic endpoint for the Azure Event Grid publisher", nameof(topicEndpoint)));

            return ForTopic(topicEndpoint, NullLogger.Instance);
        }
        
        /// <summary>
        /// Specifies the custom Event Grid <paramref name="topicEndpoint"/> for which a <see cref="IEventGridPublisher"/> will be created.
        /// </summary>
        /// <param name="topicEndpoint">The URL of the custom Event Grid topic.</param>
        /// <param name="logger">The logger instance to write dependency telemetry during the interaction with the Azure Event Grid resource.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicEndpoint"/> is blank.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="topicEndpoint"/> is not a correct URL.</exception>
        /// <example>
        /// Shows how a <see cref="EventGridPublisher"/> should be created.
        /// <code>
        /// EventGridPublisher myPublisher =
        ///     EventGridPublisherBuilder
        ///         .ForTopic("http://myTopic", logger)
        ///         .UsingAuthenticationKey("myAuthenticationKey")
        ///         .Build();
        /// </code>
        /// </example>
        public static EventGridPublisherBuilder ForTopic(string topicEndpoint, ILogger logger)
        {
            Guard.NotNullOrWhitespace(topicEndpoint, nameof(topicEndpoint), "Requires a non-blank topic endpoint for the Azure Event Grid publisher");
            Guard.For(() => Uri.IsWellFormedUriString(topicEndpoint, UriKind.Absolute) == false, 
                new ArgumentException("Requires a URI-valid topic endpoint for the Azure Event Grid publisher", nameof(topicEndpoint)));

            return ForTopic(topicEndpoint, logger, configureOptions: null);
        }
        
        /// <summary>
        /// Specifies the custom Event Grid <paramref name="topicEndpoint"/> for which a <see cref="IEventGridPublisher"/> will be created.
        /// </summary>
        /// <param name="topicEndpoint">The URL of the custom Event Grid topic.</param>
        /// <param name="logger">The logger instance to write dependency telemetry during the interaction with the Azure Event Grid resource.</param>
        /// <param name="configureOptions">The additional function to configure optional settings on the <see cref="IEventGridPublisher"/>.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicEndpoint"/> is blank.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="topicEndpoint"/> is not a correct URL.</exception>
        /// <example>
        /// Shows how a <see cref="EventGridPublisher"/> should be created.
        /// <code>
        /// EventGridPublisher myPublisher =
        ///     EventGridPublisherBuilder
        ///         .ForTopic("http://myTopic", logger)
        ///         .UsingAuthenticationKey("myAuthenticationKey")
        ///         .Build();
        /// </code>
        /// </example>
        public static EventGridPublisherBuilder ForTopic(string topicEndpoint, ILogger logger, Action<EventGridPublisherOptions> configureOptions)
        {
            Guard.NotNullOrWhitespace(topicEndpoint, nameof(topicEndpoint), "Requires a non-blank topic endpoint for the Azure Event Grid publisher");
            Guard.For(() => Uri.IsWellFormedUriString(topicEndpoint, UriKind.Absolute) == false, 
                new ArgumentException("Requires a URI-valid topic endpoint for the Azure Event Grid publisher", nameof(topicEndpoint)));

            var topicEndpointUri = new Uri(topicEndpoint);
            Guard.For(() => topicEndpointUri.Scheme != Uri.UriSchemeHttp 
                            && topicEndpointUri.Scheme != Uri.UriSchemeHttps,
                new ArgumentException("Requires a topic endpoint that has a HTTP or HTTPS scheme", nameof(topicEndpointUri)));

            return ForTopic(topicEndpointUri, logger, configureOptions);
        }

        /// <summary>
        /// Specifies the custom Event Grid <paramref name="topicEndpoint"/> for which a <see cref="IEventGridPublisher"/> will be created.
        /// </summary>
        /// <param name="topicEndpoint">The URL of the custom Event Grid topic.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicEndpoint"/> is blank.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="topicEndpoint"/> is not a correct URL.</exception>
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
            Guard.NotNull(topicEndpoint, nameof(topicEndpoint), "Requires a topic endpoint for the Azure Event Grid publisher");
            Guard.For(() => topicEndpoint.Scheme != Uri.UriSchemeHttp 
                            && topicEndpoint.Scheme != Uri.UriSchemeHttps,
                new UriFormatException("Requires a topic endpoint that has a HTTP or HTTPS scheme"));

            return ForTopic(topicEndpoint, NullLogger.Instance);
        }
        
        /// <summary>
        /// Specifies the custom Event Grid <paramref name="topicEndpoint"/> for which a <see cref="IEventGridPublisher"/> will be created.
        /// </summary>
        /// <param name="topicEndpoint">The URL of the custom Event Grid topic.</param>
        /// <param name="logger">The logger instance to write dependency telemetry during the interaction with the Azure Event Grid resource.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicEndpoint"/> is blank.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="topicEndpoint"/> is not a correct URL.</exception>
        /// <example>
        /// Shows how a <see cref="EventGridPublisher"/> should be created.
        /// <code>
        /// EventGridPublisher myPublisher =
        ///     EventGridPublisherBuilder
        ///         .ForTopic("http://myTopic", logger)
        ///         .UsingAuthenticationKey("myAuthenticationKey")
        ///         .Build();
        /// </code>
        /// </example>
        public static EventGridPublisherBuilder ForTopic(Uri topicEndpoint, ILogger logger)
        {
            Guard.NotNull(topicEndpoint, nameof(topicEndpoint), "Requires a topic endpoint for the Azure Event Grid publisher");
            Guard.For(() => topicEndpoint.Scheme != Uri.UriSchemeHttp 
                            && topicEndpoint.Scheme != Uri.UriSchemeHttps,
                new UriFormatException("Requires a topic endpoint that has a HTTP or HTTPS scheme"));

            return ForTopic(topicEndpoint, logger, configureOptions: null);
        }

        /// <summary>
        /// Specifies the custom Event Grid <paramref name="topicEndpoint"/> for which a <see cref="IEventGridPublisher"/> will be created.
        /// </summary>
        /// <param name="topicEndpoint">The URL of the custom Event Grid topic.</param>
        /// <param name="logger">The logger instance to write dependency telemetry during the interaction with the Azure Event Grid resource.</param>
        /// <param name="configureOptions">The additional function to configure optional settings on the <see cref="IEventGridPublisher"/>.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicEndpoint"/> is blank.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="topicEndpoint"/> is not a correct URL.</exception>
        /// <example>
        /// Shows how a <see cref="EventGridPublisher"/> should be created.
        /// <code>
        /// EventGridPublisher myPublisher =
        ///     EventGridPublisherBuilder
        ///         .ForTopic("http://myTopic", logger, options => { })
        ///         .UsingAuthenticationKey("myAuthenticationKey")
        ///         .Build();
        /// </code>
        /// </example>
        public static EventGridPublisherBuilder ForTopic(Uri topicEndpoint, ILogger logger, Action<EventGridPublisherOptions> configureOptions)
        {
            Guard.NotNull(topicEndpoint, nameof(topicEndpoint), "Requires a topic endpoint for the Azure Event Grid publisher");
            Guard.For(() => topicEndpoint.Scheme != Uri.UriSchemeHttp 
                            && topicEndpoint.Scheme != Uri.UriSchemeHttps,
                new UriFormatException("Requires a topic endpoint that has a HTTP or HTTPS scheme"));

            return new EventGridPublisherBuilder(topicEndpoint, logger ?? NullLogger.Instance, configureOptions);
        }

        /// <summary>
        /// Specifies the <paramref name="authenticationKey"/> for the custom Event Grid topic for Which a <see cref="IEventGridPublisher"/> will be created.
        /// </summary>
        /// <param name="authenticationKey">The authentication key for the custom Azure Event Grid topic.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="authenticationKey"/> is blank.</exception>
        public IEventGridPublisherBuilderWithExponentialRetry UsingAuthenticationKey(string authenticationKey)
        {
            Guard.NotNullOrWhitespace(authenticationKey, nameof(authenticationKey), "The authentication key must not be empty and is required");

            return new EventGridPublisherBuilderResult(_topicEndpoint, authenticationKey, _logger, _configureOptions);
        }
    }
}
