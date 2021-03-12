---
title: "Deserializing Events"
layout: default
---

## Deserializing Events

The `Arcus.EventGrid` package provides several ways to deserializing events.

Following paragraphs describe each supported type of event.

- [Deserializing Custom Events](#deserializing-custom-events)

### Deserializing Custom Events

We provide support for deserializing events to typed event objects where the custom event payload is available via the `.GetPayload()` method.

If you want to have the original raw JSON event payload, you can get it via the `.Data` property.

```csharp
using Arcus.EventGrid;
using Arcus.EventGrid.Parsers;

// Parse from your custom event implementation with the `.Parse<>` function.
EventGridMessage<NewCarRegistered> eventGridBatch = EventGridParser.Parse<NewCarRegistered>(rawEvent);

// The event data type will be wrapped inside an `EventGridMessage<>` instance.
NewCarRegistered eventGridMessage = eventGridBatch.Events.First();

// The original event payload can now be accessed.
CarEventData eventPayload = eventGridMessage.Data;
```

[&larr; back](/)
