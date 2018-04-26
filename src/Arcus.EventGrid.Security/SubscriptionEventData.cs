using Newtonsoft.Json;

namespace Arcus.EventGrid.Security
{
    public class SubscriptionEventData
    {
        [JsonProperty(PropertyName = "validationCode")]
        public string ValidationCode { get; set; }
    }
}
