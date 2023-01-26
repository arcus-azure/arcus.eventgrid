using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.EventGrid.Security.Core.Validation;
using Arcus.EventGrid.Security.WebApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Arcus.EventGrid.Tests.Unit.Security
{
    public class EventGridSubscriptionValidationActionFilterTests
    {
        [Fact]
        public async Task ValidateSubscription_WithDefault_Succeeds()
        {
            // Arrange
            var attribute = new EventGridSubscriptionValidationAttribute();
            var services = new ServiceCollection();
            services.AddLogging();

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            var filter = (IAsyncActionFilter) attribute.CreateInstance(serviceProvider);

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = serviceProvider;

            var actionExecutingContext = new ActionExecutingContext(
                new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary()),
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                controller: null);

            var actionExecutedContext = new ActionExecutedContext(
                new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary()),
                new List<IFilterMetadata>(),
                controller: null);

            // Act / Assert
            await filter.OnActionExecutionAsync(actionExecutingContext, () => Task.FromResult(actionExecutedContext));
        }
    }
}
