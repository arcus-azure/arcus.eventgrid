using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Bogus;
using GuardNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arcus.EventGrid.Tests.Integration.Fixture
{
     /// <summary>
    /// Represents a builder model to create <see cref="HttpResponseMessage"/>s in a more dev-friendly manner.
    /// </summary>
    public class HttpRequestBuilder
    {
        private Func<HttpContent> _createContent;
        private readonly string _path;
        private readonly HttpMethod _method;
        private readonly ICollection<KeyValuePair<string, string>> _headers = new Collection<KeyValuePair<string, string>>();
        private readonly ICollection<KeyValuePair<string, string>> _parameters = new Collection<KeyValuePair<string, string>>();
        
        private HttpRequestBuilder(HttpMethod method, string path)
        {
            _method = method;
            _path = path;
        }
        
        /// <summary>
        /// Creates an <see cref="HttpRequestBuilder"/> instance that represents an HTTP GET request to a given <paramref name="route"/>.
        /// </summary>
        /// <remarks>Only the relative route is required, the base endpoint will be prepended upon the creation of the HTTP request.</remarks>
        /// <param name="route">The relative HTTP route.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="route"/> is blank.</exception>
        public static HttpRequestBuilder Get(string route)
        {
            Guard.NotNullOrWhitespace(route, nameof(route), "Requires a non-blank HTTP relative route to create a HTTP GET request builder instance");
            return new HttpRequestBuilder(HttpMethod.Get, route);
        }

        /// <summary>
        /// Creates an <see cref="HttpRequestBuilder"/> instance that represents an HTTP POST request to a given <paramref name="route"/>.
        /// </summary>
        /// <remarks>Only the relative route is required, the base endpoint will be prepended upon the creation of the HTTP request.</remarks>
        /// <param name="route">The relative HTTP route.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="route"/> is blank.</exception>
        public static HttpRequestBuilder Post(string route)
        {
            Guard.NotNullOrWhitespace(route, nameof(route), "Requires a non-blank HTTP relative route to create a HTTP POST request builder instance");
            return new HttpRequestBuilder(HttpMethod.Post, route);
        }
        
        /// <summary>
        /// Adds a header to the HTTP request.
        /// </summary>
        /// <param name="headerName">The name of the header.</param>
        /// <param name="headerValue">The value of the header.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="headerName"/> is blank.</exception>
        public HttpRequestBuilder WithHeader(string headerName, object headerValue)
        {
            Guard.NotNullOrWhitespace(headerName, nameof(headerName), "Requires a non-blank header name to add the header to the HTTP request builder instance");
            _headers.Add(new KeyValuePair<string, string>(headerName, headerValue.ToString()));

            return this;
        }

        /// <summary>
        /// Adds a query parameter to the HTTP request.
        /// </summary>
        /// <param name="parameterName">The name of the query parameter.</param>
        /// <param name="parameterValue">The value of the query parameter.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="parameterName"/> is blank.</exception>
        public HttpRequestBuilder WithParameter(string parameterName, object parameterValue)
        {
            Guard.NotNullOrWhitespace(parameterName, nameof(parameterName), "Requires a non-blank query parameter name to add the parameter to the HTTP request builder instance");
            _parameters.Add(new KeyValuePair<string, string>(parameterName, parameterValue.ToString()));

            return this;
        }

        /// <summary>
        /// Adds a JSON body to the HTTP request.
        /// </summary>
        /// <remarks>This is a non-accumulative method, multiple calls will override the request body, not append to it.</remarks>
        /// <param name="json">The JSON request body.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="json"/> is blank.</exception>
        public HttpRequestBuilder WithJsonBody(string json)
        {
            Guard.NotNullOrWhitespace(json, nameof(json), "Requires non-blank JSON request body to add the content to the HTTP request builder instance");
            _createContent = () => new StringContent($"\"{json}\"", Encoding.UTF8, "application/json");

            return this;
        }

        /// <summary>
        /// Builds the actual <see cref="HttpRequestMessage"/> with the previously provided configurations.
        /// </summary>
        /// <param name="baseRoute">The base route of the HTTP request.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="baseRoute"/> is blank.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="baseRoute"/> is not in the correct HTTP format.</exception>
        internal HttpRequestMessage Build(string baseRoute)
        {
            Guard.NotNullOrWhitespace(baseRoute, nameof(baseRoute), "Requires a non-blank base HTTP endpoint to create a HTTP request message from the HTTP request builder instance");

            string parameters = "";
            if (_parameters.Count > 0)
            {
                parameters = "?" + String.Join("&", _parameters.Select(p => $"{p.Key}={p.Value}")); 
            }

            string path = _path;
            if (path.StartsWith("/"))
            {
                path = path.TrimStart('/');
            }

            var request = new HttpRequestMessage(_method, baseRoute + path + parameters);

            foreach (KeyValuePair<string, string> header in _headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            if (_createContent != null)
            {
                request.Content = _createContent();
            }

            return request;
        }
    }
    
    /// <summary>
    /// <para>Configurable options to change the <see cref="TestApiServer"/> hosting application.</para>
    /// <para>Contains by default the endpoint routing functionality.</para>
    /// </summary>
    public class TestApiServerOptions
    {
        private readonly Faker _bogusGenerator = new Faker();
        private readonly ICollection<Action<IServiceCollection>> _configureServices = new Collection<Action<IServiceCollection>>();
        private readonly ICollection<Action<IApplicationBuilder>> _preconfigures = new Collection<Action<IApplicationBuilder>>();
        private readonly ICollection<Action<IApplicationBuilder>> _configures = new Collection<Action<IApplicationBuilder>>();
        private readonly ICollection<Action<IHostBuilder>> _hostingConfigures = new Collection<Action<IHostBuilder>>();
        private readonly ICollection<Action<IConfigurationBuilder>> _appConfigures = new Collection<Action<IConfigurationBuilder>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TestApiServerOptions" /> class.
        /// </summary>
        public TestApiServerOptions()
        {
            Url = $"http://localhost:{_bogusGenerator.Random.Int(4000, 5999)}/";
        }
        
        /// <summary>
        /// Gets the current HTTP endpoint on which the <see cref="TestApiServer"/> will be hosted.
        /// </summary>
        internal string Url { get; }
        
        /// <summary>
        /// <para>Adds a function to configure the dependency services on the test API server.</para>
        /// <para>This corresponds with the <see cref="IHostBuilder.ConfigureServices"/> call.</para>
        /// </summary>
        /// <param name="configureServices">The function to configure the dependency services.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configureServices"/> is <c>null</c>.</exception>
        public TestApiServerOptions ConfigureServices(Action<IServiceCollection> configureServices)
        {
            Guard.NotNull(configureServices, nameof(configureServices), "Requires a function to configure the dependency services on the test API server");
            _configureServices.Add(configureServices);

            return this;
        }
        
        /// <summary>
        /// <para>
        ///     Adds a function to configure the startup of the application, after
        ///     the default endpoint routing <see cref="EndpointRoutingApplicationBuilderExtensions.UseRouting"/>.
        /// </para>
        /// <para>This corresponds with the <see cref="WebHostBuilderExtensions.Configure(IWebHostBuilder,Action{IApplicationBuilder})"/>.</para>
        /// </summary>
        /// <param name="configure">The function to configure the startup of the application.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configure"/> is <c>null</c>.</exception>
        public TestApiServerOptions Configure(Action<IApplicationBuilder> configure)
        {
            Guard.NotNull(configure, nameof(configure), "Requires a function to configure the application on the test API server");
            _configures.Add(configure);

            return this;
        }
        
        /// <summary>
        /// <para>Adds a function to configure the hosting of the application.</para>
        /// <para>This corresponds with interacting with the <see cref="IHostBuilder"/> directly.</para>
        /// </summary>
        /// <param name="configure">The function to configure the hosting of the application.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configure"/> is <c>null</c>.</exception>
        public TestApiServerOptions ConfigureHost(Action<IHostBuilder> configure)
        {
            Guard.NotNull(configure, nameof(configure), "Requires a function to configure the hosting configuration of the test API server");
            _hostingConfigures.Add(configure);

            return this;
        }
        
        /// <summary>
        /// Apply the current state of options to the given <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The hosting builder to apply these options to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        internal void ApplyOptions(IHostBuilder builder)
        {
            foreach (Action<IHostBuilder> hostConfigure in _hostingConfigures)
            {
                hostConfigure(builder);
            }

            builder.ConfigureAppConfiguration(config =>
            {
                foreach (Action<IConfigurationBuilder> appConfigure in _appConfigures)
                {
                    appConfigure(config);
                }
            });

            builder.ConfigureServices(services =>
            {
                services.AddRouting()
                        .AddControllers()
                        .AddApplicationPart(typeof(TestApiServer).Assembly);
                
                foreach (Action<IServiceCollection> configureService in _configureServices)
                {
                    configureService(services);
                }
            });

            builder.ConfigureWebHostDefaults(webHost =>
            {
                webHost.ConfigureKestrel(options => { })
                       .UseUrls(Url)
                       .Configure(app =>
                       {
                           foreach (Action<IApplicationBuilder> preconfigure in _preconfigures)
                           {
                               preconfigure(app);
                           }

                           app.UseRouting();

                           foreach (Action<IApplicationBuilder> configure in _configures)
                           {
                               configure(app);
                           }

                           app.UseEndpoints(endpoints => endpoints.MapControllers());
                       });
            });
        }
    }

    /// <summary>
    /// Represents a test API server which can be used to mimic a real-life hosted web API application.
    /// </summary>
    public class TestApiServer : IAsyncDisposable
    {
        private readonly IHost _host;
        private readonly TestApiServerOptions _options;
        private readonly ILogger _logger;

        private static readonly HttpClient HttpClient = new HttpClient();

        private TestApiServer(IHost host, TestApiServerOptions options, ILogger logger)
        {
            Guard.NotNull(host, nameof(host), "Requires a 'IHost' instance to start/stop the test API server");
            _host = host;
            _options = options;
            _logger = logger;
        }
        
        /// <summary>
        /// Starts a new instance of the <see cref="TestApiServer"/> using the configurable <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The configurable options to control the behavior of the test API server.</param>
        /// <param name="logger">The logger instance to include in the test API server to write diagnostic messages during the lifetime of the server.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> or <paramref name="logger"/> is <c>null</c>.</exception>
        public static async Task<TestApiServer> StartNewAsync(TestApiServerOptions options, ILogger logger)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of configurable options to control the behavior of the test API server");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write diagnostic messages during the lifetime of the test API server");

            IHostBuilder builder = Host.CreateDefaultBuilder();
            options.ApplyOptions(builder);
            options.ConfigureServices(services =>
                services.AddLogging(logging => logging.AddProvider(new CustomLoggerProvider(logger))));

            IHost host = builder.Build();
            var server = new TestApiServer(host, options, logger);
            await host.StartAsync();

            return server;
        }

        /// <summary>
        /// Sends a HTTP request to the test API server based on the result of the given request <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The builder instance to create an <see cref="HttpRequestMessage"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public async Task<HttpResponseMessage> SendAsync(HttpRequestBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a HTTP request builder instance to create a HTTP request to the test API server");

            HttpRequestMessage request = builder.Build(_options.Url);
            try
            {
                HttpResponseMessage response = await HttpClient.SendAsync(request);
                return response;
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Cannot connect to HTTP endpoint {Method} '{Uri}'", request.Method, request.RequestUri);
                throw;
            }
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
