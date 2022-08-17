using System;
using Arcus.EventGrid.Tests.Unit.Publishing.Fixtures;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Publishing
{
    public class AzureClientFactoryBuilderExtensionsTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddEventGridPublisherClient_WithoutTopicEndpoint_Fails(string topicEndpoint)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAzureClients(
                    clients => clients.AddEventGridPublisherClient(topicEndpoint, "<authentication-key-secret-name>")));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddEventGridPublisherClient_WithoutAuthenticationKeySecretName_Fails(string authenticationKeySecretName)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAzureClients(
                    clients => clients.AddEventGridPublisherClient("<topic-endpoint>", authenticationKeySecretName)));
        }

        [Fact]
        public void AddEventGridPublisherClient_WithoutSecretStore_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddCorrelation();

            // Act
            services.AddAzureClients(
                clients => clients.AddEventGridPublisherClient("<topic-endpoint>", "<authentication-key>"));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();
            Assert.ThrowsAny<InvalidOperationException>(() => factory.CreateClient("Default"));
        }

        [Fact]
        public void AddEventGridPublisherClient_WithoutCorrelation_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSecretStore(stores => stores.AddInMemory());

            // Act
            services.AddAzureClients(
                clients => clients.AddEventGridPublisherClient("<topic-endpoint>", "<authentication-key>"));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();
            Assert.ThrowsAny<InvalidOperationException>(() => factory.CreateClient("Default"));
        }

        [Fact]
        public void AddEventGridPublisherClient_WithTopicEndpointAndAuthenticationKeySecretNameWithSecretStoreWithCorrelation_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddCorrelation();
            services.AddSecretStore(stores => stores.AddInMemory());

            // Act
            services.AddAzureClients(
                clients => clients.AddEventGridPublisherClient("<topic-endpoint>", "<authentication-key>"));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();
            EventGridPublisherClient client = factory.CreateClient("Default");
            Assert.NotNull(client);
            Assert.IsType<EventGridPublisherClientWithTracking>(client);
        }

         [Theory]
        [ClassData(typeof(Blanks))]
        public void AddEventGridPublisherClientWithOptions_WithoutTopicEndpoint_Fails(string topicEndpoint)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAzureClients(
                    clients => clients.AddEventGridPublisherClient(topicEndpoint, "<authentication-key-secret-name>", options => { })));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddEventGridPublisherClientWithOptions_WithoutAuthenticationKeySecretName_Fails(string authenticationKeySecretName)
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAzureClients(
                    clients => clients.AddEventGridPublisherClient("<topic-endpoint>", authenticationKeySecretName, options => { })));
        }

        [Fact]
        public void AddEventGridPublisherClientWithOptions_WithoutSecretStore_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddCorrelation();

            // Act
            services.AddAzureClients(
                clients => clients.AddEventGridPublisherClient("<topic-endpoint>", "<authentication-key>", options => { }));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();
            Assert.ThrowsAny<InvalidOperationException>(() => factory.CreateClient("Default"));
        }

        [Fact]
        public void AddEventGridPublisherClientWithOptions_WithoutCorrelation_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSecretStore(stores => stores.AddInMemory());

            // Act
            services.AddAzureClients(
                clients => clients.AddEventGridPublisherClient("<topic-endpoint>", "<authentication-key>", options => { }));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();
            Assert.ThrowsAny<InvalidOperationException>(() => factory.CreateClient("Default"));
        }

        [Fact]
        public void AddEventGridPublisherClientWithOptions_WithTopicEndpointAndAuthenticationKeySecretNameWithSecretStoreWithCorrelation_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddCorrelation();
            services.AddSecretStore(stores => stores.AddInMemory());

            // Act
            services.AddAzureClients(
                clients => clients.AddEventGridPublisherClient("<topic-endpoint>", "<authentication-key>", options => { }));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();
            EventGridPublisherClient client = factory.CreateClient("Default");
            Assert.NotNull(client);
            Assert.IsType<EventGridPublisherClientWithTracking>(client);
        }

        [Fact]
        public void AddEventGridPublisherClientWithCustomImplementation_WithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAzureClients(
                    clients => clients.AddEventGridPublisherClient<EventGridPublisherClientWithTracking>(implementationFactory: null)));
        }

        [Fact]
        public void AddEventGridPublisherClientWithCustomImplementation_WithImplementationFactory_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddAzureClients(clients => clients.AddEventGridPublisherClient(provider => new StubEventGridPublisherClientWithTracking()));

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();
            EventGridPublisherClient client = factory.CreateClient("Default");
            Assert.NotNull(client);
            Assert.IsType<StubEventGridPublisherClientWithTracking>(client);
        }

        [Fact]
        public void AddEventGridPublisherClientWithCustomImplementationWithOptions_WithoutImplementationFactory_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddAzureClients(
                    clients => clients.AddEventGridPublisherClient<EventGridPublisherClientWithTracking>(options => { }, implementationFactory: null)));
        }

        [Fact]
        public void AddEventGridPublisherClientWithCustomImplementationWithOptions_WithImplementationFactory_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSecretStore(stores => stores.AddInMemory());

            // Act
            services.AddAzureClients(clients =>
            {
                clients.AddEventGridPublisherClient(options => { }, (provider, options) => new StubEventGridPublisherClientWithTracking());
            });

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IAzureClientFactory<EventGridPublisherClient>>();
            EventGridPublisherClient client = factory.CreateClient("Default");
            Assert.NotNull(client);
            Assert.IsType<StubEventGridPublisherClientWithTracking>(client);
        }
    }
}