---
title: "Deserializing Events"
layout: default
---

## Deserializing Events

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

Or from event data type objects where the event data payload is available via the `.GetPayload()` method.
The `.ParseFromData<>` function is especially useful when using directly the Azure services built-in types as shown in the example:

```csharp
// Parse directly from an event data type with the `.ParseFromData<>` function.
EventGridBatch<EventGridEvent<StorageBlobCreatedEventData>> eventGridBatch = EventGridParser.ParseFromData<StorageBlobCreatedEventData>(rawEvent);

// The event data type will be wrapped inside an `EventGridEvent<>` instance.
EventGridEvent<StorageBlobCreatedEventData> eventGridMessage = eventGridBatch.Events.First();

// The original event payload can now be accessed.
StorageBlobCreatedEventData typedEventPayload = eventGridMessage.GetPayload();
object untypedEventPaylaod = eventGridMessage.Data;
```

[&larr; back](/arcus.eventgrid)
