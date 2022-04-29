using System;
using System.Net.Http;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using Arcus.Security.Core;
using GuardNet;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> related to Azure EventGrid event publishing.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an <see cref="IEventGridPublisher"/> implementation to the application <paramref name="services"/>.
        /// </summary>
        /// <param name="services">The registered application services available in the application.</param>
        /// <param name="topicEndpoint">The URL of the custom Azure EventGrid topic.</param>
        /// <param name="authenticationKeySecretName">
        ///     The secret name to the authentication key secret that's being used to interact with the Azure EventGrid topic (Uses <see cref="ISecretProvider"/>).
        /// </param>
        /// <remarks>
        ///     This extension requires the Arcus secret store to retrieve the authentication key for interaction with the Azure EventGrid topic.
        ///     For more information on the Arcus secret store, see: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicEndpoint"/> or <paramref name="authenticationKeySecretName"/> is blank.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="topicEndpoint"/> does not represent a valid URI.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no Arcus secret store is configured in the application.</exception>
        public static EventGridPublishingServiceCollection AddEventGridPublishing(
            this IServiceCollection services,
            string topicEndpoint,
            string authenticationKeySecretName)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of registered application service collection to register the Azure EventGrid publisher");
            Guard.NotNullOrWhitespace(topicEndpoint, nameof(topicEndpoint), "Requires a non-blank topic endpoint for the Azure EventGrid publisher");
            Guard.NotNullOrWhitespace(authenticationKeySecretName, nameof(authenticationKeySecretName), "Requires a non-blank secret name to the authentication key secret to interact with the Azure EventGrid topic");
            Guard.For(() => Uri.IsWellFormedUriString(topicEndpoint, UriKind.Absolute) == false,
                new UriFormatException("Requires a URI-valid topic endpoint for the Azure EventGrid publisher"));
            Guard.For<UriFormatException>(
                () => !topicEndpoint.StartsWith(Uri.UriSchemeHttp)
                      && !topicEndpoint.StartsWith(Uri.UriSchemeHttps),
                "Requires an Azure Event Grid topic endpoint that's either an HTTP or HTTPS endpoint");

            return AddEventGridPublishing(services, topicEndpoint, authenticationKeySecretName, configureOptions: null);
        }

        /// <summary>
        /// Adds an <see cref="IEventGridPublisher"/> implementation to the application <paramref name="services"/>.
        /// </summary>
        /// <param name="services">The registered application services available in the application.</param>
        /// <param name="topicEndpoint">The URL of the custom Azure EventGrid topic.</param>
        /// <param name="authenticationKeySecretName">
        ///     The secret name to the authentication key secret that's being used to interact with the Azure EventGrid topic (Uses <see cref="ISecretProvider"/>).
        /// </param>
        /// <param name="configureOptions">The function to configure additional options to manipulate the <see cref="IEventGridPublisher"/> operational workings.</param>
        /// <remarks>
        ///     This extension requires the Arcus secret store to retrieve the authentication key for interaction with the Azure EventGrid topic.
        ///     For more information on the Arcus secret store, see: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicEndpoint"/> or <paramref name="authenticationKeySecretName"/> is blank.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="topicEndpoint"/> does not represent a valid URI.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no Arcus secret store is configured in the application.</exception>
        public static EventGridPublishingServiceCollection AddEventGridPublishing(
            this IServiceCollection services, 
            string topicEndpoint, 
            string authenticationKeySecretName,
            Action<EventGridPublisherOptions> configureOptions)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of registered application service collection to register the Azure EventGrid publisher");
            Guard.NotNullOrWhitespace(topicEndpoint, nameof(topicEndpoint), "Requires a non-blank topic endpoint for the Azure EventGrid publisher");
            Guard.NotNullOrWhitespace(authenticationKeySecretName, nameof(authenticationKeySecretName), "Requires a non-blank secret name to the authentication key secret to interact with the Azure EventGrid topic");
            Guard.For(() => Uri.IsWellFormedUriString(topicEndpoint, UriKind.Absolute) == false,
                new UriFormatException("Requires a URI-valid topic endpoint for the Azure EventGrid publisher"));
            Guard.For<UriFormatException>(
                () => !topicEndpoint.StartsWith(Uri.UriSchemeHttp)
                      && !topicEndpoint.StartsWith(Uri.UriSchemeHttps),
                "Requires an Azure Event Grid topic endpoint that's either an HTTP or HTTPS endpoint");

            return AddEventGridPublishing(services, (serviceProvider, options) =>
            {
                var topicEndpointUri = new Uri(topicEndpoint);

                var secretProvider = serviceProvider.GetService<ISecretProvider>();
                if (secretProvider is null)
                {
                    throw new InvalidOperationException(
                        "Cannot add the Azure EventGrid publisher to the application services because no Arcus secret store was configured," +
                        "please make sure that you call '.ConfigureSecretStore' or '.AddSecretStore'; more information: https://security.arcus-azure.net/features/secret-store");
                }

                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var logger = serviceProvider.GetRequiredService<ILogger<EventGridPublisher>>();

                return new EventGridPublisher(topicEndpointUri, authenticationKeySecretName, secretProvider, httpClientFactory, options, logger);
            }, configureOptions);
        }

        /// <summary>
        /// Adds an <see cref="IEventGridPublisher"/> implementation to the application <paramref name="services"/>.
        /// </summary>
        /// <param name="services">The registered application services available in the application.</param>
        /// <param name="implementationFactory">The function to create an <see cref="IEventGridPublisher"/> implementation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or the <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static EventGridPublishingServiceCollection AddEventGridPublishing(
            this IServiceCollection services,
            Func<IServiceProvider, IEventGridPublisher> implementationFactory)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of registered application service collection to register the Azure EventGrid publisher");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires an implementation factory function to create a custom Azure EventGrid publisher");

            return AddEventGridPublishing(services, (serviceProvider, options) => implementationFactory(serviceProvider), configureOptions: null);
        }

        /// <summary>
        /// Adds an <see cref="IEventGridPublisher"/> implementation to the application <paramref name="services"/>.
        /// </summary>
        /// <param name="services">The registered application services available in the application.</param>
        /// <param name="implementationFactory">The function to create an <see cref="IEventGridPublisher"/> implementation.</param>
        /// <param name="configureOptions">The function to configure additional options to manipulate the <see cref="IEventGridPublisher"/> operational workings.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> or the <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static EventGridPublishingServiceCollection AddEventGridPublishing(
            this IServiceCollection services,
            Func<IServiceProvider, EventGridPublisherOptions, IEventGridPublisher> implementationFactory,
            Action<EventGridPublisherOptions> configureOptions)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of registered application service collection to register the Azure EventGrid publisher");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires an implementation factory function to create a custom Azure EventGrid publisher");

            Func<IServiceProvider, IEventGridPublisher> createPublisher = serviceProvider =>
            {
                var options = new EventGridPublisherOptions();
                configureOptions?.Invoke(options);

                return implementationFactory(serviceProvider, options);
            };

            services.AddHttpClient();
            services.AddSingleton(createPublisher);

            return new EventGridPublishingServiceCollection(services, createPublisher);
        }
    }
}
