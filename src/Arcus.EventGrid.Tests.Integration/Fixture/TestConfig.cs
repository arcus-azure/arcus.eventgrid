using System;
using System.Collections.Generic;
using Arcus.EventGrid.Contracts;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Arcus.EventGrid.Tests.Integration.Fixture
{
    /// <summary>
    /// Configuration representation of the current test suite environment.
    /// </summary>
    public class TestConfig : IConfigurationRoot
    {
        private readonly IConfigurationRoot _config;

        private TestConfig(IConfigurationRoot configuration)
        {
            Guard.NotNull(configuration, nameof(configuration));

            _config = configuration;
        }

        /// <summary>
        /// Creates a new test implementation of the current configuration of the test suite environment.
        /// </summary>
        public static TestConfig Create()
        {
            IConfigurationRoot configuration = 
                new ConfigurationBuilder()
                    .AddEnvironmentVariables()
                    .AddJsonFile(path: "appsettings.json")
                    .AddJsonFile(path: "appsettings.local.json", optional: true)
                    .Build();

            return new TestConfig(configuration);
        }

        /// <summary>
        /// Gets the Azure Service Bus connection string for an Azure Event Grid endpoint type.
        /// </summary>
        public string GetServiceBusConnectionString()
        {
            return _config["Arcus:ServiceBus:ConnectionString"];
        }

        /// <summary>
        /// Gets the Azure Service Bus topic name for an Azure Event Grid endpoint type.
        /// </summary>
        public string GetServiceBusTopicName(EventSchema type)
        {
            return SwitchEventGridEndpointType(type, "Arcus:EventGridEvent:ServiceBus:TopicName", "Arcus:CloudEvent:ServiceBus:TopicName");
        }

        /// <summary>
        /// Gets the Event Grid topic endpoint for an Azure Event Grid endpoint type.
        /// </summary>
        public string GetEventGridTopicEndpoint(EventSchema type)
        {
            return SwitchEventGridEndpointType(type, "Arcus:EventGridEvent:EventGrid:TopicEndpoint", "Arcus:CloudEvent:EventGrid:TopicEndpoint");
        }

        /// <summary>
        /// Gets the Event Grid endpoint key for an Azure Event Grid endpoint type.
        /// </summary>
        public string GetEventGridEndpointKey(EventSchema type)
        {
            return SwitchEventGridEndpointType(type, "Arcus:EventGridEvent:EventGrid:EndpointKey", "Arcus:CloudEvent:EventGrid:EndpointKey");
        }

        private string SwitchEventGridEndpointType(EventSchema type, string eventGridEventKey, string cloudEventKey)
        {
            switch (type)
            {
                case EventSchema.EventGrid:
                    return _config[eventGridEventKey];
                case EventSchema.CloudEvent:
                    return _config[cloudEventKey];
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown Event Grid endpoint type");
            }
        }

        /// <summary>
        /// Gets a configuration sub-section with the specified key.
        /// </summary>
        /// <param name="key">The key of the configuration section.</param>
        /// <returns>The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" />.</returns>
        /// <remarks>
        ///     This method will never return <c>null</c>. If no matching sub-section is found with the specified key,
        ///     an empty <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> will be returned.
        /// </remarks>
        public IConfigurationSection GetSection(string key)
        {
            return _config.GetSection(key);
        }

        /// <summary>
        /// Gets the immediate descendant configuration sub-sections.
        /// </summary>
        /// <returns>The configuration sub-sections.</returns>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return _config.GetChildren();
        }

        /// <summary>
        /// Returns a <see cref="T:Microsoft.Extensions.Primitives.IChangeToken" /> that can be used to observe when this configuration is reloaded.
        /// </summary>
        /// <returns>A <see cref="T:Microsoft.Extensions.Primitives.IChangeToken" />.</returns>
        public IChangeToken GetReloadToken()
        {
            return _config.GetReloadToken();
        }

        /// <summary>Gets or sets a configuration value.</summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configuration value.</returns>
        public string this[string key]
        {
            get => _config[key];
            set => _config[key] = value;
        }

        /// <summary>
        /// Force the configuration values to be reloaded from the underlying <see cref="T:Microsoft.Extensions.Configuration.IConfigurationProvider" />s.
        /// </summary>
        public void Reload()
        {
            _config.Reload();
        }

        /// <summary>
        /// The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationProvider" />s for this configuration.
        /// </summary>
        public IEnumerable<IConfigurationProvider> Providers => _config.Providers;
    }
}
