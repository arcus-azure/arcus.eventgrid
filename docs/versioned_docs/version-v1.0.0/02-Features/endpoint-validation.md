---
title: "Endpoint validation"
layout: default
---

## Endpoint validation

We provide support for endpoint validation, when implementing your own custom web hook.  This validation allows to secure your web hook with a secret key (taken from the query string or an HTTP header).  This is required, because Azure Event Grid sends a validation request to a newly configured web hook, in order to prevent people leveraging Azure Event Grid to bring down a 3rd party API.  The implementation we provide, is echoing back the validation key on your operation, in order to have the validation by Event Grid out of the box.

Import the following namespace into your project:
```csharp
using Arcus.EventGrid.Security;
using Arcus.EventGrid.Security.Attributes;
```

Next, tag the Operation that will be handling the Event Grid events with the `EventGridSubscriptionValidator` and the  `EventGridAuthorization` attributes, that will secure the endpoint and handle the handshake.
```csharp
[EventGridSubscriptionValidator]
[EventGridAuthorization("x-api-key", "event-grid")]
public IHttpActionResult HandleEvents(HttpRequestMessage message)
{
    return Ok();
}
```

### Configuring Authorization with attributes
There are 2 attributes available to secure operations:    
- The `EventGridAuthorization` has a hard-coded secret key and is only advised for demonstration or testing purposes.  
- The `DynamicEventGridAuthorizationAttribute` allows developer to specify a static `Func` with name `RetrieveAuthenticationSecret` to implement custom logic to retrieve the actual secret key (for example from Azure KeyVault, web.config or appsettings.json)

These attributes will only allow an operation to be called, if the configured secret key value is found in the HTTP header or the Query String with the configured key name.

#### The EventGridAuthorization attribute
This attribute has the following public constructor.  This constructor sets the header or query string name and the hard coded secret value.

```csharp
[EventGridAuthorization("keyName", "keyValue")]
```

#### The DynamicEventGridAuthorization attribute
This attribute has the following public constructors.  This constructor sets the header or query string name to the provided value (or defaults to 'x-api-key' for the default constructor).

```csharp
[DynamicEventGridAuthorizationAttribute()]
[DynamicEventGridAuthorizationAttribute("custom-key-name")]
```
Important for this authorization method to work, is to set the static property `RetrieveAuthenticationSecret` to a `Func<Task<string>>`.  This can be seen in the following example.

```csharp
DynamicEventGridAuthorizationAttribute.RetrieveAuthenticationSecret = () => Task.FromResult("my-secret-key");
```

[&larr; back](/)