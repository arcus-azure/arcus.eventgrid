using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using GuardNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Arcus.EventGrid.WebApi.Security
{
    /// <summary>
    /// Represents a filter to authorize HTTP requests using Azure Event Grid.
    /// </summary>
    public class EventGridAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly HttpRequestProperty _property;
        private readonly string _propertyName, _secretName;
        private readonly EventGridAuthorizationOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridAuthorizationFilter" /> class.
        /// </summary>
        /// <param name="property">The value indicating where the authorization secret should come from.</param>
        /// <param name="propertyName">The name of the <paramref name="property"/> that holds the authorization secret.</param>
        /// <param name="secretName">The name of the stored secret which value should match the request header or query parameter.</param>
        /// <param name="options">The additional consumer-configurable options to influence the behavior of the HTTP request authorization.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="secretName"/> is blank.</exception>
        public EventGridAuthorizationFilter(HttpRequestProperty property, string propertyName, string secretName, EventGridAuthorizationOptions options)
        {
            Guard.NotNullOrWhitespace(propertyName, nameof(propertyName), "Requires a non-blank name for the request input");
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Requires a non-blank name for the secret");
            Guard.NotNull(options, nameof(options), "Requires a set of consumer-configurable options to influence the behavior of the Azure Event Grid authorization");
            Guard.For(() => !Enum.IsDefined(typeof(HttpRequestProperty), property), new ArgumentException("Requires the request input to be within the bounds of the enumeration", nameof(property)));
            
            _property = property;
            _propertyName = propertyName;
            _secretName = secretName;
            _options = options;
        }
        
        /// <summary>
        /// Called early in the filter pipeline to confirm request is authorized.
        /// </summary>
        /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext" />.</param>
        /// <returns>
        /// A <see cref="T:System.Threading.Tasks.Task" /> that on completion indicates the filter has executed.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="context"/> doesn't consists of all the correct information to run an Azure Event Grid authorization.</exception>
        /// <exception cref="SecretNotFoundException">Thrown when no secret value can be found to authorize the HTTP request.</exception>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            Guard.NotNull(context, nameof(context), "Requires a HTTP context to authorize the request using Azure Event Grid authorization");
            Guard.NotNull(context.HttpContext, nameof(context), "Requires a HTTP context to authorize the request using Azure Event Grid authorization");
            Guard.NotNull(context.HttpContext.RequestServices, nameof(context), "Requires a set of registered services to authorize the request using Azure Event Grid authorization");
            
            ILogger logger = GetRegisteredLogger(context.HttpContext.RequestServices);
            ISecretProvider secretProvider = GetRegisteredSecretProvider(context.HttpContext.RequestServices);

            string secretValue = await GetAuthorizationSecretAsync(secretProvider);

            if (!context.HttpContext.Request.Headers.ContainsKey(_propertyName)
                && !context.HttpContext.Request.Query.ContainsKey(_propertyName))
            {
                LogUnauthorizedSecurityEvent(logger, $"Cannot authorize request using Azure Event Grid authorization because neither a HTTP request header or query parameter was found with the name '{_propertyName}' in the incoming request");
                context.Result = new UnauthorizedObjectResult("Azure Event Grid authorization failed because no authorization key was found in the request");
            }
            else
            {
                if (HasAuthorizeRequestHeader(context, secretValue, logger) 
                    && HasAuthorizeRequestQuery(context, secretValue, logger))
                {
                    var genericIdentity = new GenericIdentity(name: "EventGrid");
                    var currentPrincipal = new GenericPrincipal(genericIdentity, roles: null);
                    
                    context.HttpContext.User = currentPrincipal;
                }
            }
        }

        private static ILogger GetRegisteredLogger(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<ILogger<EventGridAuthorizationFilter>>() 
                ?? NullLogger<EventGridAuthorizationFilter>.Instance;
        }
        
        private static ISecretProvider GetRegisteredSecretProvider(IServiceProvider serviceProvider)
        {
            ISecretProvider userDefinedSecretProvider =
                serviceProvider.GetService<ICachedSecretProvider>()
                ?? serviceProvider.GetService<ISecretProvider>();

            if (userDefinedSecretProvider is null)
            {
                throw new InvalidOperationException(
                    $"No configured {nameof(ICachedSecretProvider)} or {nameof(ISecretProvider)} implementation found in the request service container. "
                    + "Please configure such an implementation (ex. in the Startup) of your application");
            }

            return userDefinedSecretProvider;
        }

        private async Task<string> GetAuthorizationSecretAsync(ISecretProvider secretProvider)
        {
            Task<string> getSecret = secretProvider.GetRawSecretAsync(_secretName);
            if (getSecret is null)
            {
                return null;
            }

            string result = await getSecret;
            if (result is null)
            {
                throw new SecretNotFoundException(_secretName);
            }
            
            return result;
        }

        private bool HasAuthorizeRequestHeader(AuthorizationFilterContext context, string secretValue, ILogger logger)
        {
            if (!_property.HasFlag(HttpRequestProperty.Header))
            {
                logger.LogTrace("Azure Event Grid skipped: the HTTP request header is not configured for authorization");
                return true;
            }
            
            if (context.HttpContext.Request.Headers.TryGetValue(_propertyName, out StringValues headerValues))
            {
                if (headerValues.Any(headerValue => headerValue != secretValue))
                {
                    LogUnauthorizedSecurityEvent(logger, "Azure Event Grid authorization failed: the HTTP request header doesn't match the expected authorization key");
                    context.Result = new UnauthorizedObjectResult("Azure Event Grid authorization failed due to an invalid authorization key in the HTTP request");

                    return false;
                }

                LogSecurityEvent(logger, "Azure Event Grid authorization succeeds: the HTTP request header matches the expected authorization key");
                return true;
            }

            LogUnauthorizedSecurityEvent(logger, "Azure Event Grid authorization failed: the HTTP request header doesn't contain the expected authorization key");
            context.Result = new UnauthorizedObjectResult("Azure Event Grid authorization failed because no authorization key was found in the request");

            return false;
        }

        private bool HasAuthorizeRequestQuery(AuthorizationFilterContext context, string secretValue, ILogger logger)
        {
            if (!_property.HasFlag(HttpRequestProperty.Query))
            {
                logger.LogTrace("Azure Event Grid skipped: the HTTP request query is not configured for authorization");
                return true;
            }
            
            if (context.HttpContext.Request.Query.TryGetValue(_propertyName, out StringValues queryValues))
            {
                if (queryValues.Any(queryValue => queryValue != secretValue))
                {
                    LogUnauthorizedSecurityEvent(logger, "Azure Event Grid authorization failed: the HTTP request query parameter doesn't match the expected authorization key");
                    context.Result = new UnauthorizedObjectResult("Azure Event Grid authorization failed due to an invalid authorization key in the HTTP request");

                    return false;
                }

                LogSecurityEvent(logger, "Azure Event Grid authorization succeeds: the HTTP request header matches the expected authorization key");
                return true;
            }

            LogUnauthorizedSecurityEvent(logger, "Azure Event Grid authorization failed: the HTTP request query parameter doesn't contain the expected authorization key");
            context.Result = new UnauthorizedObjectResult("Azure Event Grid authorization failed because no authorization key was found in the request");

            return false;
        }

        private void LogUnauthorizedSecurityEvent(ILogger logger, string description)
        {
            LogSecurityEvent(logger, description, HttpStatusCode.Unauthorized);
        }

        private void LogSecurityEvent(ILogger logger, string description, HttpStatusCode? responseStatusCode = null)
        {
            if (!_options.EmitSecurityEvents)
            {
                return;
            }
            
            var context = new Dictionary<string, object>
            {
                ["AuthorizationType"] = "Event Grid",
                ["Description"] = description
            };

            if (responseStatusCode != null)
            {
                context["ResponseStatusCode"] = responseStatusCode.ToString();
            }

            logger.LogSecurityEvent("Authorization", context);
        }
    }
}
