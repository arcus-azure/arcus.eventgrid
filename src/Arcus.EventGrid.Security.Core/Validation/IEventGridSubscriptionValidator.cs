using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.EventGrid.Security.Core.Validation
{
    /// <summary>
    /// Represents a validation on events on Azure EventGrid, wrapped as HTTP requests.
    /// </summary>
    public interface IEventGridSubscriptionValidator
    {
        /// <summary>
        /// CloudEvents validation handshake of the incoming HTTP <paramref name="request"/>.
        /// </summary>
        /// <param name="request">The incoming HTTP request that needs to be validated.</param>
        /// <returns>
        ///     An [OK] HTTP response that represents a successful result of the validation; [BadRequest] otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="request"/> is <c>null</c>.</exception>
        IActionResult ValidateCloudEventsHandshakeRequest(HttpRequest request);

        /// <summary>
        /// Azure EventGrid subscription event validation of the incoming HTTP <paramref name="request"/>.
        /// </summary>
        /// <param name="request">The incoming HTTP request that needs to be validated.</param>
        /// <returns>
        ///     An [OK] HTTP response that represents a successful result of the validation; [BadRequest] otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="request"/> is <c>null</c>.</exception>
        Task<IActionResult> ValidateEventGridSubscriptionEventRequestAsync(HttpRequest request);
    }
}
