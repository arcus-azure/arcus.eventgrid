using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.EventGrid.Tests.Unit.Security.Fixture
{
    /// <summary>
    /// Represents a stubbed-out Azure Functions host builder instance.
    /// </summary>
    public class StubFunctionsHostBuilder : IFunctionsHostBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StubFunctionsHostBuilder"/> class.
        /// </summary>
        /// <param name="services">The stubbed-out registered application services for the Azure Functions application.</param>
        public StubFunctionsHostBuilder(IServiceCollection services)
        {
            Services = services;
        }

        /// <summary>
        /// Gets the available registered services collection for the Azure Functions application.
        /// </summary>
        public IServiceCollection Services { get; }
    }
}
