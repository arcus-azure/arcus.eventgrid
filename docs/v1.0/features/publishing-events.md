---
title: "Publishing Events"
layout: default
---

## Publishing Events

![](https://img.shields.io/badge/Available%20starting-v1.0-green)

We provide support for publishing custom events to a custom Azure Event Grid Topics.

Import the following namespace into your project:

```csharp
using Arcus.EventGrid.Publishing;
```

Next, create an `EventGridPublisher` instance via the `.Create()` method which requires the endpoint & authentication key of your custom topic endpoint.

```csharp
var eventGridPublisher = EventGridPublisher.Create(topicEndpoint, endpointKey);
```
**Publishing EventGridEvent's**

Create your event that you want to publish

```csharp
string licensePlate = "1-TOM-337";
string eventSubject = $"/cars/{licensePlate}";
string eventId = Guid.NewGuid().ToString();
var @event = new NewCarRegistered(eventId, eventSubject, licensePlate);

await eventGridPublisher.Publish(eventSubject, eventType: "NewCarRegistered", data: new [] { @event }, id: eventId);
```

[&larr; back](/)
