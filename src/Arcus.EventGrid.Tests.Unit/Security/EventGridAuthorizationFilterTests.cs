using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using Arcus.EventGrid.Security.WebApi;
using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Security
{
    public class EventGridAuthorizationFilterTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public async Task AuthorizeRequestWithHeader_WithMatchingSecret_Succeeds()
        {
            // Arrange
            string inputName = "x-custom-input";
            string secretName = $"MySecret-{Guid.NewGuid()};";
            string secretValue = $"secret-{Guid.NewGuid()}";

            HttpContext httpContext = CreateHttpContext(
                configureHeaders: headers => headers.Add(inputName, secretValue),
                configureServices: services => services.AddSecretStore(stores => stores.AddInMemory(secretName, secretValue)));
            
            AuthorizationFilterContext authorizationContext = CreateAuthContext(httpContext);
            EventGridAuthorizationFilter filter = CreateAuthFilter(HttpRequestProperty.Header, inputName, secretName);
            
            // Act
            await filter.OnAuthorizationAsync(authorizationContext);
            
            // Assert
            Assert.NotNull(authorizationContext.HttpContext.User);
            var principal = Assert.IsType<GenericPrincipal>(authorizationContext.HttpContext.User);
            Assert.Equal("EventGrid", principal.Identity.Name);
        }
        
        [Fact]
        public async Task AuthorizeRequestWithQuery_WithMatchingSecret_Succeeds()
        {
            // Arrange
            string inputName = "x-custom-input";
            string secretName = $"MySecret-{Guid.NewGuid()};";
            string secretValue = $"secret-{Guid.NewGuid()}";

            HttpContext httpContext = CreateHttpContext(
                configureQuery: para => para.Add(inputName, secretValue),
                configureServices: services => services.AddSecretStore(stores => stores.AddInMemory(secretName, secretValue)));

            AuthorizationFilterContext authorizationContext = CreateAuthContext(httpContext);
            EventGridAuthorizationFilter filter = CreateAuthFilter(HttpRequestProperty.Query, inputName, secretName);
            
            // Act
            await filter.OnAuthorizationAsync(authorizationContext);
            
            // Assert
            Assert.NotNull(authorizationContext.HttpContext.User);
            var principal = Assert.IsType<GenericPrincipal>(authorizationContext.HttpContext.User);
            Assert.Equal("EventGrid", principal.Identity.Name);
        }

        [Fact]
        public async Task AuthorizeRequestWithHeader_WithoutHeaderValue_Fails()
        {
            // Arrange
            string inputName = BogusGenerator.Lorem.Word();
            string secretName = BogusGenerator.Lorem.Word();
            string secretValue = BogusGenerator.Random.AlphaNumeric(10);

            HttpContext httpContext = CreateHttpContext(
                configureServices: services => services.AddSecretStore(stores => stores.AddInMemory(secretName, secretValue)));

            AuthorizationFilterContext authorizationContext = CreateAuthContext(httpContext);
            EventGridAuthorizationFilter filter = CreateAuthFilter(HttpRequestProperty.Header, inputName, secretName);

            // Act
            await filter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(authorizationContext.Result);
        }

        [Fact]
        public async Task AuthorizeRequestWithHeader_WithInvalidHeaderValue_Fails()
        {
            // Arrange
            string inputName = BogusGenerator.Lorem.Word();
            string secretName = BogusGenerator.Lorem.Word();
            string secretValue = BogusGenerator.Random.AlphaNumeric(10);
            string invalidValue = BogusGenerator.Random.AlphaNumeric(12);

            HttpContext httpContext = CreateHttpContext(
                configureHeaders: headers => headers.Add(inputName, invalidValue),
                configureServices: services => services.AddSecretStore(stores => stores.AddInMemory(secretName, secretValue)));

            AuthorizationFilterContext authorizationContext = CreateAuthContext(httpContext);
            EventGridAuthorizationFilter filter = CreateAuthFilter(HttpRequestProperty.Header, inputName, secretName);

            // Act
            await filter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(authorizationContext.Result);
        }

        [Fact]
        public async Task AuthorizeRequestWithQuery_WithoutHeaderValue_Fails()
        {
            // Arrange
            string inputName = BogusGenerator.Lorem.Word();
            string secretName = BogusGenerator.Lorem.Word();
            string secretValue = BogusGenerator.Random.AlphaNumeric(10);

            HttpContext httpContext = CreateHttpContext(
                configureServices: services => services.AddSecretStore(stores => stores.AddInMemory(secretName, secretValue)));

            AuthorizationFilterContext authorizationContext = CreateAuthContext(httpContext);
            EventGridAuthorizationFilter filter = CreateAuthFilter(HttpRequestProperty.Query, inputName, secretName);

            // Act
            await filter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(authorizationContext.Result);
        }

        [Fact]
        public async Task AuthorizeRequestWithQuery_WithWrongQueryValue_Fails()
        {
            // Arrange
            string inputName = BogusGenerator.Lorem.Word();
            string secretName = BogusGenerator.Lorem.Word();
            string secretValue = BogusGenerator.Random.AlphaNumeric(10);
            string invalidValue = BogusGenerator.Random.AlphaNumeric(12);

            HttpContext httpContext = CreateHttpContext(
                configureQuery: para => para.Add(inputName, invalidValue),
                configureServices: services => services.AddSecretStore(stores => stores.AddInMemory(secretName, secretValue)));

            AuthorizationFilterContext authorizationContext = CreateAuthContext(httpContext);
            EventGridAuthorizationFilter filter = CreateAuthFilter(HttpRequestProperty.Query, inputName, secretName);

            // Act
            await filter.OnAuthorizationAsync(authorizationContext);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(authorizationContext.Result);
        }

        [Fact]
        public async Task AuthorizeRequest_WithoutSecretStore_Fails()
        {
            // Arrange
            HttpContext httpContext = CreateHttpContext();
            AuthorizationFilterContext authorizationContext = CreateAuthContext(httpContext);

            var requestProperty = BogusGenerator.PickRandom<HttpRequestProperty>();
            string inputName = BogusGenerator.Lorem.Word();
            string secretName = BogusGenerator.Lorem.Word();
            EventGridAuthorizationFilter filter = CreateAuthFilter(requestProperty, inputName, secretName);

            // Act / Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => filter.OnAuthorizationAsync(authorizationContext));
        }

        private static HttpContext CreateHttpContext(
            Action<IHeaderDictionary> configureHeaders = null,
            Action<IDictionary<string, StringValues>> configureQuery = null,
            Action<IServiceCollection> configureServices = null)
        {
            var services = new ServiceCollection();
            configureServices?.Invoke(services);
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            var parameters = new Dictionary<string, StringValues>();
            configureQuery?.Invoke(parameters);

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
                Request = { Query = new QueryCollection(parameters) }
            };
            configureHeaders?.Invoke(httpContext.Request.Headers);

            return httpContext;
        }

        private static EventGridAuthorizationFilter CreateAuthFilter(HttpRequestProperty requestProperty, string inputName, string secretName)
        {
            var options = new EventGridAuthorizationOptions
            {
                EmitSecurityEvents = BogusGenerator.Random.Bool()
            };
            return new EventGridAuthorizationFilter(requestProperty, inputName, secretName, options);
        }

        private static AuthorizationFilterContext CreateAuthContext(HttpContext httpContext)
        {
            var authorizationContext = new AuthorizationFilterContext(
                new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>());

            return authorizationContext;
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateFilter_WithoutSecretName_Fails(string secretName)
        {
            // Arrange
            var property = BogusGenerator.Random.Enum<HttpRequestProperty>();
            string inputName = BogusGenerator.Name.FirstName();
            var options = new EventGridAuthorizationOptions();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new EventGridAuthorizationFilter(property, inputName, secretName, options));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateFilter_WithoutInputName_Fails(string inputName)
        {
            // Arrange
            var property = BogusGenerator.Random.Enum<HttpRequestProperty>();
            string secretName = BogusGenerator.Name.FirstName();
            var options = new EventGridAuthorizationOptions();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new EventGridAuthorizationFilter(property, inputName, secretName, options));
        }

        [Theory]
        [InlineData(HttpRequestProperty.Header)]
        [InlineData(HttpRequestProperty.Query)]
        public void CreateFilter_WithRequestFlags_Succeeds(HttpRequestProperty property)
        {
            // Arrange
            string inputName = BogusGenerator.Name.FirstName();
            string secretName = BogusGenerator.Name.FirstName();
            var options = new EventGridAuthorizationOptions();
            
            // Act / Assert
            var filter = new EventGridAuthorizationFilter(property, inputName, secretName, options);
        }

        [Fact]
        public void CreateFilter_WithInvalidRequestFlags_Succeeds()
        {
            // Arrange
            string inputName = BogusGenerator.Name.FirstName();
            string secretName = BogusGenerator.Name.FirstName();
            var property = (HttpRequestProperty) BogusGenerator.Random.Int(min: 3);
            var options = new EventGridAuthorizationOptions();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new EventGridAuthorizationFilter(property, inputName, secretName, options));
        }

        [Fact]
        public void CreateFilter_WithoutOptions_Fails()
        {
            // Arrange
            var property = BogusGenerator.Random.Enum<HttpRequestProperty>();
            string inputName = BogusGenerator.Name.FirstName();
            string secretName = BogusGenerator.Name.FirstName();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                new EventGridAuthorizationFilter(property, inputName, secretName, options: null));
        }
    }
}
