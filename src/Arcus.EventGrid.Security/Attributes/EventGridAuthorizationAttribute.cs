using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using System.Web.Http.Results;

namespace Arcus.EventGrid.Security.Attributes
{
    /// <summary>
    ///     Attribute that leverages authentication validation for api operations via query string or HTTP headers
    /// </summary>
    public class EventGridAuthorizationAttribute : Attribute, IAuthenticationFilter
    {
        private readonly string _authenticationKeySecret;

        /// <summary>
        ///     Attribute that leverages authentication validation for api operations via query string or HTTP headers
        /// </summary>
        /// <param name="authenticationKeyName">The unique name of the security querystring or header parameter</param>
        /// <param name="authenticationKeySecret">The hardcoded/compiled value of the security querystring or header parameter</param>
        public EventGridAuthorizationAttribute(string authenticationKeyName, string authenticationKeySecret) : this(authenticationKeyName)
        {
            Guard.AgainstNullOrEmptyValue(authenticationKeySecret, nameof(authenticationKeySecret));

            _authenticationKeySecret = authenticationKeySecret;
        }

        /// <summary>
        ///     Attribute that leverages authentication validation for api operations via query string or HTTP headers
        /// </summary>
        /// <param name="authenticationKeyName">The unique name of the security querystring or header parameter</param>
        protected EventGridAuthorizationAttribute(string authenticationKeyName = "x-api-key")
        {
            Guard.AgainstNullOrEmptyValue(authenticationKeyName, nameof(authenticationKeyName));

            AuthenticationKeyName = authenticationKeyName;
        }

        /// <summary>
        ///     Allow multiple attributes in case multiple headers need to be supported
        /// </summary>
        public virtual bool AllowMultiple => true;

        /// <summary>
        ///     The unique name of the security querystring or header parameter
        /// </summary>
        protected string AuthenticationKeyName { get; }

        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            try
            {
                var authenticationKeySecret = await GetAuthenticationSecret();
                if (ValidateRequest(context, authenticationKeySecret))
                {
                    // Either querystring or header validation succeeded, so setting a GenericIdentity and continuing
                    var genericIdentity = new GenericIdentity(name: "EventGrid");
                    var currentPrincipal = new GenericPrincipal(genericIdentity, roles: null);
                    context.Principal = currentPrincipal;
                }
                else
                {
                    // Unauthorized result
                    context.ErrorResult = CreateUnauthorizedResult(context.Request);
                }
            }
            catch (Exception)
            {
                context.ErrorResult = CreateUnauthorizedResult(context.Request);
            }
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            context.Result = new ResultWithChallenge(context.Result);

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Provides the authentication secret that should be specified for authorized requests
        /// </summary>
        protected virtual Task<string> GetAuthenticationSecret()
        {
            return Task.FromResult(_authenticationKeySecret);
        }

        private UnauthorizedResult CreateUnauthorizedResult(HttpRequestMessage requestMessage)
        {
            var authenticationHeaderValue = new AuthenticationHeaderValue[0];
            return new UnauthorizedResult(authenticationHeaderValue, requestMessage);
        }

        private bool HasValidHeaderKey(HttpAuthenticationContext context, string authenticationKeySecret)
        {
            // Check for a header that matches and is valid
            return context.Request.Headers.TryGetValues(AuthenticationKeyName, out IEnumerable<string> headerValues)
                   && headerValues.Contains(authenticationKeySecret);
        }

        private bool HasValidQueryStringKey(HttpAuthenticationContext context, string authenticationKeySecret)
        {
            // Check for a query string value that matches and is valid
            IEnumerable<KeyValuePair<string, string>> queryStringParameters = context.Request.GetQueryNameValuePairs();
            if (queryStringParameters.Any(keyValuePair => keyValuePair.Key == AuthenticationKeyName) == false)
            {
                return false;
            }

            IEnumerable<KeyValuePair<string, string>> queryStringItems = context.Request.GetQueryNameValuePairs();
            KeyValuePair<string, string> secretKeyValue = queryStringItems.Single(keyValuePair => keyValuePair.Key.Equals(AuthenticationKeyName));

            return secretKeyValue.Value == authenticationKeySecret;
        }

        private bool ValidateRequest(HttpAuthenticationContext context, string authenticationKeySecret)
        {
            return HasValidQueryStringKey(context, authenticationKeySecret) || HasValidHeaderKey(context, authenticationKeySecret);
        }
    }
}