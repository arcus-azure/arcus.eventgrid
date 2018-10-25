using Newtonsoft.Json;

namespace Arcus.EventGrid.Security.Contracts.Events.v1.Data
{
    public class SubscriptionEventData
    {
        [JsonProperty(PropertyName = "validationCode")]
        public string ValidationCode { get; set; }
    }
}