using Xunit;
using System;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
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
            Assert.Throws<ArgumentException>(() => EventGridPublisherBuilder.ForTopic(topic));
        }

        [Theory]
        [InlineData("something not a HTTP endpoint ☺")]
        [InlineData("11304-asdf-123123-sdafsd")]
        [InlineData("test.be")]
        public void ForTopic_NonUriEndpointTopic_ShouldFailWithInvalidOperationException(string topic)
        {
            Assert.Throws<UriFormatException>(() => EventGridPublisherBuilder.ForTopic(topic));
        }

        [Theory]
        [InlineData("sftp://some-FTPS-uri")]
        [InlineData("file:///C:\\temp\\dir")]
        [InlineData("net.tcp://localhost:55509")]
        public void ForTopic_NonHttpEndpointTopic_ShouldFailWithUriFormatException(string topic)
        {
            Assert.Throws<UriFormatException>(() => EventGridPublisherBuilder.ForTopic(topic));
        }

        [Theory]
        [InlineData("sftp://some-FTPS-uri")]
        [InlineData("file:///C:\\temp\\dir")]
        [InlineData("net.tcp://localhost:55509")]
        public void ForTopic_NonHttpEndpointTopic_WitUriOverload_ShouldFailWithUriFormatException(string topic)
        {
            var uri = new Uri(topic);
            Assert.Throws<UriFormatException>(() => EventGridPublisherBuilder.ForTopic(uri));
        }

        [Theory]
        [InlineData("http://some-http-topic-endpoint")]
        [InlineData("http://some-https-topic-endpoint")]
        [InlineData(SampleTopicEndpoint)]
        public void ForTopic_HttpEndpointTopic_WithUriOverload_ShouldCreatePublisher(string topic)
        {
            var uri = new Uri(topic);
            IEventGridPublisher publisher = 
                EventGridPublisherBuilder
                    .ForTopic(uri)
                    .UsingAuthenticationKey(SampleAuthenticationKey)
                    .Build();

            Assert.NotNull(publisher);
            Assert.IsType<EventGridPublisher>(publisher);
            Assert.Equal(topic, publisher.TopicEndpoint);
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
            var publisher = EventGridPublisherBuilder
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
        public void WithExponentialRetry_PositiveRetryCount_ShouldCreatePublisher(int retryCount)
        {
            // Act
            var publisher = CreateEventGridBuilder()
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
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateEventGridBuilder()
                                                                .WithExponentialRetry<Exception>(negativeInt));
        }

        [Fact]
        public void WithCircuitBroker_PositiveCircuitBrokenDuration_ShouldCreatePublisher()
        {
            // Arrange
            var positiveInterval = TimeSpan.FromSeconds(5);

            // Act
            var publisher =CreateEventGridBuilder()
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
            var negativeInterval = TimeSpan.FromSeconds(-10);

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
            var negativeInterval = TimeSpan.FromDays(-1);

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
            var positiveInterval = TimeSpan.FromSeconds(5);

            // Act
            var publisher =
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
