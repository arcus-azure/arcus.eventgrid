---
title: "Create custom events"
layout: default
---

## Create Custom Events
Besides the [offcial Azure SDK event schemas](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.eventgrid.models?view=azure-dotnet), we support custom events also.

The official SDK provides a parent class called `EventGridEvent` which provides the data as an ordinary .NET `object`.
We provide another parent (which is the child of the official `EventGridEvent`) that provides your data as an typed instance instead.

This makes sure that your new custom events are both type-safe and are supported by any offical functions that uses the official `EventGridEvent`.

```csharp
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

The typed data is now available as method called `.GetPayload()`:

```csharp
EventGridBatch<NewCarRegistered> eventGridBatch = EventGridParser.Parse<NewCarRegistered>(rawEvent);

IEnumerable<NewCarRegistered> events = eventGridBatch.Events;
NewRegisteredEvent firstEvent = events.First();

CarEventData eventPayload = firstEvent.GetPayload();
```

[&larr; back](/arcus.eventgrid)