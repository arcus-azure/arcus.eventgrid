using Arcus.EventGrid.Contracts;
using Arcus.EventGrid.Security.Contracts.Events.Data;

namespace Arcus.EventGrid.Security.Contracts.Events
{
    public class SubscriptionValidation : Event<SubscriptionEventData>
    {
        public SubscriptionValidation()
        {
        }

        public SubscriptionValidation(string id) : base(id)
        {
        }

        public SubscriptionValidation(string id, string subject) : base(id, subject)
        {
        }

        public override string DataVersion { get; set; } = "1";
        public override string EventType { get; set; } = "Microsoft.EventGrid.SubscriptionValidationEvent";
    }
}