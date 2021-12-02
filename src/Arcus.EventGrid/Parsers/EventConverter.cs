using System;
using System.Net.Mime;
using System.Text;
using Arcus.EventGrid.Contracts;
using CloudNative.CloudEvents;
using GuardNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Arcus.EventGrid.Parsers
{
    /// <summary>
    /// Converter to control how the <see cref="Event"/> model gets serialized and deserialized.
    /// </summary>
    public class EventConverter : JsonConverter<Event>
    {
        private static readonly JsonEventFormatter JsonFormatter = new JsonEventFormatter();

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="writer"/> or the <paramref name="value"/> is <c>null</c>.</exception>
        public override void WriteJson(JsonWriter writer, Event value, JsonSerializer serializer)
        {
            Guard.NotNull(writer, nameof(writer), "Requires a JSON writer to write the event");
            Guard.NotNull(value, nameof(value), "Requires an event instance to write to JSON");

            if (value.IsCloudEvent)
            {
                byte[] contents = JsonFormatter.EncodeStructuredEvent(value, out ContentType contentType);
                writer.WriteRaw(Encoding.UTF8.GetString(contents));
            }
            else if (value.IsEventGridEvent)
            {
                JObject.FromObject(value.AsEventGridEvent()).WriteTo(writer);
            }
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read. If there is no existing value then <c>null</c> will be used.</param>
        /// <param name="hasExistingValue">The existing value has a value.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="reader"/> is <c>null</c>.</exception>
        public override Event ReadJson(
            JsonReader reader,
            Type objectType,
            Event existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            Guard.NotNull(reader, nameof(reader), "Requires a JSON reader to read the event");

            JObject rawInput = JObject.Load(reader);
            return EventParser.ParseJObject(rawInput);
        }
    }
}