using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Arcus.EventGrid.Security.Core.Validation;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using SubscriptionValidationResponse = Azure.Messaging.EventGrid.SystemEvents.SubscriptionValidationResponse;

namespace Arcus.EventGrid.Tests.Unit.Security
{
    public class EventGridSubscriptionValidatorTests
    {
        private static readonly ILogger<EventGridSubscriptionValidator> NullLogger = NullLogger<EventGridSubscriptionValidator>.Instance;

        [Fact]
        public void ValidateCloudEventsHandshakeRequest_WithWebRequestOriginHeaderInHttpRequest_Succeeds()
        {
            // Arrange
            var validator = new EventGridSubscriptionValidator(NullLogger);
            var context = new DefaultHttpContext();
            var expectedRequestOrigin = $"request-origin-{Guid.NewGuid()}";
            context.Request.Headers.Add("WebHook-Request-Origin", expectedRequestOrigin);

            // Act
            IActionResult result = validator.ValidateCloudEventsHandshakeRequest(context.Request);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);
            Assert.Contains(expectedRequestOrigin, (string) context.Response.Headers["WebHook-Allowed-Origin"]);
        }

        [Fact]
        public void ValidateCloudEventsHandshakeRequest_WithHttpRequestWithoutProperHeader_Fails()
        {
            // Arrange
            var validator = new EventGridSubscriptionValidator(NullLogger);
            var context = new DefaultHttpContext();

            // Act
            IActionResult result = validator.ValidateCloudEventsHandshakeRequest(context.Request);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ValidateEventGridSubscriptionRequest_WithSubscriptionValidationCodeInHttpRequestBody_Succeeds()
        {
            // Arrange
            var validator = new EventGridSubscriptionValidator(NullLogger);
            var context = new DefaultHttpContext
            {
                Request = { Body = new MemoryStream(Encoding.UTF8.GetBytes(Artifacts.EventSamples.SubscriptionValidationEvent)) }
            };

            // Act
            IActionResult result = await validator.ValidateEventGridSubscriptionEventRequestAsync(context.Request);

            // Assert
            Assert.NotNull(result);
            var responseBody = Assert.IsType<OkObjectResult>(result).Value;
            var subscriptionValidationResponse = Assert.IsType<SubscriptionValidationResponse>(responseBody);
            Assert.Equal("512d38b6-c7b8-40c8-89fe-f46f9e9622b6", subscriptionValidationResponse.ValidationResponse);
        }

        [Fact]
        public async Task ValidateEventGridSubscriptionRequest_WithoutSubscriptionValidationCodeInHttpRequestBody_Fails()
        {
            // Arrange
            var validator = new EventGridSubscriptionValidator(NullLogger);
            var context = new DefaultHttpContext();
            var validationEvent = new EventGridEvent(
                subject: "Sample.Event",
                data: JObject.Parse($"{{ \"validationCode\": null }}"),
                dataVersion: "1",
                eventType: "Microsoft.EventGrid.SubscriptionValidationEvent")
            {
                Id = "2d1781af-3a4c-4d7c-bd0c-e34b19da4e66"
            };
            string json = JsonConvert.SerializeObject(validationEvent);
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Act
            IActionResult result = await validator.ValidateEventGridSubscriptionEventRequestAsync(context.Request);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ValidateEventGridSubscriptionRequest_WithHttpRequestWithoutRequestBody_Fails()
        {
            // Arrange
            var validator = new EventGridSubscriptionValidator(NullLogger);
            var context = new DefaultHttpContext();

            // Act
            IActionResult result = await validator.ValidateEventGridSubscriptionEventRequestAsync(context.Request);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("    ")]
        public async Task ValidateEventGridSubscriptionRequest_WithBlankHttpRequestBody_Fails(string requestBody)
        {
            // Arrange
            var validator = new EventGridSubscriptionValidator(NullLogger);
            var context = new DefaultHttpContext();
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));

            // Act
            IActionResult result = await validator.ValidateEventGridSubscriptionEventRequestAsync(context.Request);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ValidateEventGridSubscriptionRequest_WithMultipleEventsInHttpRequestBody_Fails()
        {
            // Arrange
            var validator = new EventGridSubscriptionValidator(NullLogger);
            var context = new DefaultHttpContext();
            EventGridEvent[] blobCreatedEvent = EventGridEvent.ParseMany(BinaryData.FromString(Artifacts.EventSamples.BlobCreateEvent));
            EventGridEvent[] iotDeviceDeletedEvent = EventGridEvent.ParseMany(BinaryData.FromString(Artifacts.EventSamples.IoTDeviceDeleteEvent));
            EventGridEvent[] events = blobCreatedEvent.Concat(iotDeviceDeletedEvent).ToArray();
            
            string requestBody = JsonConvert.SerializeObject(events);
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));

            // Act
            IActionResult result = await validator.ValidateEventGridSubscriptionEventRequestAsync(context.Request);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ValidateEventGridSubscriptionRequest_WithWrongEventTypeInHttpRequestBody_Fails()
        {
            // Arrange
            var validator = new EventGridSubscriptionValidator(NullLogger);
            var context = new DefaultHttpContext();
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(Artifacts.EventSamples.BlobCreateEvent));

            // Act
            IActionResult result = await validator.ValidateEventGridSubscriptionEventRequestAsync(context.Request);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void ValidateCloudEventsHandshakeRequest_WithoutHttpRequest_Fails()
        {
            // Arrange
            var validator = new EventGridSubscriptionValidator(NullLogger);
            var context = new DefaultHttpContext();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => validator.ValidateCloudEventsHandshakeRequest(request: null));
        }

        [Fact]
        public void ValidateEventGridSubscriptionRequest_WithoutHttpRequest_Fails()
        {
            // Arrange
            var validator = new EventGridSubscriptionValidator(NullLogger);

            // Act / Assert
            Assert.ThrowsAnyAsync<ArgumentException>(
                () => validator.ValidateEventGridSubscriptionEventRequestAsync(request: null));
        }
    }
}
