
![Logo of the project](media/logo/arcus-logo.png)

# Arcus - Azure Event Grid
> Azure Event Grid development in a breeze

This library provides Azure Event Grid functionality that can be used to build reliable cloud projects.

## Installing / Getting started

Just leverage the Nuget package

```shell
> Install-Package Arcus.EventGrid.All -Version 0.0.1-preview  
```

This downloads the lates nuget package and add the references in your project

## Features

What can you perform with the library?
* Handling Azure events
* Publishing custom Event Grid events
* Securing Event Grid custom webhook API's

## Testing
Currently we provide both unit tests and integration tests.
Every new feature should be covered by both.

Our integration tests are using Azure Relay Hybrid Connections to relay events back to our tests by using our `HybridConnectionHost`.
For more information, [read our wiki](https://github.com/arcus-azure/arcus.eventgrid/wiki/Running-integration-tests-with-Arcus) or read [this blog post](https://www.codit.eu/blog/writing-tests-for-azure-event-grid/).

### How do I run the integration tests?
In order to run the integration tests, you will need to do the following:
1. Deploy the Azure Resource Manager template located at `/deploy/armdeploy-testing-infrastructure.json`
2. Create a subscription on our custom Azure Event Grid topic for our Hybrid Connection ([walkthrough](https://www.codit.eu/blog/writing-tests-for-azure-event-grid/))
3. Configure the following environment variables:
    - `Arcus__HybridConnections__RelayNamespace` _- Azure Relay namespace, for example `arcus.servicebus.windows.net`_
    - `Arcus__HybridConnections__Name` _- Name of your create Hybrid Connection_
    - `Arcus__HybridConnections__AccessPolicyName` _- Name of the access policy_
    - `Arcus__HybridConnections__AccessPolicyKey` _- Authentication key for the access policy_
    - `Arcus__EventGrid__TopicEndpoint` _- Custom topic endpoint for Azure Event Grid, for example `https://arcus.westeurope-1.eventgrid.azure.net/api/events`_
    - `Arcus__EventGrid__EndpointKey` _- Authentication key for the custom Azure Event Grid topic_

Once you have completed the above, you can run `dotnet test` from the `src\Arcus.EventGrid.Tests.Integration` directory.

---------

:pencil: _**Notes**_

- _If you are using Visual Studio, you must restart Visual Studio in order to use new Environment Variables._
- _`src\Arcus.EventGrid.Tests.Integration\appsettings.json` can also be overriden but it brings the risk of commiting these changes. **This approach is not recommended.** This is also why we don't use `appsettings.{Environment}.json`_

---------

## Links

- Repository: https://github.com/arcus-azure/arcus.eventgrid
- Issue tracker: https://github.com/arcus-azure/arcus.eventgrid/issues
  - In case of sensitive bugs like security vulnerabilities, please contact
    info@codit.eu directly instead of using issue tracker. We value your effort
    to improve the security and privacy of this project!


## Licensing

This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.