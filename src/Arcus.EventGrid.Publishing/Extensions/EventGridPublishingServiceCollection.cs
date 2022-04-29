using System;
using Arcus.EventGrid.Publishing.Interfaces;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Represents a specific <see cref="IServiceCollection"/> implementation to increase user-friendliness
    /// when registering additional decorating functionality to the <see cref="IEventGridPublisher"/>.
    /// </summary>
    public class EventGridPublishingServiceCollection
    {
        private readonly Func<IServiceProvider, IEventGridPublisher> _createPublisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridPublishingServiceCollection" /> class.
        /// </summary>
        /// <param name="services">The available registered application services.</param>
        /// <param name="createPublisher">The function to get a registered <see cref="IEventGridPublisher"/> from the registered application services.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public EventGridPublishingServiceCollection(IServiceCollection services, Func<IServiceProvider, IEventGridPublisher> createPublisher)
        {
            Guard.NotNull(services, nameof(services), "Requires an instance of the registered application service collection to register the Azure EventGrid publisher");

            _createPublisher = createPublisher;
            Services = services;
        }

        /// <summary>
        /// Gets the current available registered application services.
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Gets an <see cref="IEventGridPublisher"/> from the registered <paramref name="serviceProvider"/>.
        /// </summary>
        /// <param name="serviceProvider">The current provider of registered application services.</param>
        public IEventGridPublisher CreatePublisher(IServiceProvider serviceProvider)
        {
            return _createPublisher(serviceProvider);
        }
    }
}