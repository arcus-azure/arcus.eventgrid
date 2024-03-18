using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.EventGrid.Tests.Integration.WebApi.Fixture;
using Arcus.EventGrid.Tests.Integration.WebApi.Security.Controllers;
using Arcus.Testing.Logging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Bogus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.EventGrid.Tests.Integration.WebApi.Security
{
    [Trait("Category", "Integration")]
    [Collection(TestCollections.Integration)]
    public class EventGridSubscriptionValidationAttributeTests
    {
        private readonly ILogger _logger;

        private static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridSubscriptionValidationAttributeTests" /> class.
        /// </summary>
        public EventGridSubscriptionValidationAttributeTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task CloudEventsValidationRequest_WithWebHookRequestOriginHeader_Succeeds()
        {
            // Arrange
            string[] requestOrigins = BogusGenerator.Lorem.Words();
            await using (var server = await TestApiServer.StartNewAsync(_logger))
            {
                var request = HttpRequestBuilder
                    .Options(EventGridSubscriptionValidationController.GetSubscriptionValidationRoute)
                    .WithHeader("WebHook-Request-Origin", requestOrigins);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    
                    string actualOrigins = Assert.Single(response.Headers.GetValues("WebHook-Allowed-Origin"));
                    string expectedOrigins = String.Join(", ", requestOrigins);
                    Assert.Equal(expectedOrigins, actualOrigins);
                    
                    string actualRate = Assert.Single(response.Headers.GetValues("WebHook-Allowed-Rate"));
                    Assert.Equal("*", actualRate);
                }
            }
        }
        
        [Fact]
        public async Task CloudEventsValidationRequest_WithoutWebHooRequestOriginHeader_Fails()
        {
            // Arrange
            await using (var server = await TestApiServer.StartNewAsync(_logger))
            {
                var request = HttpRequestBuilder
                    .Options(EventGridSubscriptionValidationController.GetSubscriptionValidationRoute);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                }
            }
        }
        
        [Fact]
        public async Task CloudEventsValidationRequest_WithWrongWebHooRequestOriginHeader_Fails()
        {
            // Arrange
            string[] requestOrigins = BogusGenerator.Lorem.Words();
            await using (var server = await TestApiServer.StartNewAsync(_logger))
            {
                var request = HttpRequestBuilder
                    .Options(EventGridSubscriptionValidationController.GetSubscriptionValidationRoute)
                    .WithHeader("WebHook-Request-Origin-SomethingElse", requestOrigins);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                }
            }
        }
        
        [Fact]
        public async Task EventGridSubscriptionValidation_WithCorrectRequestValidationData_Succeeds()
        {
            // Arrange
            var validationCode = Guid.NewGuid().ToString();
            string json = $@"[
              {{
                ""id"": ""2d1781af-3a4c-4d7c-bd0c-e34b19da4e66"",
                ""topic"": ""/subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"",
                ""subject"": ""Sample.Subject"",
                ""data"": {{
                    ""validationCode"": ""{validationCode}""
                }}, ""dataVersion"": """",
                ""eventType"": ""Microsoft.EventGrid.SubscriptionValidationEvent"",
                ""eventTime"": ""2017-08-06T22:09:30.740323Z""
              }}
            ]";

            await using (var server = await TestApiServer.StartNewAsync(_logger))
            {
                var request = HttpRequestBuilder
                    .Get(EventGridSubscriptionValidationController.GetSubscriptionValidationRoute)
                    .WithHeader("Aeg-Event-Type", "SubscriptionValidation")
                    .WithJsonBody(json);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string contents = await response.Content.ReadAsStringAsync();
                    var validationResponse = JsonConvert.DeserializeObject<SubscriptionValidationResponse>(contents);
                    Assert.Equal(validationCode, validationResponse.ValidationResponse);
                }
            }
        }

        [Fact]
        public async Task EventGridSubscriptionValidation_WithMultipleEvents_Fails()
        {
            // Arrange
            var validationCode = Guid.NewGuid().ToString();
            var validationEvent = new EventGridEvent(
                subject: "Sample.Subect",
                data: JObject.Parse($"{{ \"validationCode\": \"{validationCode}\" }}"),
                eventType: "Microsoft.EventGrid.SubscriptionValidationEvent",
                dataVersion: "1.0")
            {
                Id = Guid.NewGuid().ToString(),
                EventTime = DateTimeOffset.UtcNow
            };
            var blobCreatedEvent = new EventGridEvent(
                subject: "Sample.Subject",
                data: JObject.Parse($"{{ \"something\": \"else\" }}"),
                eventType: "Microsoft.Storage.BlobCreatedEvent",
                dataVersion: "1.0")
            {
                Id = Guid.NewGuid().ToString(),
                EventTime = DateTimeOffset.UtcNow
            };
            EventGridEvent[] events = {validationEvent, blobCreatedEvent};
            string json = JsonConvert.SerializeObject(events);

            await using (var server = await TestApiServer.StartNewAsync(_logger))
            {
                var request = HttpRequestBuilder
                    .Get(EventGridSubscriptionValidationController.GetSubscriptionValidationRoute)
                    .WithHeader("Aeg-Event-Type", "SubscriptionValidation")
                    .WithJsonBody(json);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task EventGridSubscriptionValidation_WithWrongEventType_Fails()
        {
            // Arrange
            var blobCreatedEvent = new EventGridEvent(
                subject: "Sample.Subject",
                data: JObject.Parse($"{{ \"something\": \"else\" }}"),
                eventType: "Microsoft.Storage.BlobCreatedEvent",
                dataVersion: "1.0")
            {
                Id = Guid.NewGuid().ToString(),
                EventTime = DateTimeOffset.UtcNow
            };
            string json = JsonConvert.SerializeObject(blobCreatedEvent);

            await using (var server = await TestApiServer.StartNewAsync(_logger))
            {
                var request = HttpRequestBuilder
                    .Get(EventGridSubscriptionValidationController.GetSubscriptionValidationRoute)
                    .WithHeader("Aeg-Event-Type", "SubscriptionValidation")
                    .WithJsonBody(json);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData("Aeg-Event-Type", "Not-SubscriptionValidation")]
        [InlineData("Not-Aeg-Event-Type", "SubscriptionValidation")]
        [InlineData("Not-Aeg-Event-Type", "Not-SubscriptionValidation")]
        public async Task EventGridSubscriptionValidation_WithoutRequestHeader_SkipsValidation(string headerName, string headerValue)
        {
            // Arrange
            await using (var server = await TestApiServer.StartNewAsync(_logger))
            {
                var request = HttpRequestBuilder
                    .Get(EventGridSubscriptionValidationController.GetSubscriptionValidationRoute)
                    .WithHeader(headerName, headerValue);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
                }
            }
        }
    }
}
