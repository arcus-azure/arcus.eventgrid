using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace CloudNative.CloudEvents
{
    /// <summary>
    /// Represents a <see cref="HttpContent"/> model containing a batch of <see cref="CloudEvent"/>s instances.
    /// </summary>
    public class CloudEventBatchContent : HttpContent
    {
        private const string CloudEventContentType = "application/cloudevents+json; charset=UTF-8", 
                             CloudEventBatchContentType = "application/cloudevents-batch+json; charset=UTF-8";

        private readonly IEnumerable<CloudEventContent> _contents;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEventBatchContent"/> class.
        /// </summary>
        public CloudEventBatchContent(IEnumerable<CloudEvent> cloudEvents, ContentMode contentMode, ICloudEventFormatter formatter)
        {
            _contents = cloudEvents.Select(ev => new CloudEventContent(ev, contentMode, formatter)).ToArray();
            
            Headers.ContentType = 
                _contents.Count() == 1 
                    ? MediaTypeHeaderValue.Parse(CloudEventContentType)
                    : MediaTypeHeaderValue.Parse(CloudEventBatchContentType);
        }

        /// <summary>Serialize the HTTP content to a stream as an asynchronous operation.</summary>
        /// <param name="stream">The target stream.</param>
        /// <param name="context">Information about the transport (channel binding token, for example). This parameter may be null.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            await EncodeStringToStreamAsync(stream, "[", cancellationToken: default);

            foreach (CloudEventContent content in _contents)
            {
                await content.CopyToAsync(stream, context);
                await EncodeStringToStreamAsync(stream, ",", cancellationToken: default);
            }

            await EncodeStringToStreamAsync(stream, "]", cancellationToken: default);
            
            Headers.ContentType = MediaTypeHeaderValue.Parse(CloudEventBatchContentType);
        }

        private static async Task EncodeStringToStreamAsync(Stream stream, string input, CancellationToken cancellationToken)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(input);
            await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        }

        /// <summary>Determines whether the HTTP content has a valid length in bytes.</summary>
        /// <param name="length">The length in bytes of the HTTP content.</param>
        /// <returns>true if <paramref name="length">length</paramref> is a valid length; otherwise, false.</returns>
        protected override bool TryComputeLength(out long length)
        {
            bool result = _contents.Any();
            length = 0;

            foreach (CloudEventContent content in _contents)
            {
                long nextLength = 0;
                result = result && TryComputeCloudEventContentLength(content, out nextLength);
                length += nextLength;
            }

            return result;
        }

        private bool TryComputeCloudEventContentLength(CloudEventContent content, out long length)
        {
            MethodInfo methodInfo = typeof(CloudEventContent).GetMethod(nameof(TryComputeLength), BindingFlags.Instance | BindingFlags.NonPublic);
            length = 0;
            if (methodInfo is null)
            {
                return false;
            }

            var parameters = new object[] { length };
            var result = (bool) methodInfo.Invoke(content, parameters);

            length = (long) parameters[0];
            return result;
        }
    }
}
