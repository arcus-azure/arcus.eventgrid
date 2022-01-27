using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.EventGrid.Tests.Integration.Fixture;
using Arcus.EventGrid.Tests.Integration.WebApi.Fixture;
using Arcus.EventGrid.Tests.Integration.WebApi.Security.Controllers;
using Arcus.EventGrid.Security.WebApi;
using Arcus.Testing.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.EventGrid.Tests.Integration.WebApi.Security
{
    [Trait("Category", "Integration")]
    [Collection(TestCollections.Integration)]
    public class EventGridAuthorizationAttributeTests
    {
        private readonly ILogger _logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridAuthorizationAttributeTests" /> class.
        /// </summary>
        public EventGridAuthorizationAttributeTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }
        
        [Fact]
        public async Task EventGridAuthorization_UsingHeader_Succeeds()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(EventGridAuthorizationController.SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(EventGridAuthorizationController.GetAuthorizedWithHeaderRoute)
                    .WithHeader(EventGridAuthorizationController.HttpRequestHeaderName, secretValue);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }
        
        [Fact]
        public async Task EventGridAuthorization_UsingHeaderWithoutHeader_Fails()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(EventGridAuthorizationController.SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(EventGridAuthorizationController.GetAuthorizedWithHeaderRoute);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }
        
        [Fact]
        public async Task EventGridAuthorization_UsingHeaderWithWrongHeader_Fails()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(EventGridAuthorizationController.SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(EventGridAuthorizationController.GetAuthorizedWithHeaderRoute)
                    .WithHeader(EventGridAuthorizationController.HttpRequestHeaderName, "some-other-value");

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }
        
        [Fact]
        public async Task EventGridAuthorization_UsingHeaderWithDefault_DoesntEmitSecurityEvents()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var spyLogger = new InMemoryLogger();
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(EventGridAuthorizationController.SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, spyLogger))
            {
                var request = HttpRequestBuilder
                    .Get(EventGridAuthorizationController.GetAuthorizedWithHeaderRoute)
                    .WithHeader(EventGridAuthorizationController.HttpRequestHeaderName, secretValue);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.DoesNotContain(spyLogger.Messages, message =>
                    {
                        return message.Contains("EventType") && message.Contains("Security");
                    });
                }
            }
        }
        
        [Fact]
        public async Task EventGridAuthorization_UsingHeader_EmitsSecurityEventsAsRequested()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var spyLogger = new InMemoryLogger();
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(EventGridAuthorizationController.SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, spyLogger))
            {
                var request = HttpRequestBuilder
                    .Get(EventGridAuthorizationController.GetAuthorizedWithHeaderEmitsSecurityEventsRoute)
                    .WithHeader(EventGridAuthorizationController.HttpRequestHeaderName, secretValue);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Contains(spyLogger.Messages, message =>
                    {
                        return message.Contains("EventType") && message.Contains("Security");
                    });
                }
            }
        }
        
        [Fact]
        public async Task EventGridAuthorization_UsingQuery_Succeeds()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(EventGridAuthorizationController.SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(EventGridAuthorizationController.GetAuthorizedWithQueryRoute)
                    .WithParameter(EventGridAuthorizationController.HttpRequestQueryParameterName, secretValue);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }
        
        [Fact]
        public async Task EventGridAuthorization_UsingQueryWithoutQuery_Fails()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(EventGridAuthorizationController.SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(EventGridAuthorizationController.GetAuthorizedWithQueryRoute);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }
        
        [Fact]
        public async Task EventGridAuthorization_UsingQueryWithWrongQuery_Fails()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(EventGridAuthorizationController.SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(EventGridAuthorizationController.GetAuthorizedWithQueryRoute)
                    .WithParameter(EventGridAuthorizationController.HttpRequestQueryParameterName, "some-other-value");

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }
        
        [Fact]
        public async Task EventGridAuthorization_UsingQueryWithDefault_DoesntEmitSecurityEvents()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var spyLogger = new InMemoryLogger();
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(EventGridAuthorizationController.SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, spyLogger))
            {
                var request = HttpRequestBuilder
                    .Get(EventGridAuthorizationController.GetAuthorizedWithQueryRoute)
                    .WithParameter(EventGridAuthorizationController.HttpRequestQueryParameterName, secretValue);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.DoesNotContain(spyLogger.Messages, message =>
                    {
                        return message.Contains("EventType") && message.Contains("Security");
                    });
                }
            }
        }
        
        [Fact]
        public async Task EventGridAuthorization_UsingQuery_EmitsSecurityEventsAsRequested()
        {
            // Arrange
            string secretValue = $"secret-{Guid.NewGuid()}";
            var spyLogger = new InMemoryLogger();
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(EventGridAuthorizationController.SecretName, secretValue)));

            await using (var server = await TestApiServer.StartNewAsync(options, spyLogger))
            {
                var request = HttpRequestBuilder
                    .Get(EventGridAuthorizationController.GetAuthorizedWithQueryEmitsSecurityEventsRoute)
                    .WithParameter(EventGridAuthorizationController.HttpRequestQueryParameterName, secretValue);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Contains(spyLogger.Messages, message =>
                    {
                        return message.Contains("EventType") && message.Contains("Security");
                    });
                }
            }
        }
    }
}
