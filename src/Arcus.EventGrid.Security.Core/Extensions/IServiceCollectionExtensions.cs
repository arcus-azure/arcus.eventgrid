using System;
using Arcus.EventGrid.Security.Core.Validation;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the application services related to Azure EventGrid security.
    /// </summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Add <see cref="IEventGridSubscriptionValidator"/> validation to the application services.
        /// </summary>
        /// <param name="services">The available registered services.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        public static IServiceCollection AddEventGridSubscriptionValidation(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services), "Requires a set of registered services to add the Azure EventGrid subscription validation");

            services.AddSingleton<IEventGridSubscriptionValidator, EventGridSubscriptionValidator>();
            return services;
        }
    }
}
