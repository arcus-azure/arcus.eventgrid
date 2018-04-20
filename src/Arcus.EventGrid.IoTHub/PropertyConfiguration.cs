using System.Collections.Generic;
using Newtonsoft.Json;

namespace Arcus.EventGrid.IoTHub
{
    public class PropertyConfiguration
    {
        [JsonProperty(PropertyName = "$metadata")]
        public IDictionary<string, string> Metadata { get; set; }
        public int Version { get; set; }
    }

}
