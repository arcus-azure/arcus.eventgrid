using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Arcus.EventGrid.Security.Attributes;
using Arcus.EventGrid.Tests.Artifacts;
using Arcus.EventGrid.Tests.InMemoryApi;
using Microsoft.Owin.Testing;
using Xunit;

namespace Arcus.EventGrid.Tests.Security.Attributes
{
    [Collection(TestCollections.ApiTests)]
    public class DynamicEventGridAuthorizationAttributeTests
    {
        [Fact]
        public async Task Authorize_UsesCorrectHeaderAndKey_ShouldSucceed()
        {
            // Arrange
            const string keyName = "x-auth";
            const string apiKey = "event-grid";
            InMemoryTestApiStartup.SecretKey = "event-grid";
            var rawMessageContent = new ByteArrayContent(Encoding.UTF8.GetBytes(Events.SubscriptionValidationEvent));

            // Act
            using (var server = TestServer.Create<InMemoryTestApiStartup>())
            {
                var response = await server.CreateRequest($"/authz/dynamic?{keyName}={apiKey}")
                    .And(message => { message.Content = rawMessageContent; })
                    .PostAsync();

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(false, null, null)]
        [InlineData(true, "x-api-key", "event-grid")]
        [InlineData(true, "key-name", "key-value")]
        public async Task Authorize_WrongAuthentication_ShouldReturnUnauthorized(bool addQueryString, string name, string secret)
        {
            // Arrange
            InMemoryTestApiStartup.SecretKey = "event-grid";
            var rawMessageContent = new ByteArrayContent(Encoding.UTF8.GetBytes(Events.SubscriptionValidationEvent));

            // Act
            using (var server = TestServer.Create<InMemoryTestApiStartup>())
            {
                var queryString = addQueryString ? $"?{name}={secret}" : "";
                var response = await server.CreateRequest($"/authz/dynamic{queryString}")
                    .And(message => { message.Content = rawMessageContent; })
                    .PostAsync();

                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public void Constructor_HasEmptyAuthenticationKeyName_ShouldFailWithArgumentException()
        {
            // Arrange
            var authenticationKeyName = string.Empty;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DynamicEventGridAuthorizationAttribute(authenticationKeyName));
        }

        [Fact]
        public void Constructor_HasNoAuthenticationKeyName_ShouldFailWithArgumentNullException()
        {
            // Arrange
            string authenticationKeyName = null;

            // Act & Assert
            // ReSharper disable once ExpressionIsAlwaysNull
            Assert.Throws<ArgumentNullException>(() => new DynamicEventGridAuthorizationAttribute(authenticationKeyName));
        }

        [Fact]
        public void Constructor_UsesDefaultConstructor_Succeeds()
        {
            // Act
            var authorizationAttribute = new DynamicEventGridAuthorizationAttribute();

            // Assert
            Assert.NotNull(authorizationAttribute);
        }
    }
}