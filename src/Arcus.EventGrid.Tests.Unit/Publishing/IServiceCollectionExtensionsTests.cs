using System;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Publishing
{
    // ReSharper disable once InconsistentNaming
    public class IServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddEventGridPublishing_WithValidArguments_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSecretStore(stores => stores.AddInMemory());

            // Act
            services.AddEventGridPublishing("https://topic-endpoint", "<auth-secret-name>");

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var publisher = provider.GetService<IEventGridPublisher>();
            Assert.NotNull(publisher);
            Assert.IsType<EventGridPublisher>(publisher);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddEventGridPublishing_WithoutTopicEndpoint_Fails(string topicEndpoint)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddEventGridPublishing(topicEndpoint, "<auth-secret-name>"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddEventGridPublishing_WithoutAuthenticationSecretName_Fails(string authenticationSecretName)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddEventGridPublishing("https://topic-url", authenticationSecretName));
        }

        [Theory]
        [InlineData("something not a HTTP endpoint ☺")]
        [InlineData("11304-asdf-123123-sdafsd")]
        [InlineData("test.be")]
        [InlineData("sftp://some-FTPS-uri")]
        [InlineData("file:///C:\\temp\\dir")]
        [InlineData("net.tcp://localhost:55509")]
        public void AddEventGridPublishing_WithInvalidTopicEndpoint_Fails(string topicEndpoint)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<UriFormatException>(
                () => services.AddEventGridPublishing(topicEndpoint, "<auth-secret-name>"));
        }

        [Fact]
        public void AddEventGridPublishingWithOptions_WithValidArguments_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSecretStore(stores => stores.AddInMemory());

            // Act
            services.AddEventGridPublishing("https://topic-endpoint", "<auth-secret-name>", options => { });

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var publisher = provider.GetService<IEventGridPublisher>();
            Assert.NotNull(publisher);
            Assert.IsType<EventGridPublisher>(publisher);
        }

        [Fact]
        public void AddEventGridPublishing_WithoutSecretStore_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddEventGridPublishing("https://topic-endpoint", "<auth-secret-name>");

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.ThrowsAny<InvalidOperationException>(() => provider.GetRequiredService<IEventGridPublisher>());
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddEventGridPublishingWithOptions_WithoutTopicEndpoint_Fails(string topicEndpoint)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddEventGridPublishing(topicEndpoint, "<auth-secret-name>", options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddEventGridPublishingWithOptions_WithoutAuthenticationSecretName_Fails(string authenticationSecretName)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                services.AddEventGridPublishing("https://topic-url", authenticationSecretName, options => { }));
        }

        [Theory]
        [InlineData("something not a HTTP endpoint ☺")]
        [InlineData("11304-asdf-123123-sdafsd")]
        [InlineData("test.be")]
        [InlineData("sftp://some-FTPS-uri")]
        [InlineData("file:///C:\\temp\\dir")]
        [InlineData("net.tcp://localhost:55509")]
        public void AddEventGridPublishingWithOptions_WithInvalidTopicEndpoint_Fails(string topicEndpoint)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<UriFormatException>(
                () => services.AddEventGridPublishing(topicEndpoint, "<auth-secret-name>", options => { }));
        }

        [Fact]
        public void AddEventGridPublishingWithOptions_WithoutSecretStore_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddEventGridPublishing("https://topic-endpoint", "<auth-secret-name>", options => { });

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.ThrowsAny<InvalidOperationException>(() => provider.GetRequiredService<IEventGridPublisher>());
        }
    }
}
