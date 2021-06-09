using System;
using Arcus.EventGrid.WebApi.Security;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// Extensions on the MVC <see cref="FilterCollection"/> to add Azure Event Grid related security filters.
    /// </summary>
    public static class FilterCollectionExtensions
    {
        /// <summary>
        /// Adds an Azure Event Grid authorization MVC filter to the given <paramref name="filters"/> that authenticates the incoming HTTP request.
        /// </summary>
        /// <param name="filters">The set of MVC filters to add the Azure Event Grid authorization.</param>
        /// <param name="requestProperty">The value indicating where the authorization secret should come from.</param>
        /// <param name="propertyName">The name of the <paramref name="requestProperty"/> that holds the authorization secret.</param>
        /// <param name="secretName">The name of the stored secret which value should match the request header or query parameter.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="requestProperty"/> is <c>null</c>.</exception>
        public static FilterCollection AddEventGridAuthorization(
            this FilterCollection filters,
            HttpRequestProperty requestProperty,
            string propertyName,
            string secretName)
        {
            Guard.NotNull(filters, nameof(filters), "Requires a set of MVC filter to add the Azure Event Grid authorization");
            Guard.NotNullOrWhitespace(propertyName, nameof(propertyName), "Requires a non-blank name for the request input");
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Requires a non-blank name for the secret");
            Guard.For(() => !Enum.IsDefined(typeof(HttpRequestProperty), requestProperty), 
                new ArgumentException("Requires the request input to be within the bounds of the enumeration", nameof(requestProperty)));

            return AddEventGridAuthorization(filters, requestProperty, propertyName, secretName, configureOptions: null);
        }
        
        /// <summary>
        /// Adds an Azure Event Grid authorization MVC filter to the given <paramref name="filters"/> that authenticates the incoming HTTP request.
        /// </summary>
        /// <param name="filters">The set of MVC filters to add the Azure Event Grid authorization.</param>
        /// <param name="requestProperty">The value indicating where the authorization secret should come from.</param>
        /// <param name="propertyName">The name of the <paramref name="requestProperty"/> that holds the authorization secret.</param>
        /// <param name="secretName">The name of the stored secret which value should match the request header or query parameter.</param>
        /// <param name="configureOptions">The additional consumer-configurable options to influence the behavior of the HTTP request authorization.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="requestProperty"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="secretName"/> is blank.</exception>
        public static FilterCollection AddEventGridAuthorization(
            this FilterCollection filters, 
            HttpRequestProperty requestProperty, 
            string propertyName,
            string secretName,
            Action<EventGridAuthorizationOptions> configureOptions)
        {
            Guard.NotNull(filters, nameof(filters), "Requires a set of MVC filter to add the Azure Event Grid authorization");
            Guard.NotNullOrWhitespace(propertyName, nameof(propertyName), "Requires a non-blank name for the request input");
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Requires a non-blank name for the secret");
            Guard.For(() => !Enum.IsDefined(typeof(HttpRequestProperty), requestProperty), 
                new ArgumentException("Requires the request input to be within the bounds of the enumeration", nameof(requestProperty)));

            var options = new EventGridAuthorizationOptions();
            configureOptions?.Invoke(options);

            filters.Add(new EventGridAuthorizationFilter(requestProperty, propertyName, secretName, options));
            return filters;
        }
    }
}
