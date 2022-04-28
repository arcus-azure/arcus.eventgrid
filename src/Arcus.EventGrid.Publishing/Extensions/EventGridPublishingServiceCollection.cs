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
        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridPublishingServiceCollection" /> class.
        /// </summary>
        /// <param name="services">The available registered application services.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public EventGridPublishingServiceCollection(IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services), "Requires an instance of the registered application service collection to register the Azure EventGrid publisher");
            Services = services;
        }

        /// <summary>
        /// Gets the current available registered application services.
        /// </summary>
        public IServiceCollection Services { get; }
    }
}