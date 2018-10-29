# Arcus - Azure Event Grid
[![Build status](https://dev.azure.com/codit/Arcus/_apis/build/status/Commit%20builds/CI%20-%20Arcus.EventGrid)](https://dev.azure.com/codit/Arcus/_build/latest?definitionId=369)[![NuGet Badge](https://buildstats.info/nuget/Arcus.EventGrid.All?includePreReleases=true)](https://www.nuget.org/packages/Arcus.EventGrid.All/)

Azure Event Grid development in a breeze.

![Arcus](https://raw.githubusercontent.com/arcus-azure/arcus/master/media/arcus.png)

# Installation

```shell
PM > Install-Package Arcus.EventGrid.All
```

# Documentation
All documentation can be found on [arcus-azure.github.io/arcus.eventgrid](https://arcus-azure.github.io/arcus.eventgrid/).

# Testing
Currently we provide both unit tests and integration tests.
Every new feature should be covered by both.

Our integration tests are using Azure Relay Hybrid Connections to relay events back to our tests by using our `HybridConnectionHost`.
For more information, [read our wiki](https://github.com/arcus-azure/arcus.eventgrid/wiki/Running-integration-tests-with-Arcus) or read [this blog post](https://www.codit.eu/blog/writing-tests-for-azure-event-grid/).

## How do I run the integration tests?
In order to run the integration tests, you will need to do the following:
1. Deploy the Azure Resource Manager template - <a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Farcus-azure%2Farcus.eventgrid%2Fmaster%2Fdeploy%2Farm%2Fdeploy-testing-infrastructure.json" target="_blank">
        <img src="http://azuredeploy.net/deploybutton.png"/>
    </a>

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

# License Information
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.
