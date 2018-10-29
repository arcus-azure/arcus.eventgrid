---
title: "Deserializing Events"
layout: default
permalink: /features/publishing-events
---

## Deserializing Events
We provide support for deserializing events to typed objects:

```csharp
var eventGridMessage = EventGridParser.Parse<BlobCreated>(rawEvent);
```

[&larr; back](/arcus.eventgrid)