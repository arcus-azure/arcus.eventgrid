using System;
using Arcus.EventGrid.Security.Core.Validation;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.Functions.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the Azure Functions <see cref="IFunctionsHostBuilder"/> to add Azure EventGrid security related methods.
    /// </summary>
    public static class IFunctionsHostBuilderExtensions
    {
        /// <summary>
        /// Adds an <see cref="IEventGridSubscriptionValidator"/> to the Azure Functions application services.
        /// </summary>
        /// <param name="builder">The Azure Functions builder instance to add the Azure EventGrid subscription validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public static IFunctionsHostBuilder AddEventGridSubscriptionValidation(this IFunctionsHostBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder), "Requires an Azure Functions host builder to add the Azure EventGrid subscription validation");

            builder.Services.AddEventGridSubscriptionValidation();
            return builder;
        }
    }
}
