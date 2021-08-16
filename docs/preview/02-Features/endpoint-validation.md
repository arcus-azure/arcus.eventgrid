---
title: "Endpoint validation"
layout: default
---

# Endpoint validation

We provide support for endpoint validation, when implementing your own custom web hook.

* [Azure Event Grid authorization](#azure-event-grid-authorization)
* [Azure Event Grid subscription validation](#azure-event-grid-subscription-validation)

## Installation

The features described here require the following package:

```shell
PM> Install-Package Arcus.EventGrid.WebApi.Security 
```

## Azure Event Grid authorization

Azure Event Grid authorization allows to secure your webhook with a secret key (taken from the query string or an HTTP header). 
This is needed, because Azure Event Grid is send a validation request to a newly configured web hook, in order to prevent people leveraging Azure Event Grid to bring down a 3rd party API. 

The implementation we provide, is echoing back the validation key on your operation, in order to have the validation by Event Grid out of the box.

### Enforce authorization globally

We created the `EventGridAuthorizationFilter` MVC filter that will secure the endpoint and handle the handshake.

```csharp
using Arcus.EventGrid.WebApi.Security;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureService(IServiceCollection services)
    {
        // Looks for the 'x-api-key' header in the HTTP request and tries to match it with the secret retrieved in the secret store with the name 'MySecret'.
        services.AddMvc(options => options.Filters.AddEventGridAuthorization(HttpRequestProperty.Header, "x-api-key", "MySecret")));
    }
}
```

For this setup to work, an Arcus secret store is required as the provided secret name (in this case `"MySecret"`) will be looked up.
See [our offical documentation](https://security.arcus-azure.net/features/secret-store/) for more information about setting this up.

#### Configuration

The `EventGridAuthorizationFilter` has some additional consumer-configurable options to influence the behavior of the authorization.

```csharp
using Arcus.EventGrid.WebApi.Security;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureService(IServiceCollection services)
    {
        // Looks for the 'x-api-key' header in the HTTP request and tries to match it with the secret retrieved in the secret store with the name 'MySecret'.
        services.AddMvc(options => options.Filters.AddEventGridAuthorization(HttpRequestProperty.Header, "x-api-key", "MySecret", options =>
        {
            // Indicates that the Azure Event Grid authorization should emit security events during the authorization of the request (default: `false`).
            options.EmitSecurityEvents = true;
        })));
    }
}
```

### Enforce authorization per controller or operation

We created the `EventGridAuthorizationAttribute` attribute that will secure the endpoint and handle the handshake.
The attribute can be placed on both the controller as the operation.

```csharp
using Arcus.EventGrid.WebApi.Security;
using Microsoft.AspNetCore.Mvc;

[Route("events")]
[ApiController]
public class EventController : ControllerBase
{
    // Looks for the 'x-api-key' header in the HTTP request and tries to match it with the secret retrieved in the secret store with the name 'MySecret'.
    [EventGridAuthorization(HttpRequestProperty.Header, "x-api-key", "MySecret")]
    public IHttpActionResult Get()
    {
        return Ok();
    }
}
```

For this setup to work, an Arcus secret store is required as the provided secret name (in this case `"MySecret"`) will be looked up.
See [our offical documentation](https://security.arcus-azure.net/features/secret-store/) for more information about setting this up.

#### Configuration

The `EventGridAuthorizationAttribute` attribute has some additional consumer-configurable options to influence the behavior of the authorization.

```csharp
// Indicates that the Azure Event Grid authorization should emit security events during the authorization of the request (default: `false`).
[EventGridAuthorization(..., EmitSecurityEvents = true)]
```

## Azure Event Grid subscription validation

This library provides an Azure Event Grid subscription validation. It can receive Azure Event Grid events from an Event subscription and validates the contents.
This is described in full at [the offical Microsoft docs](https://docs.microsoft.com/en-us/azure/event-grid/receive-events).

### Enforce subscription validation per controller or operation

We created the `EventGridSubscriptionValidationAttribute` attribute that will validate all the incoming requests.
The attribute can be placed on both the controller as the operation.

```csharp
using Arcus.EventGrid.WebApi.Security;
using Microsoft.AspNetCore.Mvc;

[Route("events")]
[ApiController]
public class EventController : ControllerBase
{
    // Looks for the `Aeg-Event-Type` header in the HTTP request, if it contains the `SubscriptionValidation` value the request body will be deserialized and validated.
    // The action attribute will short-circuit the incoming request and return the validation result as an `SubscriptionValidationResponse` (see: https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.eventgrid.models.subscriptionvalidationresponse?view=azure-dotnet).
    [EventGridSubscriptionValidation]
    public IActionResult Get()
    {
        return Ok();
    }
}
```

[&larr; back](/)
