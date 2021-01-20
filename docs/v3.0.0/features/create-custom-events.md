---
title: "Create custom events"
layout: default
---

## Create Custom Events

We allow you to create custom events next to the [official Azure SDK event schemas](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.eventgrid.models?view=azure-dotnet).

The official SDK provides a class called `EventGridEvent` which provides your event data as an ordinary .NET `object`.
We provide a variation of the official `EventGridEvent` which provides your data as an typed instance instead.

This makes sure that your new custom events are both type-safe and are supported by any offical functions that uses the official `EventGridEvent`.

```csharp
using Arcus.EventGrid.Contracts;
using Newtonsoft.Json;

public class CarEventData
{
    public CarEventData(string licensePlate)
    {
        LicensePlate = licensePlate;
    }

    public string LicensePlate { get; }
}

public class NewCarRegistered : EventGridEvent<CarEventData>
{
    private const string DefaultDataVersion = "1", 
                         DefaultEventType = "Arcus.Samples.Cars.NewCarRegistered";

        // `JsonConstructor` attribute is not necesary but can help as documentation for the event.
        [JsonConstructor]
        private NewCarRegistered() 
        {
        }

        public NewCarRegistered(string id, string licensePlate) : this(id, "New registered car", licensePlate)
        {
        }

        public NewCarRegistered(string id, string subject, string licensePlate) 
            : base(id, subject, new CarEventData(licensePlate), DefaultDataVersion, DefaultEventType) 
        {
        }
}
```

Read ["Deserializing Events"](features/deserializing-events.md) for information on how custom events are deserialized.


[&larr; back](/arcus.eventgrid)