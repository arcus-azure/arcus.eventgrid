using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Arcus.EventGrid.Parsers;
using Arcus.EventGrid.Security.Contracts.Events;
using Arcus.EventGrid.Tests.InMemoryApi;
using Arcus.EventGrid.Tests.Unit.Artifacts;
using Microsoft.Owin.Testing;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Security
{
    [Collection(TestCollections.ApiTests)]
    public class WebhookValidationTests
    {
        [Fact]
        public async Task Validate_HasValidEvent_ShouldSucceed()
        {
            // Arrange
            var gridMessage = EventGridParser.Parse<SubscriptionValidation>(EventSamples.SubscriptionValidationEvent);

            // Act
            using (var server = TestServer.Create<InMemoryTestApiStartup>())
            {
                var response = await server.CreateRequest("/events/test")
                    .And(message =>
                    {
                        message.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(EventSamples.SubscriptionValidationEvent));
                    })
                    .AddHeader("x-api-key", "event-grid")
                    .AddHeader("Aeg-Event-Type", "SubscriptionValidation")
                    .PostAsync();

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var responseObject = JObject.Parse(await response.Content.ReadAsStringAsync());
                    Assert.NotNull(responseObject["validationResponse"]);
                    Assert.Equal(gridMessage.Events[0]?.Data?.ValidationCode, responseObject["validationResponse"]);
                }
            }
        }
    }
}
