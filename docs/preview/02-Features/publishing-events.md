---
title: "Publishing Events"
layout: default
---

# Azure Event Grid publishing
We provide support for publishing Azure EventGrid, CloudEvents and custom events to a custom Azure Event Grid topics. 
This event publishing via a dedicated `IEventGridPublisher` implementation that can either be added to the dependency container of your application, or can be created directly via our `EventGridPublisherBuilder`.

- [Azure Event Grid publishing](#azure-event-grid-publishing)
  - [Usage](#usage)
  - [Publishing events](#publishing-events)
    - [Publishing EventGridEvents](#publishing-eventgridevents)
    - [Publishing CloudEvents](#publishing-cloudevents)
  - [Resilient Publishing](#resilient-publishing)
    - [Exponential retry](#exponential-retry)
    - [Circuit breaker](#circuit-breaker)
    - [Combination](#combination)

## Usage
Adding simple Azure EventGrid publishing to your application only required the following registration.
> ⚠ Note that this way of registering requires the [Arcus secrect store](https://security.arcus-azure.net/features/secret-store) to retrieve the necessary authentication secrets to interact with the Azure EventGrid topic.

```csharp
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Requires Arcus secret store, for more information, see: https://security.arcus-azure.net/features/secret-store
    services.AddSecretStore(stores => ...);

    // Registers an `IEventGridPublisher` to your application to a custom topic.
    services.AddEventGridPublishing(
        "https://my-eventgrid-topic-endpoint", 
        "<my-authentication-secret-name>");
}
```

To create an `IEventGridPublisher` instance directly via the `EventGridPublisherBuilder`, the authentication key of your custom topic endpoint is required. Since the authentication key is passed directly, this approach doesn't require the Arcus secret store.  Nevertheless, it is advised to safely store your authentication key in a secret store.

```csharp
using Arcus.EventGrid.Publishing;

IEventGridPublisher publisher = 
    EventGridPublisherBuilder.ForTopic("https://my-eventgrid-topic-endpoint")
                             .UsingAuthenticationKey("<my-authentication-secret>")
                             .Build();
```

## Publishing events

### Publishing EventGridEvents
The library provides support to publish custom Azure EventGrid events based on your custom definition of an event, to your custom Azure EventGrid topic endpoint. In this case, `NewCarRegistered` is the created, which inherits `Event`:

```csharp
public class NewCarRegistered : EventGridEvent<CarEventData>
{
     private const string DefaultDataVersion = "1", 
                          DefaultEventType = "Arcus.Samples.Cars.NewCarRegistered";

    // Usually required for serialization.
    private NewCarRegistered()
    {
    }

    public NewCarRegistered(string id, string subject, string licensePlate) 
        : base(id, subject: "New registered car", new CarEventData(licensePlate), DefaultDataVersion, DefaultEventType) 
    {
    }
}
```

Once the event definition is ready, you can publish it with the previously created `IEventGridPublisher` implementation:

```csharp
using Arcus.EventGrid.Publishing;

IEventGridPublisher publisher = ...

string licensePlate = "1-TOM-337";
string eventSubject = $"/cars/{licensePlate}";
string eventId = Guid.NewGuid().ToString();
var @event = new NewCarRegistered(eventId, eventSubject, licensePlate);

// Publish a single event.
await publisher.PublishAsync(@event);

// Alternatively you can publish a list of events by using.
await publisher.PublishManyAsync(events);
```

We also provide the capability to push EventGrid events without a schema based on a raw JSON string.

```csharp
using Arcus.EventGrid.Publishing;

EventGridPublisher publisher = ...

string licensePlate = "1-TOM-337";
string eventSubject = $"/cars/{licensePlate}";
string eventId = Guid.NewGuid().ToString();
string rawEventPayload = String.Format("{ \"licensePlate\": \"{0}\"}", licensePlate);

await publisher.PublishRawEventGridEventAsync(eventId, eventSubject, rawEventPayload);
```

### Publishing CloudEvents
The library supports to publish [CloudEvents](https://github.com/cloudevents/spec) to your custom Azure EventGrid topic endpoint.

```csharp
using Arcus.EventGrid.Publishing;

IEventGridPublisher publisher = ...

string licensePlate = "1-TOM-337";
string eventSubject = $"/cars/{licensePlate}";
string eventId = Guid.NewGuid().ToString();
var @event = new CloudEvent(
    CloudEventsSpecVersion.V1_0, 
    "NewCarRegistered", 
    new Uri("https://eventgrid.arcus-azure.net/"), 
    eventSubject, 
    eventId)
{
    Data = new CarEventData(licensePlate),
    DataContentType = new ContentType("application/json")
};

// Publish a single event.
await publisher.PublishAsync(@event);

// Alternatively you can publish a list of events using the `PublishMany` method.
await publisher.PublishManyAsync(events);
```

We also provide the capability to push [CloudEvents](https://github.com/cloudevents/spec) without a schema based on a raw JSON string.

```csharp
using Arcus.EventGrid.Publishing;

IEventGridPublisher publisher = ...

string licensePlate = "1-TOM-337";
string eventSubject = $"/cars/{licensePlate}";
string eventId = Guid.NewGuid().ToString();
string rawEventPayload = String.Format("{ \"licensePlate\": \"{0}\"}", licensePlate);

await publisher.PublishRawCloudEventAsync(eventId, eventSubject, rawEventPayload);
```

## Resilient Publishing
The library also provides several ways to publish events in a resilient manner. Resilient meaning we support three ways to add resilience to your event publishing:

### Exponential retry
This makes the publishing resilient by retrying a specified number of times with exponential back off.

Adding exponential retry to your `IEventGridPublisher` implementation can easily be done via the guided extensions:

```csharp
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Requires Arcus secret store, for more information, see: https://security.arcus-azure.net/features/secret-store
    services.AddSecretStore(stores => ...);

    // Registers an `IEventGridPublisher` to your application to a custom topic.
    services.AddEventGridPublishing(
        "https://my-eventgrid-topic-endpoint", 
        "<my-authentication-secret-name>")
            .WithExponentialRetry<HttpRequestException>(retryCount: 3);
}
```

Adding exponential back-off resilience is also available when building the publisher yourself:

```csharp
using Arcus.EventGrid.Publishing;

IEventGridPublisher publisher =
    EventGridPublisherBuilder.ForTopic(topicEndpoint)
                             .UsingAuthenticationKey(endpointKey)
                             .WithExponentialRetry<HttpRequestException>(retryCount: 3)
                             .Build();
```

### Circuit breaker
This makes the publishing resilient by breaking the circuit if the maximum specified number of exceptions are handled by the policy. The circuit will stay broken for a specified duration. Any attempt to execute the function while the circuit is broken will result in a `BrokenCircuitException`.

Adding circuit breaker retry to your `IEventGridPublisher` implementation can easily be done via the guided extensions:

```csharp
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Requires Arcus secret store, for more information, see: https://security.arcus-azure.net/features/secret-store
    services.AddSecretStore(stores => ...);

    // Registers an `IEventGridPublisher` to your application to a custom topic.
    services.AddEventGridPublishing("https://my-eventgrid-topic-endpoint", "<my-authentication-secret-name>")
            .WithCircuitBreaker<HttpRequestException>(
                exceptionsBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(5));
}
```

Adding circuit breaker resilience is also available when building the publisher yourself:

```csharp
using Arcus.EventGrid.Publishing;

IEventGridPublisher publisher =
    EventGridPublisherBuilder.ForTopic(topicEndpoint)
                             .UsingAuthenticationKey(endpointKey)
                             .WithCircuitBreaker<HttpRequestException>(
                                 exceptionsBeforeBreaking: 3, 
                                 durationOfBreak: TimeSpan.FromSeconds(5))
                             .Build();
```

### Combination
Both exponential back-off and circuit breaker resilience cam be combined together. Use the available extensions to guide you in this process.

```csharp
using Microsoft.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Requires Arcus secret store, for more information, see: https://security.arcus-azure.net/features/secret-store
    services.AddSecretStore(stores => ...);

    // Registers an `IEventGridPublisher` to your application to a custom topic.
    services.AddEventGridPublishing("https://my-eventgrid-topic-endpoint", "<my-authentication-secret-name>")
            .WithExponentialRetry<HttpRequestException>(retryCount: 3)
            .WithCircuitBreaker<HttpRequestException>(
                exceptionsBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(5));
}
```

Adding the combination is also available when building the publisher yourself:

```csharp
using Arcus.EventGrid.Publishing;

IEventGridPublisher publisher =
    EventGridPublisherBuilder.ForTopic(topicEndpoint)
                             .UsingAuthenticationKey(endpointKey)
                             .WithExponentialRetry<HttpRequestException>(retryCount)
                             .WithCircuitBreaker<HttpRequestException>(
                                 exceptionsBeforeBreaking: 3, 
                                 durationOfBreak: TimeSpan.FromSeconds(5))
                             .Build();
```