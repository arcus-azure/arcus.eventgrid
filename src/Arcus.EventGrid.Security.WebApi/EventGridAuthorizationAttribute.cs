using System;
using GuardNet;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.EventGrid.WebApi.Security
{
    /// <summary>
    /// Represents an attribute to authorize HTTP requests using Azure Event Grid.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class EventGridAuthorizationAttribute : TypeFilterAttribute
    {
        private readonly EventGridAuthorizationOptions _options;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridAuthorizationAttribute"/> class.
        /// </summary>
        /// <param name="property">The value indicating where the authorization secret should come from.</param>
        /// <param name="propertyName">The name of the <paramref name="property"/> that holds the authorization secret.</param>
        /// <param name="secretName">The name of the stored secret which value should match the request header or query parameter.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="secretName"/> is blank.</exception>
        public EventGridAuthorizationAttribute(HttpRequestProperty property, string propertyName, string secretName) : base(typeof(EventGridAuthorizationFilter))
        {
            Guard.NotNullOrWhitespace(propertyName, nameof(propertyName), "Requires a non-blank name for the request input");
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Requires a non-blank name for the secret");
            Guard.For(() => !Enum.IsDefined(typeof(HttpRequestProperty), property), new ArgumentException("Requires the request input to be within the bounds of the enumeration", nameof(property)));

            _options = new EventGridAuthorizationOptions();
            Arguments = new object[] {property, propertyName, secretName, _options};
        }

        /// <summary>
        /// Gets or sets the flag indicating whether or not the Azure Event Grid authorization should emit security events during the authorization process.
        /// </summary>
        public bool EmitSecurityEvents
        {
            get => _options.EmitSecurityEvents;
            set => _options.EmitSecurityEvents = value;
        }
    }
}
