using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Owin.Testing;
using Xunit;
using System.Threading;
using System.Threading.Tasks;
using Arcus.EventGrid.Security;
using Newtonsoft.Json.Linq;

namespace Arcus.EventGrid.Tests.Security
{
    [Collection(TestCollections.ApiTests)]
    public class WebhookValidationTests
    {
        [Fact]
        public async Task TestWebhookValidationResponse()
        {
            using (var server = TestServer.Create<TestStartup>())
            {
                var gridMessage = EventGridMessage<SubscriptionEventData>.Parse(TestArtifacts.SubscriptionValidationEvent);
                var response = await server.CreateRequest($"/events/test")
                    .And(message =>
                    {
                        message.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(TestArtifacts.SubscriptionValidationEvent));
                    })
                    .AddHeader("x-api-key", "event-grid")
                    .AddHeader("Aeg-Event-Type", "SubscriptionValidation")
                    .PostAsync();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                if (response.IsSuccessStatusCode )
                {
                    var responseObject = JObject.Parse(await response.Content.ReadAsStringAsync());
                    Assert.NotNull(responseObject["validationResponse"]);
                    Assert.Equal(gridMessage.Events[0]?.Data?.ValidationCode, responseObject["validationResponse"]);
                }
            }
        }
    }
}
