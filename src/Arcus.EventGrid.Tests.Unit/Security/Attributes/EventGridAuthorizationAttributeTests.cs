﻿using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Arcus.EventGrid.Security.Attributes;
using Arcus.EventGrid.Tests.InMemoryApi;
using Arcus.EventGrid.Tests.Unit.Artifacts;
using Microsoft.Owin.Testing;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Security.Attributes
{
    [Collection(TestCollections.ApiTests)]
    public class EventGridAuthorizationAttributeTests
    {
        [Fact]
        public async Task Authorize_UsesCorrectHeaderAndKey_ShouldSucceed()
        {
            // Arrange
            const string keyName = "x-auth";
            const string apiKey = "auth-key";
            var rawMessageContent = new ByteArrayContent(Encoding.UTF8.GetBytes(EventSamples.SubscriptionValidationEvent));

            // Act
            using (var server = TestServer.Create<InMemoryTestApiStartup>())
            {
                var response = await server.CreateRequest($"/authz/hardcoded?{keyName}={apiKey}")
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
            var rawMessageContent = new ByteArrayContent(Encoding.UTF8.GetBytes(EventSamples.SubscriptionValidationEvent));

            // Act
            using (var server = TestServer.Create<InMemoryTestApiStartup>())
            {
                var queryString = addQueryString ? $"?{name}={secret}" : "";
                var response = await server.CreateRequest($"/authz/hardcoded{queryString}")
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
            const string authenticationKeySecret = "abc";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new EventGridAuthorizationAttribute(authenticationKeyName, authenticationKeySecret));
        }

        [Fact]
        public void Constructor_HasEmptyAuthenticationKeySecret_ShouldFailWithArgumentException()
        {
            // Arrange
            var authenticationKeyName = "key";
            var authenticationKeySecret = string.Empty;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new EventGridAuthorizationAttribute(authenticationKeyName, authenticationKeySecret));
        }

        [Fact]
        public void Constructor_HasNoAuthenticationKeyName_ShouldFailWithArgumentException()
        {
            // Arrange
            string authenticationKeyName = null;
            const string authenticationKeySecret = "abc";

            // Act & Assert
            // ReSharper disable once ExpressionIsAlwaysNull
            Assert.Throws<ArgumentException>(() => new EventGridAuthorizationAttribute(authenticationKeyName, authenticationKeySecret));
        }

        [Fact]
        public void Constructor_HasNoAuthenticationKeySecret_ShouldFailWithArgumentException()
        {
            // Arrange
            const string authenticationKeyName = "key";
            string authenticationKeySecret = null;

            // Act & Assert
            // ReSharper disable once ExpressionIsAlwaysNull
            Assert.Throws<ArgumentException>(() => new EventGridAuthorizationAttribute(authenticationKeyName, authenticationKeySecret));
        }
    }
}