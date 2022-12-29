using System;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Arcus.EventGrid.Security.Core.Validation
{
    /// <summary>
    /// Represents a validation on events on Azure EventGrid, wrapped as HTTP requests.
    /// </summary>
    public class EventGridSubscriptionValidator : IEventGridSubscriptionValidator
    {
        private readonly ILogger<EventGridSubscriptionValidator> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridSubscriptionValidator" /> class.
        /// </summary>
        /// <param name="logger">The logger instance to write diagnostic and error messages during the event validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> is <c>null</c>.</exception>
        public EventGridSubscriptionValidator(ILogger<EventGridSubscriptionValidator> logger)
        {
            Guard.NotNull(logger, nameof(logger), "Requires an logger instance to write diagnostic and error messages during the event validation");
            _logger = logger;
        }

        /// <summary>
        /// CloudEvents validation handshake of the incoming HTTP <paramref name="request"/>.
        /// </summary>
        /// <param name="request">The incoming HTTP request that needs to be validated.</param>
        /// <returns>
        ///     An [OK] HTTP response that represents a successful result of the validation; [BadRequest] otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="request"/> is <c>null</c>.</exception>
        public IActionResult ValidateCloudEventsHandshakeRequest(HttpRequest request)
        {
            Guard.NotNull(request, nameof(request), "Requires a HTTP request to validate the CloudEvent handshake");

            var headerName = "WebHook-Request-Origin";
            if (request.Headers.TryGetValue(headerName, out StringValues requestOrigins))
            {
                // TODO: configurable rate?
                request.HttpContext.Response.Headers.Add("WebHook-Allowed-Rate", "*");
                request.HttpContext.Response.Headers.Add("WebHook-Allowed-Origin", requestOrigins);

                return new OkResult();
            }

            _logger.LogError("Invalid CloudEvents validation request due the missing '{HeaderName}' request header", headerName);
            return new BadRequestObjectResult("Invalid CloudEvents validation request");
        }

        /// <summary>
        /// Azure EventGrid subscription event validation of the incoming HTTP <paramref name="request"/>.
        /// </summary>
        /// <param name="request">The incoming HTTP request that needs to be validated.</param>
        /// <returns>
        ///     An [OK] HTTP response that represents a successful result of the validation; [BadRequest] otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="request"/> is <c>null</c>.</exception>
        public async Task<IActionResult> ValidateEventGridSubscriptionEventRequestAsync(HttpRequest request)
        {
            Guard.NotNull(request, nameof(request), "Requires a HTTP request to validate the Azure EventGrid subscription event");

            BinaryData data = await BinaryData.FromStreamAsync(request.Body);
            EventGridEvent subscriptionEvent = EventGridEvent.Parse(data);
            if (subscriptionEvent is null)
            {
                _logger.LogError("Cannot validate Azure Event Grid subscription in the HTTP request body because the request has no body");
                return new BadRequestObjectResult("Cannot validate Azure Event Grid subscription in the HTTP request body because the request has no body");
            }

            var validationEventData = subscriptionEvent.Data.ToObjectFromJson<SubscriptionValidationEventData>();
            if (validationEventData?.ValidationCode is null)
            {
                _logger.LogTrace("Cannot validate Azure Event Grid subscription because the HTTP request doesn't contain an Event Grid subscription validation event data");
                return new BadRequestObjectResult("Cannot validate Azure Event Grid subscription because the HTTP request doesn't contain an Event Grid subscription validation data");
            }

            var response = new SubscriptionValidationResponse()
            {
                ValidationResponse = validationEventData.ValidationCode
            };
            return new OkObjectResult(response);
        }
    }
}
