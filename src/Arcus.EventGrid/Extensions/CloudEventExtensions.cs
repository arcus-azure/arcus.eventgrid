using GuardNet;
using Newtonsoft.Json.Linq;

// ReSharper disable once CheckNamespace
namespace CloudNative.CloudEvents
{
    /// <summary>
    /// Add extensions on the <see cref="CloudEvent"/> SDK model.
    /// </summary>
    public static class CloudEventExtensions
    {
        /// <summary>
        /// Gets the typed data payload from the <see cref="CloudEvent.Data"/> by parsing to a specified <typeparamref name="TData"/> type.
        /// </summary>
        /// <typeparam name="TData">The resulting event data type.</typeparam>
        /// <param name="cloudEvent">The event from which the event data should be parsed.</param>
        public static TData GetPayload<TData>(this CloudEvent cloudEvent)
        {
            Guard.NotNull(cloudEvent, nameof(cloudEvent));

            if (cloudEvent.Data is null)
            {
                return default(TData);
            }

            if (cloudEvent.Data is TData data)
            {
                return data;
            }

            return JObject.Parse(cloudEvent.Data.ToString()).ToObject<TData>();
        }
    }
}
