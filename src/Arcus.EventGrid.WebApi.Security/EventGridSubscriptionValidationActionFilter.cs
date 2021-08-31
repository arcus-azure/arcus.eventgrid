using System;
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
            
            ILogger logger = GetRegisteredLogger(context.HttpContext.RequestServices);
            
            // TODO: configurable header name/value
            if (context.HttpContext.Request.Headers.TryGetValue("Aeg-Event-Type", out StringValues eventTypes)
                && eventTypes.Contains("SubscriptionValidation"))
            {
                await ValidateEventGridSubscriptionEventAsync(context, logger);
            }
            else
            {
                await next();
            }
        }

        private async Task ValidateEventGridSubscriptionEventAsync(ActionExecutingContext context, ILogger logger)
        {
            string json = await ReadRequestBodyAsync(context.HttpContext.Request);
            EventBatch<Event> eventBatch = EventParser.Parse(json);

            // TODO: configurable event count: allow multiple events?
            // TODO: overridable for custom validation.
            if (eventBatch.Events.Count != 1)
            {
                logger.LogError("Cannot validate Azure Event Grid subscription because the HTTP request doesn't contains an single Event Grid event, but {EventCount} events", eventBatch.Events.Count);
                context.Result = new BadRequestObjectResult("Cannot validate Azure Event Grid subscription because the HTTP request doesn't contain an single Event Grid event");
            }
            else
            {
                Event subscriptionEvent = eventBatch.Events.Single();
                var validationEventData = subscriptionEvent.GetPayload<SubscriptionValidationEventData>();
                
                if (validationEventData?.ValidationCode is null)
                {
                    logger.LogTrace("Cannot validate Azure Event Grid subscription because the HTTP request doesn't contain an Event Grid subscription validation event data");
                    context.Result = new BadRequestObjectResult("Cannot validate Azure Event Grid subscription because the HTTP request doesn't contain an Event Grid subscription validation data");
                }
                else
                {
                    var response = new SubscriptionValidationResponse(validationEventData.ValidationCode);
                    context.Result = new OkObjectResult(response);
                }
            }
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
