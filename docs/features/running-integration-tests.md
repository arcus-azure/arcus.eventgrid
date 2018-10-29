---
title: "Running integration tests with Arcus"
layout: default
---

## Running integration tests with Arcus

We provide some minimal testing infrastructure that allows you to run integration tests on top of Azure Event Grid.

Easy to start:
```shell
> Install-Package Arcus.EventGrid.Testing
```

### Receiving events in your tests
By using the `HybridConnectionHost` you can subscribe to Azure Event Grid events on a custom topic and consume them in your tests. This is achieved by setting up a [Azure Relay Hybrid Connection](https://docs.microsoft.com/en-us/azure/service-bus-relay/relay-what-is-it#hybrid-connections) which is subscribing for events and storing them internally in the host.

You can easily check if an event is received:
```csharp
_hybridConnectionHost.GetReceivedEvent(eventId, retryCount: 5)
```

### Example
Here is an example of how you can setup the `HybridConnectionHost` and receive query for events in your tests:
```csharp
[Trait(name: "Category", value: "Integration")]
public class EventPublishingTests : IAsyncLifetime
{
    private HybridConnectionHost _hybridConnectionHost;

    protected IConfiguration Configuration { get; }

    public async Task DisposeAsync()
    {
        await _hybridConnectionHost.Stop();
    }

    public async Task InitializeAsync()
    {
        var relayNamespace = "<azure-relay-namespace-name>";
        var hybridConnectionName = "<hybrid-connection-name>";
        var accessPolicyName = "<access-policy-name>";
        var accessPolicyKey = "<access-policy-key>";

        _hybridConnectionHost = await HybridConnectionHost.Start(relayNamespace, hybridConnectionName, accessPolicyName, accessPolicyKey);
    }

    [Fact]
    public async Task Publish_ValidParameters_Succeeds()
    {
        // Arrange
        var topicEndpoint = "<topic-endpoint>";
        var endpointKey = "<endpoint-key>";
        const string licensePlate = "1-TOM-337";
        string eventSubject = $"/cars/{licensePlate}";
        string eventId = Guid.NewGuid().ToString();
        var @event = new NewCarRegistered(eventId, eventSubject, licensePlate);

        // Act
        var eventGridPublisher = EventGridPublisherBuilder
                                        .ForTopic(topicEndpoint)
                                        .UsingAuthenticationKey(endpointKey)
                                        .Build();
        await eventGridPublisher.Publish(@event);

        // Assert
        var receivedEvent = _hybridConnectionHost.GetReceivedEvent(eventId);
        Assert.NotEmpty(receivedEvent);
    }
}
```

[&larr; back](/)