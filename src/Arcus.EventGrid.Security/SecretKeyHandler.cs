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
    public class SecretKeyHandler : Attribute, IAuthenticationFilter
    {
        private  string _secretKey;
        private  string _secretKeyName;
        public bool AllowMultiple => false;

        public static Func<string> SecretKeyRetriever
        {
            private get;
            set;
        }

        public SecretKeyHandler(string secretKeyName = "x-api-key", string secretKeyValue = null)
        {
            _secretKeyName = secretKeyName;
            _secretKey = secretKeyValue;
        }

        public void ValidateConfiguration()
        {
            Guard.ForCondition(() => _secretKey != null || SecretKeyRetriever != null, "The secret key validation or value should be provided");
            _secretKey = _secretKey ?? SecretKeyRetriever();
            Guard.AgainstNullOrEmptyValue(_secretKey, "secretKey", "The secret key for the API was empty");
            Guard.AgainstNullOrEmptyValue(_secretKeyName, "secretKeyName", "The secret key name for the API was empty");
        }

        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            try
            {
                ValidateConfiguration();
                if (ValidateQueryStringKey(context) || ValidateHeaderKey(context))
                {
                    var currentPrincipal = new GenericPrincipal(new GenericIdentity("EventGrid"), null);
                    context.Principal = currentPrincipal;
                    return Task.CompletedTask;
                }
                context.ErrorResult = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                context.ErrorResult = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);
                return Task.CompletedTask;
            }
        }

        private bool ValidateHeaderKey(HttpAuthenticationContext context)
        {
            return context.Request.Headers.TryGetValues(_secretKeyName, out var headerValues) 
                   && headerValues.Contains(_secretKey);
        }

        private bool ValidateQueryStringKey(HttpAuthenticationContext context)
        {
            var queryStringParameters = context.Request.GetQueryNameValuePairs();
            if (queryStringParameters.Count(kvp => kvp.Key == _secretKeyName) > 0)
            {
                var secretKeyValue = context.Request.GetQueryNameValuePairs().First(kvp => kvp.Key.Equals(_secretKeyName));
                return secretKeyValue.Value == _secretKey;
            }

            return false;
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            context.Result = new ResultWithChallenge(context.Result);
            return Task.FromResult(0);
        }
    }
}