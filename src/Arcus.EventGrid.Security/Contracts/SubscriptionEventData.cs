using Newtonsoft.Json;

namespace Arcus.EventGrid.Security.Contracts
{
    public class SubscriptionEventData
    {
        [JsonProperty(PropertyName = "validationCode")]
        public string ValidationCode { get; set; }
    }
}