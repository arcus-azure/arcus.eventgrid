using System;
using Arcus.EventGrid.WebApi.Security;
using Bogus;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Security
{
    public class EventGridAuthorizationAttributeTests
    {
        private readonly Faker _bogusGenerator = new Faker();
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateAttribute_WithoutSecretName_Fails(string secretName)
        {
            // Arrange
            var property = _bogusGenerator.Random.Enum<HttpRequestProperty>();
            string inputName = _bogusGenerator.Name.FirstName();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new EventGridAuthorizationAttribute(property, inputName, secretName));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateAttribute_WithoutInputName_Fails(string inputName)
        {
            // Arrange
            var property = _bogusGenerator.Random.Enum<HttpRequestProperty>();
            string secretName = _bogusGenerator.Name.FirstName();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new EventGridAuthorizationAttribute(property, inputName, secretName));
        }

        [Theory]
        [InlineData(HttpRequestProperty.Header)]
        [InlineData(HttpRequestProperty.Query)]
        public void CreateAttribute_WithRequestFlags_Succeeds(HttpRequestProperty requestInput)
        {
            // Arrange
            string property = _bogusGenerator.Name.FirstName();
            string secretName = _bogusGenerator.Name.FirstName();
            
            // Act / Assert
            var attribute = new EventGridAuthorizationAttribute(requestInput, property, secretName);
        }
        
        [Theory]
        [InlineData((HttpRequestProperty) 4)]
        [InlineData((HttpRequestProperty) 5)]
        [InlineData((HttpRequestProperty) 15)]
        [InlineData(HttpRequestProperty.Query | HttpRequestProperty.Header)]
        public void CreateAttribute_WithInvalidRequestFlags_Succeeds(HttpRequestProperty requestInput)
        {
            // Arrange
            string property = _bogusGenerator.Name.FirstName();
            string secretName = _bogusGenerator.Name.FirstName();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => new EventGridAuthorizationAttribute(requestInput, property, secretName));
        }
    }
}
