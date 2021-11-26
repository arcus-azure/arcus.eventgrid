using System;
using Arcus.EventGrid.Security.Core.Validation;
using Arcus.EventGrid.Tests.Unit.Security.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Security.Extensions
{
    public class IServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddEventGridSubscriptionValidation_WithServices_RegistersInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddEventGridSubscriptionValidation();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IEventGridSubscriptionValidator>());
        }

        [Fact]
        public void AddEventGridSubscriptionValidation_WithFuntionHostServices_RegisteresInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new StubFunctionsHostBuilder(services);
            services.AddLogging();

            // Act
            builder.AddEventGridSubscriptionValidation();

            // Assert
            IServiceProvider provider = builder.Services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IEventGridSubscriptionValidator>());
        }
    }
}
