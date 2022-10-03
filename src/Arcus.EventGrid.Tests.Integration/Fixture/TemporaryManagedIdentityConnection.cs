using System;
using GuardNet;
using Microsoft.Extensions.Configuration;

namespace Arcus.EventGrid.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a temporary active Azure Managed Identity connection.
    /// </summary>
    public class TemporaryManagedIdentityConnection : IDisposable
    {
        private const string AzureTenantIdEnvironmentVariable = "AZURE_TENANT_ID",
                             AzureServicePrincipalClientIdVariable = "AZURE_CLIENT_ID",
                             AzureServicePrincipalClientSecretVariable = "AZURE_CLIENT_SECRET";

        private TemporaryManagedIdentityConnection()
        {
        }

        /// <summary>
        /// Start a temporary active Azure Managed Identity connection.
        /// </summary>
        /// <param name="config">The test configuration to retrieve the secrets from to represent the Azure Managed Identity.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="config"/> is <c>null</c>.</exception>
        public static TemporaryManagedIdentityConnection Create(TestConfig config)
        {
            Guard.NotNull(config, nameof(config), "Requires a test configuration instance to retrieve any secrets to represent the Azure Managed Identity");

            Environment.SetEnvironmentVariable(AzureTenantIdEnvironmentVariable, config.GetValue<string>("Arcus:TenantId"));
            Environment.SetEnvironmentVariable(AzureServicePrincipalClientIdVariable, config.GetValue<string>("Arcus:ServicePrincipal:ClientId"));
            Environment.SetEnvironmentVariable(AzureServicePrincipalClientSecretVariable, config.GetValue<string>("Arcus:ServicePrincipal:ClientKey"));
            
            return new TemporaryManagedIdentityConnection();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Environment.SetEnvironmentVariable(AzureTenantIdEnvironmentVariable, null);
            Environment.SetEnvironmentVariable(AzureServicePrincipalClientIdVariable, null);
            Environment.SetEnvironmentVariable(AzureServicePrincipalClientSecretVariable, null);
        }
    }
}
