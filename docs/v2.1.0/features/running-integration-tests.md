---
title: "Running integration tests with Arcus"
layout: default
---

## Running integration tests with Arcus

We provide some minimal testing infrastructure that allows you to run integration tests on top of Azure Event Grid.

Easy to start:
```shell
> Install-Package Arcus.EventGrid.Testing -Version 2.1.0
```

Here is what this article will cover:

- [Receiving events in your tests](#receiving-events-in-your-tests)
- [The Azure infrastructure](#azure-infrastructure)
- [Test Example](#example)
- [Troubleshooting tests](#troubleshooting-tests)
    - [Use logging](#use-logging)
    - [Keeping a test subscription on the topic](#keeping-a-test-subscription-on-the-topic)

### Receiving events in your tests
By using the `ServiceBusEventConsumerHost` you can subscribe to Azure Event Grid events on a custom topic and consume them in your tests.

You can easily check if an event is received:
```csharp
_serviceBusEventConsumerHost.GetReceivedEvent(eventId, retryCount: 5)
```

As requests are flowing in asynchronously, we provide the capability to retry the looking for an event which is using an exponential backoff.

In order to use this host, we require you to set up a small infrastructure in Azure that is consuming all events on your custom Azure Event Grid topic.

More information can be found in ["The Azure infrastructure"](#azure-infrastructure).

### Example
Here is an example of how you can set up the `ServiceBusEventConsumerHost` and query for events in your tests:
```csharp
[Trait(name: "Category", value: "Integration")]
public class EventPublishingTests : IAsyncLifetime
{
    private ServiceBusEventConsumerHost _serviceBusEventConsumerHost;

    protected IConfiguration Configuration { get; }

    public async Task DisposeAsync()
    {
        await _serviceBusEventConsumerHost.Stop();
    }

    public async Task InitializeAsync()
    {        
        var serviceBusConnectionString = "<service-bus-connectionstring>";
        var serviceBusTopicName = "<topic-name>";

        var serviceBusEventConsumerHostOptions = new ServiceBusEventConsumerHostOptions(serviceBusTopicName, serviceBusConnectionString);
        _serviceBusEventConsumerHost = await ServiceBusEventConsumerHost.Start(serviceBusEventConsumerHostOptions, _testLogger);
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
        var receivedEvent = _serviceBusEventConsumerHost.GetReceivedEvent(eventId);
        Assert.NotEmpty(receivedEvent);
    }
}
```

### Azure infrastructure

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Farcus-azure%2Farcus.eventgrid%2Fmaster%2Fdeploy%2Farm%2Ftesting-infrastructure%2Fazuredeploy.json" target="_blank">
    <img src="https://azuredeploy.net/deploybutton.png"/>
</a>
<a href="http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2Farcus-azure%2Farcus.eventgrid%2Fmaster%2Fdeploy%2Farm%2Ftesting-infrastructure%2Fazuredeploy.json" target="_blank">
    <img src="https://armviz.io/visualizebutton.png"/>
</a>


When running integration tests with Azure, Arcus needs to be one of the consumers of your custom Azure Event Grid Topic. By doing this, it will send all received events to an [Azure Service Bus Topics](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview#topics) on which every test run will create a subscription to poll for new events.

The `ServiceBusEventConsumerHost` will poll for new messages on the subscription that was created and store them internally. Once the host closes, it will shut itself down and delete the subscription on the topic.

In the future, we will provide an ARM template which allows you to easily deploy an Azure Logic App that will receive all events from Event Grid and store them on the Service Bus Topic, but you can also provide your own flow to achieve this.

Here is a visual representation of how it works:
![Infrastructure](/media/integration-testing-infrastructure.png)


### Troubleshooting tests
#### Use logging
Our `ServiceBusEventConsumerHost` supports writing telemetry that can be used to troubleshoot failing tests.

This is based on `Microsoft.Extensions.Logging.ILogger` where we provide the following loggers out-of-the-box:
- `XunitTestLogger` - Useful when running tests with Xunit which is using `ITestOutputHelper` 
- `ConsoleLogger` - Used to write logging to the output console
- `NoOpLogger` - Used when no logging is required

#### Keeping a test subscription on the topic
Every test run automatically creates a new subscription on the topic. By default, this subscription is automatically deleted when the test has finished.

You can turn this off by configuring this on the `ServiceBusEventConsumerHostOptions` as following:
```csharp
var serviceBusEventConsumerHostOptions = new ServiceBusEventConsumerHostOptions(serviceBusTopicName, serviceBusConnectionString)
{
    SubscriptionBehavior = SubscriptionBehavior.KeepOnClosure // Default: SubscriptionBehavior.DeleteOnClosure
};
```

[&larr; back](/arcus.eventgrid)