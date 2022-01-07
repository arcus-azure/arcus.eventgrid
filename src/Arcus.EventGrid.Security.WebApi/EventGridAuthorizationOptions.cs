namespace Arcus.EventGrid.Security.WebApi
{
    /// <summary>
    /// Represents the additional consumer-configurable options to influence the behavior of the Azure Event Grid authorization (<see cref="EventGridAuthorizationFilter"/>).
    /// </summary>
    public class EventGridAuthorizationOptions
    {
        /// <summary>
        /// Gets or sets the flag indicating whether or not the Azure Event Grid authorization should emit security events during the authorization of the request.
        /// </summary>
        public bool EmitSecurityEvents { get; set; } = false;
    }
}