using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using System;

namespace Arcus.EventGrid.Security.Contracts.Events.v1.Data
{
    [Obsolete(
        "Azure Event Grid events are now being used in favor of specific Arcus event types, use " 
        + nameof(SubscriptionValidationEventData) + " for example or any  other  'Subscription...' event data models" )]
    public class SubscriptionEventData
    {
        [JsonProperty(PropertyName = "validationCode")]
        public string ValidationCode { get; set; }
    }
}