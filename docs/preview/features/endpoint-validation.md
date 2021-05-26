---
title: "Endpoint validation"
layout: default
---

# Endpoint validation

We provide support for endpoint validation, when implementing your own custom web hook. This validation allows to secure your web hook with a secret key (taken from the query string or an HTTP header).  
This is needed, because Azure Event Grid is send a validation request to a newly configured web hook, in order to prevent people leveraging Azure Event Grid to bring down a 3rd party API. 

## Installation

The features described here require the following package:

```shell
PM> Install-Package Arcus.EventGrid.WebApi.Security 
```

## Usage

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
        // Default set of authorization options.
        var options = new EventGridAuthorizationOptions();

        // Looks for the 'x-api-key' header in the HTTP request and tries to match it with the secret retrieved in the secret store with the name 'MySecret'.
        services.AddMvc(options => options.Filters.Add(new EventGridAuthorizationFilter(HttpRequestProperty.Header, "x-api-key", "MySecret", options)));
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
        var options = new EventGridAuthorizationOptions();

        // Indicates that the Azure Event Grid authorization should emit security events during the authorization of the request (default: `false`).
        options.EmitSecurityEvents = true;

        // Looks for the 'x-api-key' header in the HTTP request and tries to match it with the secret retrieved in the secret store with the name 'MySecret'.
        services.AddMvc(options => options.Filters.Add(new EventGridAuthorizationFilter(HttpRequestProperty.Header, "x-api-key", "MySecret", options)));
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

[&larr; back](/)