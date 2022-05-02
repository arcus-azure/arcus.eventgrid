# Arcus - Azure Event Grid
[![Build Status](https://dev.azure.com/codit/Arcus/_apis/build/status/Commit%20builds/CI%20-%20Arcus.EventGrid?branchName=master)](https://dev.azure.com/codit/Arcus/_build/latest?definitionId=734&branchName=master)
[![NuGet Badge](https://buildstats.info/nuget/Arcus.EventGrid?includePreReleases=true)](https://www.nuget.org/packages/Arcus.EventGrid/)

Azure Event Grid development in a breeze.

![Arcus](https://raw.githubusercontent.com/arcus-azure/arcus/master/media/arcus.png)

# Installation

Easy to install it via NuGet:

- **Publishing**

```shell
PM > Install-Package Arcus.EventGrid.Publishing
```

- **Models**

```shell
PM > Install-Package Arcus.EventGrid
```

For a more thorough overview, we recommend reading our [documentation](#documentation).

# Documentation
All documentation can be found on [here](https://eventgrid.arcus-azure.net/).

# Customers
Are you an Arcus user? Let us know and [get listed](https://bit.ly/become-a-listed-arcus-user)!

# How do I run the integration tests?
In order to run the integration tests, you will need to do the following:
1. Setup the infrastructure ([docs](https://eventgrid.arcus-azure.net/features/running-integration-tests#azure-infrastructure))

2. Configure the following environment variables:
    - `Arcus__EventGrid__TopicEndpoint` _- Custom topic endpoint for Azure Event Grid, for example `https://arcus.westeurope-1.eventgrid.azure.net/api/events`_
    - `Arcus__EventGrid__EndpointKey` _- Authentication key for the custom Azure Event Grid topic_
    - `Arcus__ServiceBus__ConnectionString` _- Connection string to use when connecting to Azure Service Bus`_
    - `Arcus__ServiceBus__TopicName` _- Name of the Service Bus Topic that you want to use_

Once you have completed the above, you can run `dotnet test` from the `src\Arcus.EventGrid.Tests.Integration` directory.

---------

:pencil: _**Notes**_

- _If you are using Visual Studio, you must restart Visual Studio in order to use new Environment Variables._
- _`src\Arcus.EventGrid.Tests.Integration\appsettings.json` can also be overriden but it brings the risk of commiting these changes. **This approach is not recommended.** This is also why we don't use `appsettings.{Environment}.json`_

---------

# License Information
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.
