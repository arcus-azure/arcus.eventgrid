﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.EventGrid.Tests.Integration.WebApi.Fixture;
using Arcus.EventGrid.Tests.Integration.WebApi.Security.Controllers;
using Arcus.Testing.Logging;
using Bogus;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
            var validationEventData = new SubscriptionValidationEventData(validationCode: Guid.NewGuid().ToString());
            var eventGridEvent = new EventGridEvent(
                id: Guid.NewGuid().ToString(),
                subject: "Sample.Subect",
                data: validationEventData,
                eventType: "Microsoft.EventGrid.SubscriptionValidationEvent",
                eventTime: DateTime.Now,
                dataVersion: "1.0");
            string json = JsonConvert.SerializeObject(eventGridEvent);

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
                    Assert.Equal(validationEventData.ValidationCode, validationResponse.ValidationResponse);
                }
            }
        }

        [Fact]
        public async Task EventGridSubscriptionValidation_WithMultipleEvents_Fails()
        {
            // Arrange
            var validationEventData = new SubscriptionValidationEventData(validationCode: Guid.NewGuid().ToString());
            var validationEvent = new EventGridEvent(
                id: Guid.NewGuid().ToString(),
                subject: "Sample.Subect",
                data: validationEventData,
                eventType: "Microsoft.EventGrid.SubscriptionValidationEvent",
                eventTime: DateTime.Now,
                dataVersion: "1.0");
            var blobCreatedEvent = new EventGridEvent(
                id: Guid.NewGuid().ToString(),
                subject: "Sample.Subject",
                data: new StorageBlobCreatedEventData(),
                eventType: "Microsoft.Storage.BlobCreatedEvent",
                eventTime: DateTime.Now,
                dataVersion: "1.0");
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
                id: Guid.NewGuid().ToString(),
                subject: "Sample.Subject",
                data: new StorageBlobCreatedEventData(),
                eventType: "Microsoft.Storage.BlobCreatedEvent",
                eventTime: DateTime.Now,
                dataVersion: "1.0");
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
