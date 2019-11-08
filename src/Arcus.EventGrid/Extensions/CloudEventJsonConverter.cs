using System;
using System.Net.Mime;
using CloudNative.CloudEvents;
using Newtonsoft.Json;

namespace Arcus.EventGrid.Extensions 
{
    public class CloudEventJsonConverter : JsonConverter<CloudEvent>
    {
        /// <summary>Writes the JSON representation of the object.</summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, CloudEvent value, JsonSerializer serializer)
        {
            var jsonFormatter = new JsonEventFormatter();
            byte[] encodeStructuredEvent = jsonFormatter.EncodeStructuredEvent(value, out ContentType _);

            writer.WriteValue(encodeStructuredEvent);
        }

        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read. If there is no existing value then <c>null</c> will be used.</param>
        /// <param name="hasExistingValue">The existing value has a value.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override CloudEvent ReadJson(
            JsonReader reader,
            Type objectType,
            CloudEvent existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var jsonFormatter = new JsonEventFormatter();
            byte[] bytes = reader.ReadAsBytes();
            
            return jsonFormatter.DecodeStructuredEvent(bytes);
        }
    }
}