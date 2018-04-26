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

namespace Arcus.EventGrid.Security
{
    /// <inheritdoc cref="IAuthenticationFilter"/>
    /// <summary>
    /// SecretKey handler attribute to use on Api operations for querystring & header validation
    /// </summary>
    public class SecretKeyHandler : Attribute, IAuthenticationFilter
    {
        private  string _secretKey;
        private readonly string _secretKeyName;
        public bool AllowMultiple => false;

        public static Func<string> SecretKeyRetriever
        {
            private get;
            set;
        }

        /// <inheritdoc />
        /// <summary>
        /// Attribute configuration
        /// </summary>
        /// <param name="secretKeyName">The unique name of the security querystring or header parameter</param>
        /// <param name="secretKeyValue">The hardcoded/compiled value of the security querystring or header parameter, it is advised to leverage the <see cref="P:Arcus.EventGrid.Security.SecretKeyHandler.SecretKeyRetriever" /> however</param>
        public SecretKeyHandler(string secretKeyName = "x-api-key", string secretKeyValue = null)
        {
            _secretKeyName = secretKeyName;
            _secretKey = secretKeyValue;
        }


        /// <summary>
        /// Validates the configured attribute
        /// </summary>
        public void ValidateConfiguration()
        {
            Guard.ForCondition(() => _secretKey != null || SecretKeyRetriever != null, "The secret key validation or value should be provided");
            _secretKey = _secretKey ?? SecretKeyRetriever();
            Guard.AgainstNullOrEmptyValue(_secretKey, "secretKey", "The secret key for the API was empty");
            Guard.AgainstNullOrEmptyValue(_secretKeyName, "secretKeyName", "The secret key name for the API was empty");
        }
        /// <inheritdoc cref="IAuthenticationFilter"/>
        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            try
            {
                // First validate the configuration of the Attribute
                ValidateConfiguration();
                if (ValidateQueryStringKey(context) || ValidateHeaderKey(context))
                {
                    // Either querystring or header validation succeeded, so setting a GenericIdentity and continuing
                    var currentPrincipal = new GenericPrincipal(new GenericIdentity("EventGrid"), null);
                    context.Principal = currentPrincipal;
                    return Task.CompletedTask;
                }
                // Unauthorized result
                context.ErrorResult = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);
                return Task.CompletedTask;
            }
            catch (Exception)
            {
                //TODO: add logging here
                context.ErrorResult = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);
                return Task.CompletedTask;
            }
        }

        private bool ValidateHeaderKey(HttpAuthenticationContext context)
        {
            // Check for a header that matches and is valid
            return context.Request.Headers.TryGetValues(_secretKeyName, out var headerValues) 
                   && headerValues.Contains(_secretKey);
        }

        private bool ValidateQueryStringKey(HttpAuthenticationContext context)
        {
            // Check for a query string value that matches and is valid
            var queryStringParameters = context.Request.GetQueryNameValuePairs();
            if (queryStringParameters.Count(kvp => kvp.Key == _secretKeyName) > 0)
            {
                var secretKeyValue = context.Request.GetQueryNameValuePairs().First(kvp => kvp.Key.Equals(_secretKeyName));
                return secretKeyValue.Value == _secretKey;
            }

            return false;
        }

        /// <inheritdoc cref="IAuthenticationFilter"/>
        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            context.Result = new ResultWithChallenge(context.Result);
            return Task.FromResult(0);
        }
    }
}