using Arcus.Observability.Correlation;
using Arcus.Security.Core;
using Azure.Core.Extensions;
using Azure.Messaging.EventGrid;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Azure
{
    /// <summary>
    /// Extension on the <see cref="IAzureClientBuilder{TClient,TOptions}"/> to add <see cref="EventGridPublisherClient"/> instances with built-in correlation tracking.
    /// </summary>
    public static class AzureClientFactoryBuilderExtensions
    {
        /// <summary>
        /// Registers an <see cref="EventGridPublisherClient"/> instance with built-in correlation tracking on a given <paramref name="topicEndpoint"/>.
        /// </summary>
        /// <remarks>
        ///     <para>Make sure that the application has the Arcus secret store configured correctly.
        ///           For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.</para>
        /// </remarks>
        /// <param name="builder">The Azure builder to add the client to.</param>
        /// <param name="topicEndpoint">The Azure Event Grid topic endpoint to where the events should be published.</param>
        /// <param name="authenticationKeySecretName">The secret name where the authentication key to initiate Azure Event Grid interaction is stored in the Arcus secret store.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicEndpoint"/> or the <paramref name="authenticationKeySecretName"/> is blank.</exception>
        public static IAzureClientBuilder<EventGridPublisherClient, EventGridPublisherClientWithTrackingOptions> AddEventGridPublisherClient(
                this AzureClientFactoryBuilder builder,
                string topicEndpoint,
                string authenticationKeySecretName)
        {
            Guard.NotNull(builder, nameof(builder), "Requires an Azure builder instance to add the Azure Event Grid publisher with built-in correlation tracking");
            Guard.NotNullOrWhitespace(topicEndpoint, nameof(topicEndpoint), "Requires a non-blank Azure Event Grid topic endpoint to register the Azure Event Grid publisher with built-in correlation tracking");
            Guard.NotNullOrWhitespace(authenticationKeySecretName, nameof(authenticationKeySecretName), "Requires a non-blank authentication key to initiate interaction with the Azure Event Grid when registering the Azure Event Grid publisher with built-in correlation tracking");

            return builder.AddEventGridPublisherClient(topicEndpoint, authenticationKeySecretName, configureOptions: null);
        }

        /// <summary>
        /// Registers an <see cref="EventGridPublisherClient"/> instance with built-in correlation tracking on a given <paramref name="topicEndpoint"/>.
        /// </summary>
        /// <remarks>
        ///     <para>Make sure that the application has the Arcus secret store configured correctly.
        ///           For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.</para>
        /// </remarks>
        /// <param name="builder">The Azure builder to add the client to.</param>
        /// <param name="topicEndpoint">The Azure Event Grid topic endpoint to where the events should be published.</param>
        /// <param name="authenticationKeySecretName">The secret name where the authentication key to initiate Azure Event Grid interaction is stored in the Arcus secret store.</param>
        /// <param name="configureOptions">The function to configure additional options that influence the correlation tracking during event publishing to Azure Event Grid.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="topicEndpoint"/> or the <paramref name="authenticationKeySecretName"/> is blank.</exception>
        public static IAzureClientBuilder<EventGridPublisherClient, EventGridPublisherClientWithTrackingOptions> AddEventGridPublisherClient(
                this AzureClientFactoryBuilder builder,
                string topicEndpoint,
                string authenticationKeySecretName,
                Action<EventGridPublisherClientWithTrackingOptions> configureOptions)
        {
            Guard.NotNull(builder, nameof(builder), "Requires an Azure builder instance to add the Azure Event Grid publisher with built-in correlation tracking");
            Guard.NotNullOrWhitespace(topicEndpoint, nameof(topicEndpoint), "Requires a non-blank Azure Event Grid topic endpoint to register the Azure Event Grid publisher with built-in correlation tracking");
            Guard.NotNullOrWhitespace(authenticationKeySecretName, nameof(authenticationKeySecretName), "Requires a non-blank authentication key to initiate interaction with the Azure Event Grid when registering the Azure Event Grid publisher with built-in correlation tracking");

            return builder.AddEventGridPublisherClient(configureOptions, (provider, options) =>
            {
                var secretProvider = provider.GetService<ISecretProvider>();
                if (secretProvider is null)
                {
                    throw new InvalidOperationException(
                        "Requires an Arcus secret store registration to retrieve the authentication key to authenticate with Azure Event Grid while creating an Event Grid publisher instance");
                }

                var correlationAccessor = provider.GetService<ICorrelationInfoAccessor>();
                if (correlationAccessor is null)
                {
                    throw new InvalidOperationException(
                        "Requires an Arcus correlation registration to retrieve the current correlation model to enrich the send out event to Azure Event Grid");
                }

                ILogger<EventGridPublisherClient> logger =
                    provider.GetService<ILogger<EventGridPublisherClient>>()
                    ?? NullLogger<EventGridPublisherClient>.Instance;

                return new EventGridPublisherClientWithTracking(topicEndpoint, authenticationKeySecretName, secretProvider, correlationAccessor, options, logger);
            });
        }

        /// <summary>
        /// Registers an <see cref="EventGridPublisherClient"/> instance with built-in correlation tracking.
        /// </summary>
        /// <remarks>
        ///     <para>Make sure that the application has the Arcus secret store configured correctly.
        ///           For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.</para>
        /// </remarks>
        /// <param name="builder">The Azure builder to add the client to.</param>
        /// <param name="implementationFactory">The function to create an instance of the <see cref="EventGridPublisherClientWithTracking"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> or the <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static IAzureClientBuilder<EventGridPublisherClient, EventGridPublisherClientWithTrackingOptions> AddEventGridPublisherClient<TPublisherClient>(
                this AzureClientFactoryBuilder builder,
                Func<IServiceProvider, TPublisherClient> implementationFactory)
                where TPublisherClient : EventGridPublisherClientWithTracking
        {
            Guard.NotNull(builder, nameof(builder), "Requires an Azure builder instance to add the Azure Event Grid publisher with built-in correlation tracking");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires an implementation factory function to create the Azure Event Grid publisher instance");

            return builder.AddEventGridPublisherClient(configureOptions: null, (provider, options) => implementationFactory(provider));
        }

        /// <summary>
        /// Registers an <see cref="EventGridPublisherClient"/> instance with built-in correlation tracking.
        /// </summary>
        /// <remarks>
        ///     <para>Make sure that the application has the Arcus secret store configured correctly.
        ///           For more on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.</para>
        /// </remarks>
        /// <param name="builder">The Azure builder to add the client to.</param>
        /// <param name="configureOptions">The function to configure additional options that influence the correlation tracking during event publishing to Azure Event Grid.</param>
        /// <param name="implementationFactory">The function to create an instance of the <see cref="EventGridPublisherClientWithTracking"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> or the <paramref name="implementationFactory"/> is <c>null</c>.</exception>
        public static IAzureClientBuilder<EventGridPublisherClient, EventGridPublisherClientWithTrackingOptions> AddEventGridPublisherClient<TPublisherClient>(
            this AzureClientFactoryBuilder builder,
            Action<EventGridPublisherClientWithTrackingOptions> configureOptions,
            Func<IServiceProvider, EventGridPublisherClientWithTrackingOptions, TPublisherClient> implementationFactory)
            where TPublisherClient : EventGridPublisherClientWithTracking
        {
            Guard.NotNull(builder, nameof(builder), "Requires an Azure builder instance to add the Azure Event Grid publisher with built-in correlation tracking");
            Guard.NotNull(implementationFactory, nameof(implementationFactory), "Requires an implementation factory function to create the Azure Event Grid publisher instance");

            return builder.AddClient<EventGridPublisherClient, EventGridPublisherClientWithTrackingOptions>((options, provider) =>
            {
                configureOptions?.Invoke(options);
                return implementationFactory(provider, options);
            });
        }
    }
}