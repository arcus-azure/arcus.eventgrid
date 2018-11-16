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

[&larr; back](/arcus.eventgrid)
