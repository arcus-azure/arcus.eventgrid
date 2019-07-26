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

await eventGridPublisher.PublishAsync(@event);
```

Alternatively you can publish a list of events by using

```csharp
await eventGridPublisher.PublishManyAsync(events);
```

### Publishing Raw Events

We provide the capability to push events without a schema based on a raw JSON string.

```csharp
// Created via EventGridPublisherBuilder.
EventGridPublisher eventGridPublisher = ...

string licensePlate = "1-TOM-337";
string eventSubject = $"/cars/{licensePlate}";
string eventId = Guid.NewGuid().ToString();
string rawEventPayload = String.Format("{ \"licensePlate\": \"{0}\"}", licensePlate);

await eventGridPublisher.PublishRawAsync(eventId, eventSubject, rawEventPayload);
```

or with a specific `RawEvent` type

```csharp
string licensePlate = "1-TOM-337";
string eventSubject = $"/cars/{licensePlate}";
string eventId = Guid.NewGuid().ToString();
string rawEventPayload = String.Format("{ \"licensePlate\": \"{0}\"}", licensePlate);
string eventType = "Arcus.Samples.Cars.NewCarRegitered";
string dataVersion = "1.0";
DateTimeOffset eventTime = DateTimeOffset.Now;

var rawEvent = new RawEvent(eventId, eventSubject, rawEventPayload, eventType, dataVersion, eventTime);

await eventGridPublisher.PublishRawAsync(rawEvent);
```

Alternatively you can publish a list of raw events by using

```csharp
IEnumerable<RawEvent> rawEvents = ...;

await eventGridPublisher.PublishManyRawAsync(rawEvents);
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
