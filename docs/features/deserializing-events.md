---
title: "Deserializing Events"
layout: default
---

## Deserializing Events
We provide support for deserializing events to typed objects:

```csharp
// From custom event.
var eventGridBatch = EventGridParser.Parse<NewCarRegistered>(rawEvent);

// From event data (can be Azure SDK types).
var eventGridBatch = EventGridParser.ParseFromData<StorageBlobCreatedEventData>(rawEvent);
```

[&larr; back](/arcus.eventgrid)
