---
title: "Deserializing Events"
layout: default
---

## Deserializing Events

> ❌ The `Arcus.EventGrid` package is deprecated and will be removed in v4. Use the `EventGridEvent.Parse` and `CloudEvent.Parse` from the `Azure.Messaging.EventGrid` to parse events.

Starting from v3.3, we use fully the Azure SDK to deserialize events from and to Azure Event Grid. See [their documentation](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.eventgrid-readme?source=recommendations&view=azure-dotnet) to learn more about parsing custom events.