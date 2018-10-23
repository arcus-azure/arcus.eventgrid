using Newtonsoft.Json;

namespace Arcus.EventGrid.Security.Contracts.Events.Data
{
    public class SubscriptionEventData
    {
        [JsonProperty(PropertyName = "validationCode")]
        public string ValidationCode { get; set; }
    }
}