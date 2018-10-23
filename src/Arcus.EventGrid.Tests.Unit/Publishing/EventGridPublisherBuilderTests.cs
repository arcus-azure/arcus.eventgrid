using System;
using Arcus.EventGrid.Publishing;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

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
            // Arrange
            const string topicEndpoint = "https://savanh-grid-lab.westcentralus-1.eventgrid.azure.net/api/events";

            // Act & Assert
            Assert.Throws<ArgumentException>( 
                () => EventGridPublisherBuilder
                      .ForTopic(topicEndpoint)
                      .UsingAuthenticationKey(authenticationKey));
        }

        [Property]
        public Property ForTopicUsingAuthentication_NonNullOrEmptyEndpointTopicAndAuthenticationKey_ShouldCreatePublisher(
            NonEmptyString endpointTopic,
            NonEmptyString endpointAuthKey)
        {
            Action act = () =>
            {
                // Act
                var publisher = EventGridPublisherBuilder
                                    .ForTopic(endpointTopic.Get)
                                    .UsingAuthenticationKey(endpointAuthKey.Get)
                                    .Build();

                // Assert
                Assert.NotNull(publisher);
                Assert.Equal(endpointTopic.Get, publisher.TopicEndpoint);
            };

            bool endpointTopicAndKeyNotEmpty = 
                !string.IsNullOrWhiteSpace(endpointTopic.Get) 
                && !string.IsNullOrWhiteSpace(endpointAuthKey.Get);

            // Conditional Property because the 'NonEmptyString' also generates line-feats which also corresponds to a null-or-whitespaced string.
            return act.When(endpointTopicAndKeyNotEmpty);
        }
    }
}
