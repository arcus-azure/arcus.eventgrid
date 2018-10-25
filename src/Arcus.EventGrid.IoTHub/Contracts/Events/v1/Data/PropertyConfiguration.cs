using System.Collections.Generic;
using Newtonsoft.Json;

namespace Arcus.EventGrid.IoTHub.Contracts.Events.v1.Data
{
    public class PropertyConfiguration
    {
        [JsonProperty(PropertyName = "$metadata")]
        public IDictionary<string, string> Metadata { get; set; }

        [JsonProperty(PropertyName = "$version")]
        public int Version { get; set; }
    }
}