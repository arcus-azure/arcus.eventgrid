---
title: "Publishing Events"
layout: default
---

## Publishing Events

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

Create your event that you want to publish

```csharp
string licensePlate = "1-TOM-337";
string eventSubject = $"/cars/{licensePlate}";
string eventId = Guid.NewGuid().ToString();
var @event = new NewCarRegistered(eventId, eventSubject, licensePlate);

await eventGridPublisher.Publish(@event);
```

Alternatively you can publish a list of events by using

```csharp
await eventGridPublisher.PublishMany(events);
```

### Resilient Publishing

The `EventGridPublisherBuilder` also provides several ways to publish events in a resilient manner. Resilient meaning we support three ways to add resilience to your event publishing:

- **Exponential retry**: makes the publishing resilient by retrying a specified number of times with exponential backoff.

```csharp
EventGridPublisherBuilder.ForTopic(topicEndpoint)
                         .UsingAuthenticationKey(endpointKey)
                         .WithExponentialRetry(retryCount)
                         .Build();
```

- **Circuit broker**: makes the publishing resilient by breaking the circuit if the maximum specified number of exceptions are handled by the policy. The circuit will stay broken for a specified duration. Any attempt to execute the function while the circuit is broken will result in a `BrokenCircuitException`.

```csharp
EventGridPublisherBuilder.ForTopic(topicEndpoint)
                         .UsingAuthenticationKey(endpointKey)
                         .WithCircuitBreaker(exceptionsBeforeBreaking, durationOfBreak)
                         .Build();
```

- Combination of the two: **Circuit broker with/after exponential retry**.

```csharp
EventGridPublisherBuilder.ForTopic(topicEndpoint)
                         .UsingAuthenticationKey(endpointKey)
                         .WithExponentialRetry(retryCount)
                         .WithCircuitBreaker(exceptionsBeforeBreaking, durationOfBreak)
                         .Build();
```

[&larr; back](/arcus.eventgrid)
