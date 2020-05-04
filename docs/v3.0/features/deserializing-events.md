---
title: "Deserializing Events"
layout: default
---

## Deserializing Events

![](https://img.shields.io/badge/Available%20starting-v3.0-green?link=https://github.com/arcus-azure/arcus.eventgrid/releases/tag/v3.0.0)

The `Arcus.EventGrid` package provides several ways to deserializing events.

Following paragraphs describe each supported type of event.

- [Deserializing Built-In Azure Events](#deserializing-built-in-azure-events)
- [Deserializing CloudEvent Events](#deserializing-cloudevent-events)
- [Deserializing Event Grid Events](#deserializing-event-grid-events)
- [Deserializing Custom Events](#deserializing-custom-events)

### Deserializing Built-In Azure Events

When using official Azure events, you can use `.ParseFromData<>` to deserialize them based on the built-in types as shown in the example:

```csharp
// Parse directly from an event data type with the `.ParseFromData<>` function.
EventGridBatch<EventGridEvent<StorageBlobCreatedEventData>> eventGridBatch = EventGridParser.ParseFromData<StorageBlobCreatedEventData>(rawEvent);
```

You can find a list of supported built-in Azure events [in the official documentation](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.eventgrid.models?view=azure-dotnet).

### Deserializing CloudEvent Events

We provide support for deserializing CloudEvents using the [official SDK for C#](https://github.com/cloudevents/sdk-csharp).

Upon receiving of CloudEvent events:

```csharp
string cloudEventJson = ...

EventBatch<Event> eventBatch = EventGridParser.Parse(cloudEventJson);

var events = eventBatch.Events;

// The type `CloudEvent` comes directly from the official SDK package
// And can be cast implicitly like so, 
CloudEvent cloudEvent = events.First();

// Or explictly.
CloudEvent cloudEvent = events.First().AsCloudEvent();
```

### Deserializing Event Grid Events

We provide support for deserializing [EventGrid events](https://docs.microsoft.com/en-us/azure/event-grid/event-schema).

```csharp
string eventGridEventJson = ...

EventBatch<Event> eventBatch = EventGridParser.Parse(eventGridEventJson);

var events = eventBatch.Events;

// The `EventGridEvent` can be cast implicitly like so, 
EventGridEvent eventGridEvent = events.First();

// Or explicitly.
EventGridEvent eventGridEvent = events.First().AsEventGridEvent();
```

### Deserializing Custom Events

We provide support for deserializing events to typed event objects where the custom event payload is available via the `.GetPayload()` method.

If you want to have the original raw JSON event payload, you can get it via the `.Data` property.

```csharp
// Parse from your custom event implementation with the `.Parse<>` function.
EventGridBatch<NewCarRegistered> eventGridBatch = EventGridParser.Parse<NewCarRegistered>(rawEvent);

// The event data type will be wrapped inside an `EventGridEvent<>` instance.
NewCarRegistered eventGridMessage = eventGridBatch.Events.First();

// The original event payload can now be accessed.
CarEventData typedEventPayload = eventGridMessage.GetPayload();
object untypedEventPaylaod = eventGridMessage.Data;
```

[&larr; back](/)