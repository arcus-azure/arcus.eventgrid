---
title: "Publishing Events"
layout: default
---

## Publishing Events

![](https://img.shields.io/badge/Available%20starting-v3.0-green?link=https://github.com/arcus-azure/arcus.eventgrid/releases/tag/v3.0.0)

We provide support for publishing custom events to a custom Azure Event Grid Topics.

Import the following namespace into your project:

```csharp
using Arcus.EventGrid.Publishing;
```

Next, create an `EventGridPublisher` instance via the `EventGridPublisherBuilder` which requires the endpoint & authentication key of your custom topic endpoint.

```csharp
var eventGridPublisher = EventGridPublisherBuilder
                                .ForTopic(topicEndpoint)
                                .UsingAuthenticationKey(endpointKey)
                                .Build();
```
**Publishing EventGridEvent's**

Create your event that you want to publish

```csharp
string licensePlate = "1-TOM-337";
string eventSubject = $"/cars/{licensePlate}";
string eventId = Guid.NewGuid().ToString();
var @event = new NewCarRegistered(eventId, eventSubject, licensePlate);

await eventGridPublisher.PublishAsync(@event);
```

Alternatively you can publish a list of events by using

```csharp
await eventGridPublisher.PublishManyAsync(events);
```

**Publishing CloudEvent's**

Create your event that you want to publish

```csharp
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

await eventGridPublisher.PublishAsync(@event);
```

Alternatively you can publish a list of events using

```csharp
await eventGridPublisher.PublishManyAsync(events);
```

### Publishing Raw Events

![](https://img.shields.io/badge/Available%20starting-v3.0-green?link=https://github.com/arcus-azure/arcus.eventgrid/releases/tag/v3.0.0)

**Publishing raw EventGridEvent's**

We provide the capability to push EventGridEvents without a schema based on a raw JSON string.

```csharp
// Created via EventGridPublisherBuilder.
EventGridPublisher eventGridPublisher = ...

string licensePlate = "1-TOM-337";
string eventSubject = $"/cars/{licensePlate}";
string eventId = Guid.NewGuid().ToString();
string rawEventPayload = String.Format("{ \"licensePlate\": \"{0}\"}", licensePlate);

await eventGridPublisher.PublishRawEventGridEventAsync(eventId, eventSubject, rawEventPayload);
```

**Publishing raw CloudEvent's**

We provide the capability to push [CloudEvent's](https://github.com/cloudevents/spec) without a schema based on a raw JSON string.

```csharp
EventGridPublisher eventGridPublisher = ...

string licensePlate = "1-TOM-337";
string eventSubject = $"/cars/{licensePlate}";
string eventId = Guid.NewGuid().ToString();
string rawEventPayload = String.Format("{ \"licensePlate\": \"{0}\"}", licensePlate);

await eventGridPublisher.PublishRawCloudEventAsync(eventId, eventSubject, rawEventPayload);
```


### Resilient Publishing

![](https://img.shields.io/badge/Available%20starting-v2.0-green?link=https://github.com/arcus-azure/arcus.eventgrid/releases/tag/v2.0.0)

The `EventGridPublisherBuilder` also provides several ways to publish events in a resilient manner. Resilient meaning we support three ways to add resilience to your event publishing:

- **Exponential retry**: makes the publishing resilient by retrying a specified number of times with exponential backoff.

```csharp
EventGridPublisherBuilder.ForTopic(topicEndpoint)
                         .UsingAuthenticationKey(endpointKey)
                         .WithExponentialRetry(retryCount)
                         .Build();
```

- **Circuit breaker**: makes the publishing resilient by breaking the circuit if the maximum specified number of exceptions are handled by the policy. The circuit will stay broken for a specified duration. Any attempt to execute the function while the circuit is broken will result in a `BrokenCircuitException`.

```csharp
EventGridPublisherBuilder.ForTopic(topicEndpoint)
                         .UsingAuthenticationKey(endpointKey)
                         .WithCircuitBreaker(exceptionsBeforeBreaking, durationOfBreak)
                         .Build();
```

- Combination of the two: **Circuit breaker with/after exponential retry**.

```csharp
EventGridPublisherBuilder.ForTopic(topicEndpoint)
                         .UsingAuthenticationKey(endpointKey)
                         .WithExponentialRetry(retryCount)
                         .WithCircuitBreaker(exceptionsBeforeBreaking, durationOfBreak)
                         .Build();
```

[&larr; back](/)