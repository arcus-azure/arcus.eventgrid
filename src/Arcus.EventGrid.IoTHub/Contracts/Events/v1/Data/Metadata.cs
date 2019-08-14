using Microsoft.Azure.EventGrid.Models;
using System;

namespace Arcus.EventGrid.IoTHub.Contracts.Events.v1.Data
{
    [Obsolete(
        "Azure Event Grid events are now being used in favor of specific Arcus event types, use " 
        + nameof(IotHubDeviceCreatedEventData) + " for example or any  other  'IotHubDevice...' event data models" )]
    public class Metadata
    {
        public DateTime LastUpdated { get; set; }
    }
}