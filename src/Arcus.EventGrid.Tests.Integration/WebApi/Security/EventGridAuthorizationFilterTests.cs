using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Arcus.EventGrid.Tests.Integration.Fixture;
using Arcus.EventGrid.Tests.Integration.WebApi.Fixture;
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
    public class EventGridAuthorizationFilterTests
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridAuthorizationFilterTests" /> class.
        /// </summary>
        public EventGridAuthorizationFilterTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task EventGridAuthorization_UsingHeader_Succeeds()
        {
            // Arrange
            string headerName = "x-custom-header";
            string secretName = $"MySecret-{Guid.NewGuid()}";
            string secretValue = $"secret-{Guid.NewGuid()}";
            var filter = new EventGridAuthorizationFilter(HttpRequestProperty.Header, headerName, secretName, new EventGridAuthorizationOptions());
            
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(secretName, secretValue))
                                                       .AddMvc(mvc => mvc.Filters.Add(filter)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(HealthController.GetRoute)
                    .WithHeader(headerName, secretValue);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }
        
        [Fact]
        public async Task EventGridAuthorization_UsingHeaderWithoutRequestHeader_Fails()
        {
            // Arrange
            string headerName = "x-custom-header";
            string secretName = $"MySecret-{Guid.NewGuid()}";
            string secretValue = $"secret-{Guid.NewGuid()}";
            var filter = new EventGridAuthorizationFilter(HttpRequestProperty.Header, headerName, secretName, new EventGridAuthorizationOptions());
            
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(secretName, secretValue))
                                                       .AddMvc(mvc => mvc.Filters.Add(filter)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(HealthController.GetRoute);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }
        
        [Fact]
        public async Task EventGridAuthorization_UsingHeaderWithWrongRequestHeader_Fails()
        {
            // Arrange
            string headerName = "x-custom-header";
            string secretName = $"MySecret-{Guid.NewGuid()}";
            string secretValue = $"secret-{Guid.NewGuid()}";
            var filter = new EventGridAuthorizationFilter(HttpRequestProperty.Header, headerName, secretName, new EventGridAuthorizationOptions());
            
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(secretName, secretValue))
                                                       .AddMvc(mvc => mvc.Filters.Add(filter)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(HealthController.GetRoute)
                    .WithHeader(headerName, "some-other-value");

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }
        
        [Fact]
        public async Task EventGridAuthorization_UsingQuery_Succeeds()
        {
            // Arrange
            string parameterName = "x-custom-parameter";
            string secretName = $"MySecret-{Guid.NewGuid()}";
            string secretValue = $"secret-{Guid.NewGuid()}";
            var filter = new EventGridAuthorizationFilter(HttpRequestProperty.Query, parameterName, secretName, new EventGridAuthorizationOptions());
            
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(secretName, secretValue))
                                                       .AddMvc(mvc => mvc.Filters.Add(filter)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(HealthController.GetRoute)
                    .WithParameter(parameterName, secretValue);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }
        
        [Fact]
        public async Task EventGridAuthorization_UsingQueryWithoutQueryParameter_Fails()
        {
            // Arrange
            string parameterName = "x-custom-parameter";
            string secretName = $"MySecret-{Guid.NewGuid()}";
            string secretValue = $"secret-{Guid.NewGuid()}";
            var filter = new EventGridAuthorizationFilter(HttpRequestProperty.Query, parameterName, secretName, new EventGridAuthorizationOptions());
            
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(secretName, secretValue))
                                                       .AddMvc(mvc => mvc.Filters.Add(filter)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(HealthController.GetRoute);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }
        
        [Fact]
        public async Task EventGridAuthorization_UsingQueryWithWrongQueryParameter_Fails()
        {
            // Arrange
            string parameterName = "x-custom-parameter";
            string secretName = $"MySecret-{Guid.NewGuid()}";
            string secretValue = $"secret-{Guid.NewGuid()}";
            var filter = new EventGridAuthorizationFilter(HttpRequestProperty.Query, parameterName, secretName, new EventGridAuthorizationOptions());
            
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(secretName, secretValue))
                                                       .AddMvc(mvc => mvc.Filters.Add(filter)));

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(HealthController.GetRoute)
                    .WithParameter(parameterName, "some-other-value");

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(HttpRequestProperty.Header)]
        [InlineData(HttpRequestProperty.Query)]
        public async Task EventGridAuthorization_WithoutSecretStore_Fails(HttpRequestProperty requestInput)
        {
            // Arrange
            string inputName = "x-custom-input";
            string secretName = $"MySecret-{Guid.NewGuid()}";
            var filter = new EventGridAuthorizationFilter(requestInput, inputName, secretName, new EventGridAuthorizationOptions());
            
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddMvc(mvc => mvc.Filters.Add(filter)));
            
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder.Get(HealthController.GetRoute);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(HttpRequestProperty.Header)]
        [InlineData(HttpRequestProperty.Query)]
        public async Task EventGridAuthorization_UsingDefault_DoestEmitsSecurityEvents(HttpRequestProperty requestInput)
        {
            // Arrange
            string inputName = "x-custom-input";
            string secretName = $"MySecret-{Guid.NewGuid()}";
            string secretValue = $"secret-{Guid.NewGuid()}";
            var filter = new EventGridAuthorizationFilter(requestInput, inputName, secretName, new EventGridAuthorizationOptions());
            var spyLogger = new InMemoryLogger();
            
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(secretName, secretValue))
                                                       .AddMvc(mvc => mvc.Filters.Add(filter)));

            await using (var server = await TestApiServer.StartNewAsync(options, spyLogger))
            {
                var request = HttpRequestBuilder.Get(HealthController.GetRoute);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.DoesNotContain(spyLogger.Messages, message =>
                    {
                        return message.Contains("EventType") && message.Contains("Security");
                    });
                }
            }
        }

        [Theory]
        [InlineData(HttpRequestProperty.Header, false)]
        [InlineData(HttpRequestProperty.Header, true)]
        [InlineData(HttpRequestProperty.Query, false)]
        [InlineData(HttpRequestProperty.Query, true)]
        public async Task EventGridAuthoridation_ConfiguresEmitsSecurityEvents_EmitsWhenRequested(
            HttpRequestProperty requestInput,
            bool emitsSecurityEvents)
        {
            // Arrange
            string inputName = "x-custom-input";
            string secretName = $"MySecret-{Guid.NewGuid()}";
            string secretValue = $"secret-{Guid.NewGuid()}";
            var authOptions = new EventGridAuthorizationOptions {EmitSecurityEvents = emitsSecurityEvents};
            var filter = new EventGridAuthorizationFilter(requestInput, inputName, secretName, authOptions);
            var spyLogger = new InMemoryLogger();
            
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddSecretStore(stores => stores.AddInMemory(secretName, secretValue))
                                                       .AddMvc(mvc => mvc.Filters.Add(filter)));

            await using (var server = await TestApiServer.StartNewAsync(options, spyLogger))
            {
                var request = HttpRequestBuilder.Get(HealthController.GetRoute);

                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Act
                    Assert.True(emitsSecurityEvents == spyLogger.Messages.Any(message =>
                    {
                        return message.Contains("EventType") && message.Contains("Security");
                    }));
                }
            }
        }
    }
}
