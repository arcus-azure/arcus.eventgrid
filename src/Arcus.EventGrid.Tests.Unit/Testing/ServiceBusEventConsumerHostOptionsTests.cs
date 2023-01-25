using System;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Bogus;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Testing
{
    public class ServiceBusEventConsumerHostOptionsTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void Create_WithDefault_Succeeds()
        {
            // Arrange
            string topicPath = BogusGenerator.Internet.UrlWithPath();
            string connectionString = BogusGenerator.Lorem.Sentence();

            // Act
            var options = new ServiceBusEventConsumerHostOptions(topicPath, connectionString);

            // Assert
            Assert.NotNull(options.SubscriptionName);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void Create_WithoutTopicPath_Fails(string topicPath)
        {
            // Arrange
            string connectionString = BogusGenerator.Lorem.Sentence();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => new ServiceBusEventConsumerHostOptions(topicPath, connectionString));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void Create_WithoutConnectionString_Fails(string connectionString)
        {
            // Arrange
            string topicPath = BogusGenerator.Internet.UrlWithPath();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => new ServiceBusEventConsumerHostOptions(topicPath, connectionString));
        }
    }
}
