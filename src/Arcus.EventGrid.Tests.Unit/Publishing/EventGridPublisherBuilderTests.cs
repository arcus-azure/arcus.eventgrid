using System;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using Xunit;
using static Arcus.EventGrid.Tests.Unit.Publishing.Fixtures.PublishingFixures;

namespace Arcus.EventGrid.Tests.Unit.Publishing
{
    public class EventGridPublisherBuilderTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        public void ForTopic_NullOrEmptyEndpointTopic_ShouldFailWithArgumentException(string topic)
        {
            Assert.Throws<ArgumentException>(
                () => EventGridPublisherBuilder.ForTopic(topic));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        public void UsingAuthenticationKey_NullOrEmptyEndpointKey_ShouldFailWithArgumentException(string authenticationKey)
        {
            Assert.Throws<ArgumentException>(
                () => EventGridPublisherBuilder
                      .ForTopic(SampleTopicEndpoint)
                      .UsingAuthenticationKey(authenticationKey));
        }

        [Fact]
        public void ForTopicUsingAuthentication_NonNullOrEmptyEndpointTopicAndAuthenticationKey_ShouldCreatePublisher()
        {
            // Act
            IEventGridPublisher publisher =
                EventGridPublisherBuilder
                    .ForTopic(SampleTopicEndpoint)
                    .UsingAuthenticationKey(SampleAuthenticationKey)
                    .Build();

            // Assert
            Assert.NotNull(publisher);
            Assert.IsType<EventGridPublisher>(publisher);
            Assert.Equal(SampleTopicEndpoint, publisher.TopicEndpoint);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void WithExpontntialRetry_PositiveRetryCount_ShouldCreatePublisher(int retryCount)
        {
            // Act
            IEventGridPublisher publisher =
                CreateEventGridBuilder()
                    .WithExponentialRetry<Exception>(retryCount)
                    .Build();

            // Assert
            Assert.NotNull(publisher);
            Assert.IsType<EventGridPublisher>(publisher);
            Assert.Equal(SampleTopicEndpoint, publisher.TopicEndpoint);
        }

        [Fact]
        public void WithExponentialRetry_NegativeRetryCount_ShouldFailWithArgumentOutOfRangeException()
        {
            // Arrange
            const int negativeInt = -100;

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateEventGridBuilder()
                      .WithExponentialRetry<Exception>(negativeInt));
        }

        [Fact]
        public void WithCircuitBroker_PositiveCircuitBrokenDuration_ShouldCreatePublisher()
        {
            // Arrange
            TimeSpan positiveInterval = TimeSpan.FromSeconds(5);

            // Act
            IEventGridPublisher publisher =
                CreateEventGridBuilder()
                    .WithCircuitBreaker<Exception>(10, positiveInterval)
                    .Build();

            // Assert
            Assert.NotNull(publisher);
            Assert.IsType<EventGridPublisher>(publisher);
            Assert.Equal(SampleTopicEndpoint, publisher.TopicEndpoint);
        }

        [Fact]
        public void WithCircuitBroker_NegativeCircuitBrokenDuration_ShouldFailWithArgumentOutOfRangeException()
        {
            // Arrange
            TimeSpan negativeInterval = TimeSpan.FromSeconds(-10);

            // Act / Assert
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateEventGridBuilder()
                      .WithCircuitBreaker<Exception>(
                         exceptionsAllowedBeforeBreaking: 10,
                         durationOfBreak: negativeInterval));
        }

        [Fact]
        public void WithExponentialRetryWithCircuitBroker_NegativeCircuitBrokenDuration_ShouldFailWithArgumentOutOfRangeException()
        {
            // Arrange
            TimeSpan negativeInterval = TimeSpan.FromDays(-1);

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateEventGridBuilder()
                      .WithExponentialRetry<Exception>(3)
                      .WithCircuitBreaker<Exception>(
                         exceptionsAllowedBeforeBreaking: 10,
                         durationOfBreak: negativeInterval));
        }

        [Fact]
        public void WithCircuitBroker_NegativeExceptionsAllowedBeforeBreaking_ShouldFailWithArgumentOutOfRangeException()
        {
            // Arrange
            const int negativeInt = -5;

            // Act / Assert
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateEventGridBuilder()
                      .WithCircuitBreaker<NotSupportedException>(
                         exceptionsAllowedBeforeBreaking: negativeInt,
                         durationOfBreak: TimeSpan.MaxValue));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-11)]
        [InlineData(-65)]
        public void WithExponentialRetryWithCircuitBroker_NegativeExceptionsAllowedBeforBreaking_ShouldFailWithArgumentOutOfRangeException(
            int negativeInt)
        {
            // Act / Assert
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateEventGridBuilder()
                      .WithExponentialRetry<ArgumentException>(3)
                      .WithCircuitBreaker<InvalidOperationException>(
                         exceptionsAllowedBeforeBreaking: negativeInt,
                         durationOfBreak: TimeSpan.MaxValue));
        }

        [Fact]
        public void WithExponentialRetryWithCircuitBroker_PositiveExceptionsAllowedBeforeBreakAndCircuitBrokenDuration_ShouldCreatePublisher()
        {
            // Arrange
            const int positiveInt = 10;
            TimeSpan positiveInterval = TimeSpan.FromSeconds(5);

            // Act
            IEventGridPublisher publisher =
                CreateEventGridBuilder()
                    .WithExponentialRetry<Exception>(6)
                    .WithCircuitBreaker<Exception>(positiveInt, positiveInterval)
                    .Build();

            // Assert
            Assert.NotNull(publisher);
            Assert.IsType<EventGridPublisher>(publisher);
            Assert.Equal(SampleTopicEndpoint, publisher.TopicEndpoint);
        }

        private static IEventGridPublisherBuilderWithExponentialRetry CreateEventGridBuilder()
        {
            return EventGridPublisherBuilder
                .ForTopic(SampleTopicEndpoint)
                .UsingAuthenticationKey(SampleAuthenticationKey);
        }
    }
}
