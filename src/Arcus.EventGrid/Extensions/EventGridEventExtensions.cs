using GuardNet;
using Newtonsoft.Json.Linq;

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.EventGrid.Models
{
    /// <summary>
    /// Adds extensions to the <see cref="EventGridEvent"/> Azure SDK model.
    /// </summary>
    public static class EventGridEventExtensions
    {
        /// <summary>
        /// Gets the typed data payload from the <see cref="EventGridEvent.Data"/> by parsing to a specified <typeparamref name="TData"/> type.
        /// </summary>
        /// <typeparam name="TData">The resulting event data type.</typeparam>
        /// <param name="eventGridEvent">The event from which the event data should be parsed.</param>
        public static TData GetPayload<TData>(this EventGridEvent eventGridEvent)
        {
            Guard.NotNull(eventGridEvent, nameof(eventGridEvent));

            if (eventGridEvent.Data is null)
            {
                return default(TData);
            }

            if (eventGridEvent.Data is TData data)
            {
                return data;
            }

            return JObject.Parse(eventGridEvent.Data.ToString()).ToObject<TData>();
        }
    }
}
