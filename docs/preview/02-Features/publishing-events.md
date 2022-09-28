---
title: "Publishing Events"
layout: default
---

# Azure Event Grid publishing
We provide support for publishing Azure EventGrid, CloudEvents and custom events to a custom Azure Event Grid topics. 
This event publishing builds on top of the existing [`EventGridPublisherClient`](https://www.nuget.org/packages/Azure.Messaging.EventGrid/) that can either be added to the application's dependency container. The injected Azure client is a great way to centralize your Azure interaction in the startup code of your application.
Arcus enhances the existing Event Grid publisher with dependency tracking that can be build up to a full service-to-service correlation model.

## Installation
The features described here require the following package:

```shell
PM> Install-Package Arcus.EventGrid.Core
```
> **⚠ Publishing events used to be in the package called `Arcus.EventGrid.Publishing`. Please make sure that you migrate towards `Arcus.EventGrid.Core` as it's not being actively maintained beyond v3.2.**

## Usage
Adding simple Azure EventGrid publishing to your application only requires the following registration.
> ⚠ Note that registering using non-managed identity authentication requires the [Arcus secrect store](https://security.arcus-azure.net/features/secret-store) to retrieve the necessary authentication secrets to interact with the Azure EventGrid topic.
> ⚠ Note that this way of registering requires the [Arcus correlation](https://observability.arcus-azure.net/Features/correlation) to retrieve the current application's correlation model to enrich the publishing events.

```csharp
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Requires Arcus secret store if not using managed identity, for more information, see: https://security.arcus-azure.net/features/secret-store
    services.AddSecretStore(stores => ...);

    // Requires Arcus correation, for more information, see: https://observability.arcus-azure.net/Features/correlation
    services.AddCorrelation();

    // For Arcus HTTP correlation, see: https://webapi.arcus-azure.net/features/correlation
    services.AddHttpCorrelation();

    // Registers an `EventGridPublisherClient` to your application to a custom topic.
    services.AddAzureClients(clients =>
    {
        clients.AddEventGridPublisherClientUsingManagedIdentity(
            // Custom Azure Even Grid topic endpoint:
            "https://my-eventgrid-topic-endpoint");

        clients.AddEventGridPublisherClient(
            // Custom Azure Even Grid topic endpoint:
            "https://my-eventgrid-topic-endpoint", 
            // Secret name where the authentication key to interact with Azure Event Grid is stored in the Arcus secret store.
            "<my-authentication-secret-name>");
    });
}
```

## Publishing events
Publishing events has no difference than only using Microsoft's `EventGridPublisherClient`, as the correlation tracking happens internally.

To publish events, inject the client and choose between publishing cloud events, Event Grid events or custom events.
```csharp
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Azure;

public class MyService
{
    public MyService(IAzureClientFactory<EventGridPublisherClient> clientFactory)
    {
        EventGridPublisherClient client = clientFactory.CreateClient("Default");

        client.SendEventAsync(new CloudEvent(...));

        client.SendEventAsync(new EventGridEvent(...));

        // And other overloads...
    }
}
```

For more information on Azure clients, how they work and how to use them, see [Microsoft's documentation](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/microsoft.extensions.azure-readme-pre).

## Configuration
The Arcus `EventGridPublisher` builds on top of the existing [`EventGridPublisherClientOptions`](https://docs.microsoft.com/en-us/dotnet/api/azure.messaging.eventgrid.eventgridpublisherclientoptions?view=azure-dotnet) to provide additional options related to corelation and resilience.

### Resilient Publishing
The library also provides several ways to publish events in a resilient manner. Resilient meaning we support three ways to add resilience to your event publishing:

#### Exponential retry
This makes the publishing resilient by retrying a specified number of times with exponential back off.

Adding exponential retry to your `EventGridPublisherClient` implementation can easily be done via the options:

```csharp
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
     // Registers an `EventGridPublisherClient` to your application to a custom topic.
    services.AddAzureClients(clients =>
    {
        clients.AddEventGridPublisherClient(
            // Custom Azure Even Grid topic endpoint:
            "https://my-eventgrid-topic-endpoint", 
            // Secret name where the authentication key to interact with Azure Event Grid is stored in the Arcus secret store.
            "<my-authentication-secret-name>",
            options =>
            {
                options.WithExponentialRetry<HttpRequestException>(retryCount: 3);
            });
    });
}
```

#### Circuit breaker
This makes the publishing resilient by breaking the circuit if the maximum specified number of exceptions are handled by the policy. The circuit will stay broken for a specified duration. Any attempt to execute the function while the circuit is broken will result in a `BrokenCircuitException`.

Adding circuit breaker retry to your `EventGridPublisherClient` implementation can easily be done via the options:

```csharp
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Registers an `EventGridPublisherClient` to your application to a custom topic.
    services.AddAzureClients(clients =>
    {
        clients.AddEventGridPublisherClient(
            // Custom Azure Even Grid topic endpoint:
            "https://my-eventgrid-topic-endpoint", 
            // Secret name where the authentication key to interact with Azure Event Grid is stored in the Arcus secret store.
            "<my-authentication-secret-name>",
            options =>
            {
                options.WithCircuitBreaker<RequestFailedException>(
                    exceptionsBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(5));
            });
    });
}
```

Adding circuit breaker resilience is also available when building the publisher yourself:

#### Combination
Both exponential back-off and circuit breaker resilience cam be combined together. Use the available extensions to guide you in this process.

```csharp
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Registers an `EventGridPublisherClient` to your application to a custom topic.
    services.AddAzureClients(clients =>
    {
        clients.AddEventGridPublisherClient(
            // Custom Azure Even Grid topic endpoint:
            "https://my-eventgrid-topic-endpoint", 
            // Secret name where the authentication key to interact with Azure Event Grid is stored in the Arcus secret store.
            "<my-authentication-secret-name>",
            options =>
            {
                options.WithExponentialRetry<RequestFailedException>(retryCount: 3)
                       .WithCircuitBreaker<RequestFailedException>(
                           exceptionsBeforeBreaking: 3,
                           durationOfBreak: TimeSpan.FromSeconds(5));
            });
    });
}
```

### Correlation tracking
The library also provides several ways to influence the correlation tracking during the event publishing. 
By default, each event that gets published will be enriched with two correlation properties: transaction ID and the operation parent ID. These two properties should be configured as [custom delivery properties](https://docs.microsoft.com/en-us/azure/event-grid/delivery-properties) in Azure Event Grid so that the event processor can easily access them and continue to correlate the message.

> ⚠ The Arcus `EventGridPublisherClient` assumes that every event published is an JSON event with `data` JSON node.

If the following cloud event gets published:
```json
{
    "specversion": "1.0",
    "type": "Microsoft.Storage.BlobCreated",  
    "source": "/subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.Storage/storageAccounts/{storage-account}",
    "id": "9aeb0fdf-c01e-0131-0922-9eb54906e209",
    "time": "2019-11-18T15:13:39.4589254Z",
    "subject": "blobServices/default/containers/{storage-container}/blobs/{new-file}",    
    "data": {
        "api": "PutBlockList",
        "clientRequestId": "4c5dd7fb-2c48-4a27-bb30-5361b5de920a",
        "requestId": "9aeb0fdf-c01e-0131-0922-9eb549000000",
        "eTag": "0x8D76C39E4407333",
        "contentType": "image/png",
        "contentLength": 30699,
        "blobType": "BlockBlob",
        "url": "https://gridtesting.blob.core.windows.net/testcontainer/{new-file}",
        "sequencer": "000000000000000000000000000099240000000000c41c18",
        "storageDiagnostics": {
            "batchId": "681fe319-3006-00a8-0022-9e7cde000000"
        }
    }
}
```

Then the Arcus `EventGridPublisherClient` will alter the event data as follows:
```json
{
    "specversion": "1.0",
    "type": "Microsoft.Storage.BlobCreated",  
    "source": "/subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.Storage/storageAccounts/{storage-account}",
    "id": "9aeb0fdf-c01e-0131-0922-9eb54906e209",
    "time": "2019-11-18T15:13:39.4589254Z",
    "subject": "blobServices/default/containers/{storage-container}/blobs/{new-file}",    
    "data": {
        "api": "PutBlockList",
        "clientRequestId": "4c5dd7fb-2c48-4a27-bb30-5361b5de920a",
        "requestId": "9aeb0fdf-c01e-0131-0922-9eb549000000",
        "eTag": "0x8D76C39E4407333",
        "contentType": "image/png",
        "contentLength": 30699,
        "blobType": "BlockBlob",
        "url": "https://gridtesting.blob.core.windows.net/testcontainer/{new-file}",
        "sequencer": "000000000000000000000000000099240000000000c41c18",
        "storageDiagnostics": {
            "batchId": "681fe319-3006-00a8-0022-9e7cde000000"
        },
        "transactionId": "<your-transaction-id>",
        "operationParentId": "<your-dependency-id>"
    }
}
```

These properties can be accessed with `data.transactionId` and `data.operationParentId` in your Azure Event Grid custom delivery properties of your event subscription.
The example shows how these properties are transfered to application properties `Transaction-Id` and `Operation-Parent-Id`. These are the default correlation properties of the [Arcus message pump](https://messaging.arcus-azure.net/).
Use the correct correlation properties of your event processor system to correctly correlate the event.

![Azure Event Grid custom delivery properties](/media/event-grid-delivery-properties.png)

Several other options related to correlation can be configured on the client:
```csharp
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Registers an `EventGridPublisherClient` to your application to a custom topic.
    services.AddAzureClients(clients =>
    {
        clients.AddEventGridPublisherClient(
            // Custom Azure Even Grid topic endpoint:
            "https://my-eventgrid-topic-endpoint", 
            // Secret name where the authentication key to interact with Azure Event Grid is stored in the Arcus secret store.
            "<my-authentication-secret-name>",
            options =>
            {
                // The name of the JSON property that represents the transaction ID that will be added to the event data of the published event (default: transactionId).
                options.TransactionIdEventDataPropertyName = "customTransactionId";

                // The name of the JSON property that represents the operation parent ID that will be added to the event data of the published event (default: operationParentId).
                options.UpstreamServicePropertyName = "customOperationParentId";

                // The function to generate the dependency ID used when tracking the event publishing.
                // This value corresponds with the operation parent ID on the receiver side, and is called the dependency ID on this side (sender).
                options.GenerateDependencyId = () => $"custom-dependency-id-{Guid.NewGuid()}";

                // Adds a telemetry context while tracking the Azure Event Grid dependency.
                options.AddTelemetryContext(new Dictionary<string, object>
                {
                    ["My-additional-tracking"] = "my-additional-tracking-value"
                });

                // The flag indicating whether or not the client should track the Azure Event Grid topic dependency (default: true).
                options.EnableDependencyTracking = false;
            });
    });
}
```

#### Custom correlation property assignment
By default, the correlation properties are added to the JSON `data` node of the event. In situations where you want to control this behavior, you can inherit from Arcus' `EventGridPublisherClientWithTracking` and override this behavior.

This example shows how the correlation property is added to the `data.telemetry` node instead of the `data` node of a cloud event. Note that the same override behavior is available for Event Grid events and custom events.

```csharp
using System.Text.Json.Nodes
using Azure.Messaging.EventGrid;

public class CustomEventGridPublisherClientWithTracking : EventGridPublisherClientWithTracking
{
    public CustomEventGridPublisherClientWithTracking(
    string topicEndpoint, 
        string authenticationKeySecretName, 
        ISecretProvider secretProvider, 
        ICorrelationInfoAccessor correlationAccessor,
        EventGridPublisherClientWithTrackingOptions options,
        ILogger<EventGridPublisherClient> logger) 
        : base(topicEndpoint, authenticationKeySecretName, secretProvider, correlationAccessor, options, logger)
        {
        }

        protected override CloudEvent SetCorrelationPropertyInCloudEvent(CloudEvent cloudEvent, string propertyName, string propertyValue)
        {
            JsonNode dataNode = JsonNode.Parse(cloudEvent.Data);
            dataNode["data"]["telemetry"][propertyName] = propertyValue;

            cloudEvent.Data = BinaryData.FromString(dataNode.ToJsonString());
            return cloudEvent;
        }
}
```

Custom implementations of `EventGridPublisherClientWithTracking` can be registered with one of the registration overloads. The same additional options are available if you want to change the custom implementation even further:
```csharp
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Registers an `EventGridPublisherClient` to your application to a custom topic.
    services.AddAzureClients(clients =>
    {
        clients.AddEventGridPublisherClient(provider =>
        {
            return new CustomEventGridPublisherClientWithTracking(
                "https://my-eventgrid-topic-endpoint",
                "<my-authentication-secret-name>",
                provider.GetRequiredService<ISecretProvider>(),
                provider.GetRequiredService<ICorrelationInfoAccessor>(),
                new EventGridPublisherClientWithTrackingOptions(),
                provider.GetRequiredService<ILogger<EventGridPublisherClient>>());
        });

        // Or with additional options:
        clients.AddEventGridPublisherClient(
            options => { ... },
            (provider, options) =>
            {
                return new CustomEventGridPublisherClientWithTracking(
                    "https://my-eventgrid-topic-endpoint",
                    "<my-authentication-secret-name>",
                    provider.GetRequiredService<ISecretProvider>(),
                    provider.GetRequiredService<ICorrelationInfoAccessor>(),
                    options,
                    provider.GetRequiredService<ILogger<EventGridPublisherClient>>());
            });
    });
}
```