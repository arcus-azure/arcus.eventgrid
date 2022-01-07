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
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Security
{
    public class EventGridAuthorizationFilterTests
    {
        private readonly Faker _bogusGenerator = new Faker();

        [Fact]
        public async Task AuthorizeRequestWithHeader_WithMatchingSecret_Succeeds()
        {
            // Arrange
            string inputName = "x-custom-input";
            string secretName = $"MySecret-{Guid.NewGuid()};";
            string secretValue = $"secret-{Guid.NewGuid()}";

            IServiceProvider serviceProvider = 
                new ServiceCollection()
                    .AddSecretStore(stores => stores.AddInMemory(secretName, secretValue))
                    .BuildServiceProvider();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add(inputName, secretValue);
            httpContext.RequestServices = serviceProvider;
            
            var authorizationContext = new AuthorizationFilterContext(
                new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>());
            
            var options = new EventGridAuthorizationOptions();
            var filter = new EventGridAuthorizationFilter(HttpRequestProperty.Header, inputName, secretName, options);
            
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

            IServiceProvider serviceProvider = 
                new ServiceCollection()
                    .AddSecretStore(stores => stores.AddInMemory(secretName, secretValue))
                    .BuildServiceProvider();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                [inputName] = secretValue
            });
            httpContext.RequestServices = serviceProvider;
            
            var authorizationContext = new AuthorizationFilterContext(
                new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>());
            
            var options = new EventGridAuthorizationOptions();
            var filter = new EventGridAuthorizationFilter(HttpRequestProperty.Query, inputName, secretName, options);
            
            // Act
            await filter.OnAuthorizationAsync(authorizationContext);
            
            // Assert
            Assert.NotNull(authorizationContext.HttpContext.User);
            var principal = Assert.IsType<GenericPrincipal>(authorizationContext.HttpContext.User);
            Assert.Equal("EventGrid", principal.Identity.Name);
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateFilter_WithoutSecretName_Fails(string secretName)
        {
            // Arrange
            var property = _bogusGenerator.Random.Enum<HttpRequestProperty>();
            string inputName = _bogusGenerator.Name.FirstName();
            var options = new EventGridAuthorizationOptions();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new EventGridAuthorizationFilter(property, inputName, secretName, options));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateFilter_WithoutInputName_Fails(string inputName)
        {
            // Arrange
            var property = _bogusGenerator.Random.Enum<HttpRequestProperty>();
            string secretName = _bogusGenerator.Name.FirstName();
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
            string inputName = _bogusGenerator.Name.FirstName();
            string secretName = _bogusGenerator.Name.FirstName();
            var options = new EventGridAuthorizationOptions();
            
            // Act / Assert
            var filter = new EventGridAuthorizationFilter(property, inputName, secretName, options);
        }
        
        [Theory]
        [InlineData((HttpRequestProperty) 4)]
        [InlineData((HttpRequestProperty) 5)]
        [InlineData((HttpRequestProperty) 15)]
        [InlineData(HttpRequestProperty.Query | HttpRequestProperty.Header)]
        public void CreateFilter_WithInvalidRequestFlags_Succeeds(HttpRequestProperty property)
        {
            // Arrange
            string inputName = _bogusGenerator.Name.FirstName();
            string secretName = _bogusGenerator.Name.FirstName();
            var options = new EventGridAuthorizationOptions();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new EventGridAuthorizationFilter(property, inputName, secretName, options));
        }

        [Fact]
        public void CreateFilter_WithoutOptions_Fails()
        {
            // Arrange
            var property = _bogusGenerator.Random.Enum<HttpRequestProperty>();
            string inputName = _bogusGenerator.Name.FirstName();
            string secretName = _bogusGenerator.Name.FirstName();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                new EventGridAuthorizationFilter(property, inputName, secretName, options: null));
        }
    }
}
