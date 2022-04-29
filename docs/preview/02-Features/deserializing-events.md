---
title: "Deserializing Events"
layout: default
---

## Deserializing Events

The `Arcus.EventGrid` package provides several ways to deserializing events.

Following paragraphs describe each supported way to deserialize an event.

- [Deserializing Events](#deserializing-events)
  - [Deserializing Built-In Azure Events](#deserializing-built-in-azure-events)
  - [Deserializing CloudEvent Events](#deserializing-cloudevent-events)
  - [Deserializing Event Grid Events](#deserializing-event-grid-events)
  - [Deserializing Custom Events](#deserializing-custom-events)

### Deserializing Built-In Azure Events

When using official Azure events, you can use `.ParseFromData<>` to deserialize them based on the built-in types as shown in the example:

```csharp
using Arcus.EventGrid;
using Arcus.EventGrid.Parsers;
using Microsoft.Azure.EventGrid.Models;

// Parse directly from an event data type with the `.Parse` function.
byte[] rawEvent = ...
EventBatch<Event> eventBatch = EventParser.Parse(rawEvent);

// The type `EventGridEvent` comes directly from the official SDK package
// and can be cast implicitly like so,
EventGridEvent eventGridEvent = eventBatch.Events.Single();

// Or explicitly.
EventGridEvent eventGridEvent = eventBatch.Events.Single().AsEventGridEvent();

// The actual EventGrid payload can be retrieved by passing along the Azure SDK model type.
var storageEventData = eventGridEvent.GetPayload<StorageBlobCreatedEventData>();
```

You can find a list of supported built-in Azure events [in the official documentation](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.eventgrid.models?view=azure-dotnet).

### Deserializing CloudEvent Events

We provide support for deserializing CloudEvents using the [official SDK for C#](https://github.com/cloudevents/sdk-csharp).

Upon receiving of CloudEvent events:

```csharp
using Arcus.EventGrid;
using Arcus.EventGrid.Parsers;
using CloudNative.CloudEvents;

string cloudEventJson = ...
EventBatch<Event> eventBatch = EventParser.Parse(cloudEventJson);
var events = eventBatch.Events;

// The type `CloudEvent` comes directly from the official SDK package
// and can be cast implicitly like so, 
CloudEvent cloudEvent = events.First();

// Or explicitly.
CloudEvent cloudEvent = events.First().AsCloudEvent();
```

### Deserializing Event Grid Events

We provide support for deserializing [EventGrid events](https://docs.microsoft.com/en-us/azure/event-grid/event-schema).

```csharp
using Arcus.EventGrid;
using Arcus.EventGrid.Parsers;
using Microsoft.Azure.EventGrid.Models;

string eventGridEventJson = ...
EventBatch<Event> eventBatch = EventParser.Parse(eventGridEventJson);
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
using Arcus.EventGrid;
using Arcus.EventGrid.Parsers;

// Parse from your custom event implementation with the `.Parse<>` function.
EventGridBatch<NewCarRegistered> eventGridBatch = EventGridParser.Parse<NewCarRegistered>(rawEvent);

// The event data type will be wrapped inside an `EventGridEvent<>` instance.
NewCarRegistered eventGridMessage = eventGridBatch.Events.First();

// The original event payload can now be accessed.
CarEventData typedEventPayload = eventGridMessage.GetPayload();
object untypedEventPayload = eventGridMessage.Data;
```
