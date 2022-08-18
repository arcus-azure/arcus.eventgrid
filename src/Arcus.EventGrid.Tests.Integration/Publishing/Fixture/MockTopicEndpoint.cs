using System;
using System.Threading.Tasks;
using Bogus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arcus.EventGrid.Tests.Integration.Publishing.Fixture
{
    public class EndpointCallCount
    {
        public int Count { get; set; }
    }

    public class MockTopicEndpoint : IAsyncDisposable
    {
        private readonly IHost _host;

        private static readonly Faker BogusGenerator = new Faker();

        private MockTopicEndpoint(IHost host, string hostingUrl)
        {
            _host = host;
            HostingUrl = hostingUrl;
        }

        public string HostingUrl { get; }

        public int EndpointCallCount => _host.Services.GetRequiredService<EndpointCallCount>().Count;

        public static async Task<MockTopicEndpoint> StartAsync()
        {
            string url = $"http://localhost:{BogusGenerator.Random.Int(5000, 5500)}/";

            var builder = WebApplication.CreateBuilder();
            builder.Services.AddSingleton<EndpointCallCount>();
            builder.WebHost.UseKestrel()
                   .UseUrls(url);

            WebApplication app = builder.Build();
            app.Use(async (HttpContext ctx, Func<Task> next) =>
            {
                ctx.RequestServices.GetRequiredService<EndpointCallCount>().Count++;

                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await ctx.Response.WriteAsync("Sabotage this endpoint!");
            });

            await app.StartAsync();
            return new MockTopicEndpoint(app, url);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
