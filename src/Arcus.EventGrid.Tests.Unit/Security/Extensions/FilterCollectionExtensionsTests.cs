using System;
using Arcus.EventGrid.Security.WebApi;
using Bogus;
using Microsoft.AspNetCore.Mvc.Filters;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Security.Extensions
{
    public class FilterCollectionExtensionsTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void AddFilter_WithoutOptions_Succeeds()
        {
            // Arrange
            var filters = new FilterCollection();
            var requestProperty = BogusGenerator.Random.Enum<HttpRequestProperty>();
            var propertyName = BogusGenerator.Random.AlphaNumeric(10);
            string secretName = BogusGenerator.Random.AlphaNumeric(10);
            
            // Act
            filters.AddEventGridAuthorization(requestProperty, propertyName, secretName);

            // Assert
            IFilterMetadata filter = Assert.Single(filters);
            Assert.IsType<EventGridAuthorizationFilter>(filter);
        }

        [Fact]
        public void AddFilter_WithOptions_Succeeds()
        {
            // Arrange
            var filters = new FilterCollection();
            var requestProperty = BogusGenerator.Random.Enum<HttpRequestProperty>();
            var propertyName = BogusGenerator.Random.AlphaNumeric(10);
            string secretName = BogusGenerator.Random.AlphaNumeric(10);
            bool emitSecurityEvents = BogusGenerator.Random.Bool();

            // Act
            filters.AddEventGridAuthorization(requestProperty, propertyName, secretName, opt => opt.EmitSecurityEvents = emitSecurityEvents);

            // Assert
            IFilterMetadata filter = Assert.Single(filters);
            Assert.IsType<EventGridAuthorizationFilter>(filter);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddFilter_WithoutPropertyName_Fails(string propertyName)
        {
            // Arrange
            var filters = new FilterCollection();
            var requestProperty = BogusGenerator.Random.Enum<HttpRequestProperty>();
            var secretName = BogusGenerator.Random.AlphaNumeric(10);
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => filters.AddEventGridAuthorization(requestProperty, propertyName, secretName));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddFilter_WithoutSecretName_Fails(string secretName)
        {
            // Arrange
            var filters = new FilterCollection();
            var requestProperty = BogusGenerator.Random.Enum<HttpRequestProperty>();
            var propertyName = BogusGenerator.Random.AlphaNumeric(10);
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => filters.AddEventGridAuthorization(requestProperty, propertyName, secretName));
        }

        [Theory]
        [InlineData(HttpRequestProperty.Query | HttpRequestProperty.Header)]
        [InlineData((HttpRequestProperty) 16)]
        public void AddFilter_OutsideEnumBounds_Fails(HttpRequestProperty requestProperty)
        {
            // Arrange
            var filters = new FilterCollection();
            var propertyName = BogusGenerator.Random.AlphaNumeric(10);
            var secretName = BogusGenerator.Random.AlphaNumeric(10);
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => filters.AddEventGridAuthorization(requestProperty, propertyName, secretName));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddFilter_WithoutPropertyNameWithOptions_Fails(string propertyName)
        {
            // Arrange
            var filters = new FilterCollection();
            var requestProperty = BogusGenerator.Random.Enum<HttpRequestProperty>();
            var secretName = BogusGenerator.Random.AlphaNumeric(10);
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => filters.AddEventGridAuthorization(requestProperty, propertyName, secretName, options => options.EmitSecurityEvents = true));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddFilter_WithoutSecretNameWithOptions_Fails(string secretName)
        {
            // Arrange
            var filters = new FilterCollection();
            var requestProperty = BogusGenerator.Random.Enum<HttpRequestProperty>();
            var propertyName = BogusGenerator.Random.AlphaNumeric(10);
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => filters.AddEventGridAuthorization(requestProperty, propertyName, secretName, options => options.EmitSecurityEvents = true));
        }

        [Theory]
        [InlineData(HttpRequestProperty.Query | HttpRequestProperty.Header)]
        [InlineData((HttpRequestProperty) 16)]
        public void AddFilter_OutsideEnumBoundsWithOptions_Fails(HttpRequestProperty requestProperty)
        {
            // Arrange
            var filters = new FilterCollection();
            var propertyName = BogusGenerator.Random.AlphaNumeric(10);
            var secretName = BogusGenerator.Random.AlphaNumeric(10);
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => filters.AddEventGridAuthorization(requestProperty, propertyName, secretName, options => options.EmitSecurityEvents = true));
        }
    }
}
