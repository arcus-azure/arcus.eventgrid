using System;
using System.Collections.Generic;
using Azure.Messaging.EventGrid;
using Bogus;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Publishing
{
    public class EventGridPublisherClientWithTrackingOptionsTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void TransactionIdPropertyName_Default_HasValue()
        {
            // Arrange
            var options = new EventGridPublisherClientWithTrackingOptions();

            // Act
            string propertyName = options.TransactionIdEventDataPropertyName;

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(propertyName), "Default transaction ID property name for event data should not be blank");
        }

        [Fact]
        public void TransactionIdPropertyName_SetValue_Succeeds()
        {
            // Arrange
            string propertyName = BogusGenerator.Lorem.Word();
            var options = new EventGridPublisherClientWithTrackingOptions();

            // Act
            options.TransactionIdEventDataPropertyName = propertyName;

            // Assert
            Assert.Equal(propertyName, options.TransactionIdEventDataPropertyName);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void TransactionIdPropertyName_SetBlankValue_Fails(string propertyName)
        {
            // Arrange
            var options = new EventGridPublisherClientWithTrackingOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.TransactionIdEventDataPropertyName = propertyName);
        }

        [Fact]
        public void UpstreamServicePropertyName_Default_HasValue()
        {
            // Arrange
            var options = new EventGridPublisherClientWithTrackingOptions();

            // Act
            string propertyName = options.UpstreamServicePropertyName;

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(propertyName), "Default upstream service property name for event data should not be blank");
        }

        [Fact]
        public void UpstreamServicePropertyName_SetValue_Succeeds()
        {
            // Arrange
            string propertyName = BogusGenerator.Lorem.Word();
            var options = new EventGridPublisherClientWithTrackingOptions();

            // Act
            options.UpstreamServicePropertyName = propertyName;

            // Assert
            Assert.Equal(propertyName, options.UpstreamServicePropertyName);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void UpstreamServicePropertyName_SetBlankValue_Fails(string propertyName)
        {
            // Arrange
            var options = new EventGridPublisherClientWithTrackingOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.UpstreamServicePropertyName = propertyName);
        }

        [Fact]
        public void GenerateDependencyId_Default_HasValue()
        {
            // Arrange
            var options = new EventGridPublisherClientWithTrackingOptions();

            // Act
            string dependencyId = options.GenerateDependencyId();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(dependencyId), "Default generated dependency ID should not be blank");
        }

        [Fact]
        public void GenerateDependencyId_WithFunction_Succeeds()
        {
            // Arrange
            string dependencyId = Guid.NewGuid().ToString();
            var options = new EventGridPublisherClientWithTrackingOptions();

            // Act
            options.GenerateDependencyId = () => dependencyId;

            // Assert
            Assert.Equal(dependencyId, options.GenerateDependencyId());
        }

        [Fact]
        public void GenerateDependencyId_WithoutFunction_Fails()
        {
            // Arrange
            var options = new EventGridPublisherClientWithTrackingOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.GenerateDependencyId = null);
        }

        [Fact]
        public void AddExponentialRetryPolicy_WithGreaterThanZeroRetryCount_Succeeds()
        {
            // Arrange
            var options = new EventGridPublisherClientWithTrackingOptions();
            int retryCount = BogusGenerator.Random.Int(min: 1);

            // Act / Assert
            options.WithExponentialRetry<Exception>(retryCount);
        }

        [Fact]
        public void AddExponentialRetryPolicy_WithLessThanOrEqualToZeroRetryCount_Fails()
        {
            // Arrange
            var options = new EventGridPublisherClientWithTrackingOptions();
            int retryCount = BogusGenerator.Random.Int(max: 0);

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.WithExponentialRetry<Exception>(retryCount));
        }

        [Fact]
        public void AddCircuitBreaker_WithGreaterThanZeroAllowedExceptionsBeforeBreakingCount_Succeeds()
        {
            // Arrange
            var options = new EventGridPublisherClientWithTrackingOptions();
            int allowedExceptionsBeforeBreakingCount = BogusGenerator.Random.Int(min: 1);
            TimeSpan durationOfBreak = TimeSpan.FromSeconds(5);

            // Act / Assert
            options.WithCircuitBreaker<Exception>(allowedExceptionsBeforeBreakingCount, durationOfBreak);
        }

        [Fact]
        public void AddCircuitBreaker_WithLessThanOrEqualToZeroAllowedExceptionsBeforeBreakingCount_Fails()
        {
            // Arrange
            var options = new EventGridPublisherClientWithTrackingOptions();
            int allowedExceptionsBeforeBreakingCount = BogusGenerator.Random.Int(max: 0);
            TimeSpan durationOfBreak = TimeSpan.FromSeconds(5);

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.WithCircuitBreaker<Exception>(allowedExceptionsBeforeBreakingCount, durationOfBreak));
        }

        [Fact]
        public void AddCircuitBreaker_WithGreaterThanZeroTimeOfBreak_Succeeds()
        {
            // Arrange
            var options = new EventGridPublisherClientWithTrackingOptions();
            var allowedExceptionsBeforeBreakingCount = 5;
            TimeSpan durationOfBreak = BogusGenerator.Date.Timespan();

            // Act / Assert
            options.WithCircuitBreaker<Exception>(allowedExceptionsBeforeBreakingCount, durationOfBreak);
        }

        [Fact]
        public void AddCircuitBreaker_WithLessThanOrEqualToZeroTimeOfBreak_Fails()
        {
            // Arrange
            var options = new EventGridPublisherClientWithTrackingOptions();
            var allowedExceptionsBeforeBreakingCount = 5;
            TimeSpan durationOfBreak = BogusGenerator.Date.Timespan().Negate();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.WithCircuitBreaker<Exception>(allowedExceptionsBeforeBreakingCount, durationOfBreak));
        }

        [Fact]
        public void AddTelemetryContext_WithValue_Succeeds()
        {
            // Arrange
            var options = new EventGridPublisherClientWithTrackingOptions();
            var context = new Dictionary<string, object>();

            // Act / Assert
            options.AddTelemetryContext(context);
        }

        [Fact]
        public void AddTelemetryContext_WithoutValue_Fails()
        {
            // Arrange
            var options = new EventGridPublisherClientWithTrackingOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.AddTelemetryContext(telemetryContext: null));
        }
    }
}
