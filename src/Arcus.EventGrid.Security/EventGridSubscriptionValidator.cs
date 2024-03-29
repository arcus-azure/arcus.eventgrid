﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Arcus.EventGrid.Parsers;
using Microsoft.Azure.EventGrid.Models;

namespace Arcus.EventGrid.Security
{
    /// <inheritdoc />
    /// <summary>
    ///     Event grid web hook validation attribute
    /// </summary>
    [Obsolete("Use the 'Arcus.EventGrid.WebApi.Security' package instead where Azure EventGrid subscription validation happens via an attribute on class and/or operation level")]
    public class EventGridSubscriptionValidator : ActionFilterAttribute
    {
        /// <summary>
        /// Occurs before the action method is invoked.
        /// </summary>
        public override async Task OnActionExecutingAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            try
            {
                // This action only has to be executed, when the incoming contains a Header Aeg-Event-Type with value SubscriptionValidation
                if (actionContext.Request.Headers.TryGetValues(name: "Aeg-Event-Type", values: out var headerValues))
                {
                    //TODO : add logging
                    //Log.Logger.Information("Subscription validation message received by Action Filter");
                    if (headerValues.Contains(value: "SubscriptionValidation"))
                    {
                        var subscriptionValidationResponse = await HandleSubscriptionValidationEventAsync(actionContext);
                        if (subscriptionValidationResponse != null)
                        {
                            actionContext.Response = subscriptionValidationResponse;
                            return;
                        }
                    }
                }

                // When arriving here, the message was not a validation message, so let it handle by the pipeline
                await base.OnActionExecutingAsync(actionContext, cancellationToken);
            }
            catch (Exception)
            {
                //TODO : add logging
                //Log.Logger.Error(e, "error in filter");
                await base.OnActionExecutingAsync(actionContext, cancellationToken);
            }
        }

        private async Task<HttpResponseMessage> HandleSubscriptionValidationEventAsync(HttpActionContext actionContext)
        {
            // Parsing the incoming message to a typed EventGrid message
            var rawRequest = await actionContext.Request.Content.ReadAsStringAsync();
            var gridMessage = EventGridParser.ParseFromData<SubscriptionValidationEventData>(rawRequest);

            if (gridMessage.Events == null || gridMessage.Events.Any() == false)
            {
                throw new Exception(message: "No subscription events were found");
            }

            if (gridMessage.Events.Count > 1)
            {
                throw new Exception(message: "More than one subscription event was found");
            }

            var subscriptionEvent = gridMessage.Events.Single();

            var validationCode = subscriptionEvent.GetPayload()?.ValidationCode;
            if (validationCode == null)
            {
                return null;
            }

            // This is a subscription validation message, echo the validationCode
            var responseMessage = actionContext.Request.CreateResponse(
                HttpStatusCode.OK,
                new {validationResponse = validationCode},
                new JsonMediaTypeFormatter()
            );

            //TODO : add logging
            //Log.Logger.Information("Echoing back the validation code {validationCode}", validationCode.ToString());

            return responseMessage;
        }
    }
}