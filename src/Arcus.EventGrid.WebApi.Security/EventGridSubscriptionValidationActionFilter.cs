using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Parsers;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Arcus.EventGrid.WebApi.Security
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
            
            ILogger logger = GetRegisteredLogger(context.HttpContext.RequestServices);

            if (HttpMethods.IsOptions(context.HttpContext.Request.Method))
            {
                context.Result = ValidateCloudEventsRequest(context.HttpContext.Request, context.HttpContext.Response, logger);
            }
            else
            {
                // TODO: configurable header name/value
                if (context.HttpContext.Request.Headers.TryGetValue("Aeg-Event-Type", out StringValues eventTypes)
                    && eventTypes.Contains("SubscriptionValidation"))
                {
                    context.Result = await ValidateEventGridSubscriptionEventAsync(context.HttpContext.Request, logger);
                }
                else
                {
                    await next();
                }
            }
        }

        private static IActionResult ValidateCloudEventsRequest(HttpRequest request, HttpResponse response, ILogger logger)
        {
            var headerName = "WebHook-Request-Origin";
            if (request.Headers.TryGetValue(headerName, out StringValues requestOrigins))
            {
                // TODO: configurable rate?
                response.Headers.Add("WebHook-Allowed-Rate", "*");
                response.Headers.Add("WebHook-Allowed-Origin", requestOrigins);
                
                return new OkResult();
            }

            logger.LogError("Invalid CloudEvents validation request due the missing '{HeaderName}' request header", headerName);
            return new BadRequestObjectResult("Invalid CloudEvents validation request");
        }

        private async Task<IActionResult> ValidateEventGridSubscriptionEventAsync(HttpRequest request, ILogger logger)
        {
            string json = await ReadRequestBodyAsync(request);
            EventBatch<Event> eventBatch = EventParser.Parse(json);

            // TODO: configurable event count: allow multiple events?
            // TODO: overridable for custom validation.
            if (eventBatch.Events.Count != 1)
            {
                logger.LogError("Cannot validate Azure Event Grid subscription because the HTTP request doesn't contains an single Event Grid event, but {EventCount} events", eventBatch.Events.Count);
                return new BadRequestObjectResult("Cannot validate Azure Event Grid subscription because the HTTP request doesn't contain an single Event Grid event");
            }

            Event subscriptionEvent = eventBatch.Events.Single();
            var validationEventData = subscriptionEvent.GetPayload<SubscriptionValidationEventData>();
                
            if (validationEventData?.ValidationCode is null)
            {
                logger.LogTrace("Cannot validate Azure Event Grid subscription because the HTTP request doesn't contain an Event Grid subscription validation event data");
                return new BadRequestObjectResult("Cannot validate Azure Event Grid subscription because the HTTP request doesn't contain an Event Grid subscription validation data");
            }

            var response = new SubscriptionValidationResponse(validationEventData.ValidationCode);
            return new OkObjectResult(response);
        }

        private static ILogger GetRegisteredLogger(IServiceProvider serviceProvider)
        {
            return serviceProvider?.GetService<ILogger<EventGridSubscriptionValidationActionFilter>>() 
                   ?? NullLogger<EventGridSubscriptionValidationActionFilter>.Instance;
        }
        
        private async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            using (var reader = new StreamReader(request.Body))
            {
                // TODO: use max buffer size option.
                string json = await reader.ReadToEndAsync();
                return json;
            }
        }
    }
}
