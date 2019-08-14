using System;
using System.Collections.Generic;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;

namespace Arcus.EventGrid.IoTHub.Contracts.Events.v1.Data
{
    [Obsolete(
        "Azure Event Grid events are now being used in favor of specific Arcus event types, use " 
        + nameof(IotHubDeviceCreatedEventData) + " for example or any  other  'IotHubDevice...' event data models" )]
    public class PropertyConfiguration
    {
        [JsonProperty(PropertyName = "$metadata")]
        public IDictionary<string, string> Metadata { get; set; }

        [JsonProperty(PropertyName = "$version")]
        public int Version { get; set; }
    }
}