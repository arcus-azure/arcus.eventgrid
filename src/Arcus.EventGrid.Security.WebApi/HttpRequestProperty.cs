namespace Arcus.EventGrid.Security.WebApi
{
    /// <summary>
    /// Represents the available inputs for the <see cref="EventGridAuthorizationFilter"/> to authorize the HTTP request using Azure Event Grid.
    /// </summary>
    public enum HttpRequestProperty
    {
        /// <summary>
        /// Uses the HTTP request header to find the authorization secret.
        /// </summary>
        Header = 1,
        
        /// <summary>
        /// Uses the HTTP request query parameter to find the authorization secret.
        /// </summary>
        Query = 2
    }
}