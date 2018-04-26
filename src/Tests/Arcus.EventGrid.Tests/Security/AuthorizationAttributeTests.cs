using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Testing;
using Xunit;

namespace Arcus.EventGrid.Tests.Security
{
    [Collection(TestCollections.ApiTests)]
    public class AuthorizationAttributeTests
    {
        [Theory]
        [InlineData(false, null, null, HttpStatusCode.Unauthorized)]
        [InlineData(true, "x-api-key", "event-grid", HttpStatusCode.Unauthorized)]
        [InlineData(true, "key-name", "key-value", HttpStatusCode.OK)]
        public async Task TestConfiguredSecretKeyValue(bool addQueryString, string name, string secret, HttpStatusCode expectedCode)
        {
            TestStartup.SecretKey = null;
            using (var server = TestServer.Create<TestStartup>())
            {
                var queryString = addQueryString ? $"?{name}={secret}" : "";
                var response = await server.CreateRequest($"/authz/keynamevalue{queryString}")
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
        [InlineData(true, "x-api-key", "event-grid", HttpStatusCode.Unauthorized)]
        [InlineData(true, "key-name", "key-value", HttpStatusCode.Unauthorized)]
        [InlineData(true, "key-name", "event-grid", HttpStatusCode.OK)]
        public async Task TestConfiguredSecretKey(bool addQueryString, string name, string secret, HttpStatusCode expectedCode)
        {
            TestStartup.SecretKey = "event-grid";
            using (var server = TestServer.Create<TestStartup>())
            {
                var queryString = addQueryString ? $"?{name}={secret}" : "";
                var response = await server.CreateRequest($"/authz/keyname{queryString}")
                    .And(message => { message.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(TestArtifacts.SubscriptionValidationEvent)); })
                    .PostAsync();
                if (!response.IsSuccessStatusCode && expectedCode != response.StatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                }

                Assert.Equal(expectedCode, response.StatusCode);
            }
        }
    }
}
