using System;
using System.Linq;
using System.Threading.Tasks;
using Arcus.EventGrid.Security.Core.Validation;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Arcus.EventGrid.Security.WebApi
{
    /// <summary>
    /// Represents a filter to validate Azure Event Grid subscription events.
    /// </summary>
    internal class EventGridSubscriptionValidationActionFilter : IAsyncActionFilter
    {
       /// <summary>
        /// Called asynchronously before the action, after model binding is complete.
        /// </summary>
        /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext" />.</param>
        /// <param name="next">
        /// The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate" />. Invoked to execute the next action filter or the action itself.
        /// </param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that on completion indicates the filter has executed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="context"/> or <paramref name="next"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="context"/> doesn't contain the necessary information to validate the Azure Event Grid subscription.
        /// </exception>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            Guard.NotNull(context, nameof(context), "Requires an action filter HTTP context to validate the Azure Event Grid subscription");
            Guard.NotNull(next, nameof(next), "Requires an action delegate function to run the next action filter or the action itself when the Azure Event Grid subscription validation is complete");
            Guard.For(() => context.HttpContext is null, new ArgumentException("Requires a HTTP context in the action filter context to validate the Azure Event Grid subscription", nameof(context)));
            Guard.For(() => context.HttpContext.Request is null, new ArgumentException("Requires a HTTP context with a HTTP request in the action filter context to validate the Azure Event Grid subscription", nameof(context)));
            Guard.For(() => context.HttpContext.Request.Headers is null, new ArgumentException("Requires a HTTP context with a HTTP request containing headers in the action filter context to validate the Azure Event Grid subscription", nameof(context)));
            Guard.For(() => context.HttpContext.Request.Body is null, new ArgumentException("Requires a HTTP context with a HTTP request containing a body in the action filter context to validate the Azure Event Grid subscription", nameof(context)));
            Guard.For(() => context.HttpContext.Response is null, new ArgumentException("Requires a HTTP context in the action filter context with a response to assign CloudEvent validation information on completion", nameof(context)));
            Guard.For(() => context.HttpContext.Response.Headers is null, new ArgumentException("Requires a HTTP context in the action filter context with response headers to assign CloudEvent validation information on completion", nameof(context)));

            IEventGridSubscriptionValidator validator = GetRegisteredValidator(context.HttpContext.RequestServices);
            ILogger logger = GetRegisteredLogger(context.HttpContext.RequestServices);

            // CloudEvents use HTTP OPTIONS to perform validation handshake.
            if (HttpMethods.IsOptions(context.HttpContext.Request.Method))
            {
                logger.LogTrace("Validate incoming HTTP request as CloudEvents request because the HTTP method = OPTIONS");
                
                IActionResult result = validator.ValidateCloudEventsHandshakeRequest(context.HttpContext.Request);
                context.Result = result;
            }
            else
            {
                // TODO: configurable header name/value
                // EventGrid scheme uses Aeg-Event-Type: SubscriptionValidation to perform validation handshake.
                const string headerName = "Aeg-Event-Type", headerValue = "SubscriptionValidation";
                if (context.HttpContext.Request.Headers.TryGetValue(headerName, out StringValues eventTypes)
                    && eventTypes.Contains(headerValue))
                {
                    logger.LogTrace("Validate incoming HTTP request as EventGrid subscription event request because the HTTP request header '{HeaderName}' contains '{HeaderValue}'", headerName, headerValue);

                    IActionResult result = await validator.ValidateEventGridSubscriptionEventRequestAsync(context.HttpContext.Request);
                    context.Result = result;
                }
                else
                {
                    await next();
                }
            }
        }

       private static IEventGridSubscriptionValidator GetRegisteredValidator(IServiceProvider serviceProvider)
       {
           return serviceProvider.GetService<IEventGridSubscriptionValidator>()
               ?? ActivatorUtilities.CreateInstance<EventGridSubscriptionValidator>(serviceProvider);
       }

       private static ILogger GetRegisteredLogger(IServiceProvider serviceProvider)
        {
            return serviceProvider?.GetService<ILogger<EventGridSubscriptionValidationActionFilter>>() 
                   ?? NullLogger<EventGridSubscriptionValidationActionFilter>.Instance;
        }
    }
}
