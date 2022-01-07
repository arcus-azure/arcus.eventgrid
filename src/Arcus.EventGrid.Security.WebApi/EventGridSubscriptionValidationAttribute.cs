using System;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.EventGrid.Security.WebApi
{
    /// <summary>
    /// Represents an attribute to validate Azure Event Grid subscription events.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class EventGridSubscriptionValidationAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridSubscriptionValidationAttribute"/> class.
        /// </summary>
        public EventGridSubscriptionValidationAttribute() : base(typeof(EventGridSubscriptionValidationActionFilter))
        {
        }
    }
}
