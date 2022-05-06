using System;
using System.Net.Http;
using Arcus.EventGrid.Publishing;
using Arcus.EventGrid.Publishing.Interfaces;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Publishing
{
    public class EventGridPublishingServiceCollectionExtensionsTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void AddExponentialRetry_WithValidArguments_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            int retryCount = BogusGenerator.Random.Int(min: 0);

            EventGridPublishingServiceCollection collection = 
                services.AddSecretStore(stores => stores.AddInMemory())
                        .AddEventGridPublishing("https://topic-endpoint", "<auth-secert-name>");

            // Act
            collection.WithExponentialRetry<ApplicationException>(retryCount);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var publishers = provider.GetServices<IEventGridPublisher>();
            IEventGridPublisher publisher = Assert.Single(publishers);
            Assert.NotNull(publisher);
            Assert.IsNotType<EventGridPublisher>(publisher);
        }

        [Fact]
        public void AddExponentialRetry_WithLessThanZeroRetryCount_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            int retryCount = BogusGenerator.Random.Int(max: 0);

            EventGridPublishingServiceCollection collection = 
                services.AddEventGridPublishing("https://topic-endpoint", "<auth-secret-name>");
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => collection.WithExponentialRetry<AccessViolationException>(retryCount));
        }

        [Fact]
        public void AddCircuitBreaker_WithValidArguments_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            int exceptionsAllowedBeforeBreaking = BogusGenerator.Random.Int(min: 0);
            var durationOfBreak = new TimeSpan(BogusGenerator.Random.Long(TimeSpan.Zero.Ticks, TimeSpan.MaxValue.Ticks));

            EventGridPublishingServiceCollection collection =
                services.AddSecretStore(stores => stores.AddInMemory())
                        .AddEventGridPublishing("https://topic-endpoint", "<auth-secert-name>");

            // Act
            collection.WithCircuitBreaker<ApplicationException>(exceptionsAllowedBeforeBreaking, durationOfBreak);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var publishers = provider.GetServices<IEventGridPublisher>();
            IEventGridPublisher publisher = Assert.Single(publishers);
            Assert.NotNull(publisher);
            Assert.IsNotType<EventGridPublisher>(publisher);
        }

        [Fact]
        public void AddCircuitBreaker_WithLessThanOrEqualExceptionsAllowedBeforeBreaking_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            int exceptionsAllowedBeforeBreaking = BogusGenerator.Random.Int(max: 1);
            TimeSpan durationOfBreak = TimeSpan.FromSeconds(5);

            EventGridPublishingServiceCollection collection = 
                services.AddEventGridPublishing("https://topic-endpoint", "<auth-secret-name>");

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => collection.WithCircuitBreaker<InvalidOperationException>(exceptionsAllowedBeforeBreaking, durationOfBreak));
        }

        [Fact]
        public void AddCircuitBreaker_WithNegativeDurationOfBreak_Fails()
        {
            // Arrange
            var services = new ServiceCollection();
            int exceptionsAllowedBeforeBreaking = BogusGenerator.Random.Int(min: 0);
            var durationOfBreak = new TimeSpan(BogusGenerator.Random.Long(TimeSpan.MinValue.Ticks, TimeSpan.Zero.Ticks));

            EventGridPublishingServiceCollection collection = 
                services.AddEventGridPublishing("https://topic-endpoint", "<auth-secret-name>");

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                collection.WithCircuitBreaker<InvalidCastException>(exceptionsAllowedBeforeBreaking, durationOfBreak));
        }

        [Fact]
        public void AddExponentialRetryAfterCircuitBreaker_WithValidArguments_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            int exceptionsAllowedBeforeBreaking = BogusGenerator.Random.Int(min: 1);
            TimeSpan durationOfBreak = TimeSpan.FromSeconds(5);

            EventGridPublishingServiceCollection collection =
                services.AddSecretStore(stores => stores.AddInMemory())
                        .AddEventGridPublishing("https://topic-endpoint", "<auth-secret-name>")
                        .WithExponentialRetry<HttpRequestException>(retryCount: 2);

            // Act
            collection.WithCircuitBreaker<HttpRequestException>(exceptionsAllowedBeforeBreaking, durationOfBreak);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var publishers = provider.GetServices<IEventGridPublisher>();
            IEventGridPublisher publisher = Assert.Single(publishers);
            Assert.NotNull(publisher);
            Assert.IsNotType<EventGridPublisher>(publisher);
        }

        [Fact]
        public void ResilientDecorators_WithPreviouslyRegisteredPublisher_OnlyRemovesDecoratedImplementation()
        {
            // Arrange
            // Arrange
            var services = new ServiceCollection();
            int exceptionsAllowedBeforeBreaking = BogusGenerator.Random.Int(min: 1);
            TimeSpan durationOfBreak = TimeSpan.FromSeconds(5);

            services.AddSecretStore(stores => stores.AddInMemory())
                    .AddEventGridPublishing("https://topic-endpiont", "<auth-secret-name>");

            EventGridPublishingServiceCollection collection = 
                services.AddEventGridPublishing("https://topic-endpoint", "<auth-secret-name>")
                        .WithExponentialRetry<HttpRequestException>(retryCount: 2);

            // Act
            collection.WithCircuitBreaker<HttpRequestException>(exceptionsAllowedBeforeBreaking, durationOfBreak);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            var publishers = provider.GetServices<IEventGridPublisher>();
            Assert.Collection(
                publishers,
                publisher => Assert.IsType<EventGridPublisher>(publisher),
                publisher => Assert.IsNotType<EventGridPublisher>(publisher));
        }
    }
}
