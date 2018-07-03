using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Arcus.EventGrid.Security
{
    public class ResultWithChallenge : IHttpActionResult
    {
        private const string AuthenticationScheme = "secretkey";
        private readonly IHttpActionResult _next;

        public ResultWithChallenge(IHttpActionResult next)
        {
            _next = next;
        }

        public async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = await _next.ExecuteAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var authenticationHeader = new AuthenticationHeaderValue(AuthenticationScheme);
                response.Headers.WwwAuthenticate.Add(authenticationHeader);
            }

            return response;
        }
    }
}