using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Testing;
using Xunit;

namespace Arcus.EventGrid.Tests.Security
{
    [Collection(TestCollections.ApiTests)]
    public class AuthorizationMethodTests
    {
        [Theory]
        [InlineData(false, null, null, HttpStatusCode.Unauthorized)]
        [InlineData(true, "x-api-key", "incorrect", HttpStatusCode.Unauthorized)]
        [InlineData(true, "incorrect", "event-grid", HttpStatusCode.Unauthorized)]
        [InlineData(true, "x-api-key", "event-grid", HttpStatusCode.OK)]
        public async Task TestQueryStringAuthorization(bool addQueryString, string name, string secret, HttpStatusCode expectedCode)
        {
            TestStartup.SecretKey = "event-grid";
            using (var server = TestServer.Create<TestStartup>())
            {
                var gridMessage = EventGridMessage<dynamic>.Parse(TestArtifacts.SubscriptionValidationEvent);
                var queryString = addQueryString ? $"?{name}={secret}" : "";
                var response = await server.CreateRequest($"/events/test{queryString}")
                    .And(message => { message.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(TestArtifacts.SubscriptionValidationEvent)); })
                    .PostAsync();
                if (!response.IsSuccessStatusCode && expectedCode != response.StatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                }

                Assert.Equal(expectedCode, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(false, null, null, HttpStatusCode.Unauthorized)]
        [InlineData(true, "x-api-key", "incorrect", HttpStatusCode.Unauthorized)]
        [InlineData(true, "incorrect", "event-grid", HttpStatusCode.Unauthorized)]
        [InlineData(true, "x-api-key", "event-grid", HttpStatusCode.OK)]
        public async Task TestHeaderStringAuthorization(bool addHeader, string name, string secret, HttpStatusCode expectedCode)
        {
            TestStartup.SecretKey = "event-grid";
            using (var server = TestServer.Create<TestStartup>())
            {
                var gridMessage = EventGridMessage<dynamic>.Parse(TestArtifacts.SubscriptionValidationEvent);
                var request = server.CreateRequest($"/events/test")
                    .And(message =>
                    {
                        message.Content =
                            new ByteArrayContent(Encoding.UTF8.GetBytes(TestArtifacts.SubscriptionValidationEvent));
                    });
                if (addHeader)
                {
                    request.AddHeader(name, secret);
                }
                var response = await request.PostAsync();
                if (!response.IsSuccessStatusCode && expectedCode != response.StatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                }

                Assert.Equal(expectedCode, response.StatusCode);
            }
        }
    }
}
