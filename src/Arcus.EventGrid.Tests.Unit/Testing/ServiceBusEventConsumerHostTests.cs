using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.EventGrid.Testing.Infrastructure.Hosts.ServiceBus;
using Arcus.Testing.Logging;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.EventGrid.Tests.Unit.Testing
{
    public class ServiceBusEventConsumerHostTests
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusEventConsumerHostTests" /> class.
        /// </summary>
        public ServiceBusEventConsumerHostTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task Test()
        {
            var options = new ServiceBusEventConsumerHostOptions("topic-path", "Endpoint=sb://something-valid.servicebus.windows.net/;SharedAccessKeyName=SomeAccessKeyName;SharedAccessKey=1A00aAaaAaa00aaa00AaaAa0a0AAaAA0a0AAaaaAaAA=");
            await Assert.ThrowsAsync<AggregateException>(() => ServiceBusEventConsumerHost.StartAsync(options, _logger));
        }
    }
}
