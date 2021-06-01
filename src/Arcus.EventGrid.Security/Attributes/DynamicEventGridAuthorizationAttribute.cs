using System;
using System.Threading.Tasks;

namespace Arcus.EventGrid.Security.Attributes
{
    [Obsolete("Use the 'Arcus.EventGrid.WebApi.Security' package instead where the secret store is used to set the secret")]
    public class DynamicEventGridAuthorizationAttribute : EventGridAuthorizationAttribute
    {
        public static Func<Task<string>> RetrieveAuthenticationSecret { private get; set; }

        /// <summary>
        ///     Attribute that leverages authentication validation for api operations via query string or HTTP headers
        /// </summary>
        public DynamicEventGridAuthorizationAttribute(string authenticationKeyName = "x-api-key") : base(authenticationKeyName)
        {
        }

        protected override async Task<string> GetAuthenticationSecret()
        {
            // TODO: Add validation that the func is there

            var authenticationKey = await RetrieveAuthenticationSecret();
            return authenticationKey;
        }
    }
}